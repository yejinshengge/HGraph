using System;
using System.Collections.Generic;

namespace HGraph.Editor
{
    /// <summary>
    /// 管理图编辑命令的执行、撤销、重做以及保存点。
    /// </summary>
    public sealed class GraphCommandService
    {
        /// <summary>
        /// 命令历史。区间 [0, _cursor) 表示已经生效的命令，
        /// [_cursor, Count) 表示可被 Redo 的命令。
        /// </summary>
        private readonly List<IGraphCommand> _history = new List<IGraphCommand>();

        /// <summary>
        /// 命令执行上下文，内部持有当前图对象。
        /// </summary>
        private readonly GraphCommandContext _context;

        /// <summary>
        /// 当前命令游标，指向“下一条将被 Redo 的命令”。
        /// </summary>
        private int _cursor;

        /// <summary>
        /// 保存点游标。当前游标与保存点不一致时，窗口即为 Dirty。
        /// </summary>
        private int _saveCursor;

        /// <summary>
        /// 命令状态变化事件，供窗口层同步脏状态与视图刷新。
        /// </summary>
        public event Action<GraphCommandEvent> StateChanged;

        /// <summary>
        /// 当前是否可以撤销。
        /// </summary>
        public bool CanUndo => _cursor > 0;

        /// <summary>
        /// 当前是否可以重做。
        /// </summary>
        public bool CanRedo => _cursor < _history.Count;

        /// <summary>
        /// 当前命令游标是否偏离保存点。
        /// </summary>
        public bool IsDirty => _cursor != _saveCursor;

        /// <summary>
        /// 是否正在执行命令，用于避免重入。
        /// </summary>
        public bool IsExecuting { get; private set; }

        /// <summary>
        /// 为指定图对象创建命令服务。
        /// </summary>
        /// <param name="graphData">目标图对象。</param>
        public GraphCommandService(HGraphData graphData)
        {
            _context = new GraphCommandContext(graphData);
        }

        /// <summary>
        /// 执行一条新命令。若当前游标不在历史末尾，会先截断 redo 分支。
        /// </summary>
        public bool Execute(IGraphCommand command)
        {
            if (command == null || IsExecuting)
            {
                return false;
            }

            IsExecuting = true;
            try
            {
                if (!command.Execute(_context))
                {
                    return false;
                }

                if (_cursor < _history.Count)
                {
                    _history.RemoveRange(_cursor, _history.Count - _cursor);
                }

                _history.Add(command);
                _cursor = _history.Count;
                _raiseStateChanged(GraphCommandChangeType.Execute, command);
                return true;
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 撤销最近一次已生效的命令。
        /// </summary>
        /// <returns>是否撤销成功。</returns>
        public bool Undo()
        {
            if (!CanUndo || IsExecuting)
            {
                return false;
            }

            IsExecuting = true;
            try
            {
                var command = _history[_cursor - 1];
                command.Undo(_context);
                _cursor--;
                _raiseStateChanged(GraphCommandChangeType.Undo, command);
                return true;
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 重做当前游标指向的命令。
        /// </summary>
        /// <returns>是否重做成功。</returns>
        public bool Redo()
        {
            if (!CanRedo || IsExecuting)
            {
                return false;
            }

            IsExecuting = true;
            try
            {
                var command = _history[_cursor];
                if (!command.Execute(_context))
                {
                    return false;
                }

                _cursor++;
                _raiseStateChanged(GraphCommandChangeType.Redo, command);
                return true;
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 切换到另一张图时重置命令历史。
        /// </summary>
        public void Reset(HGraphData graphData)
        {
            if (IsExecuting)
            {
                return;
            }

            _history.Clear();
            _cursor = 0;
            _saveCursor = 0;
            _raiseStateChanged(GraphCommandChangeType.Reset, null, graphData);
        }

        /// <summary>
        /// 将当前游标记录为保存点。
        /// </summary>
        public void MarkSaved()
        {
            _saveCursor = _cursor;
            _raiseStateChanged(GraphCommandChangeType.MarkSaved, null);
        }

        /// <summary>
        /// 将命令历史变化广播给窗口层，由窗口决定如何刷新视图与更新脏状态。
        /// </summary>
        private void _raiseStateChanged(GraphCommandChangeType changeType, IGraphCommand command, HGraphData graphDataOverride = null)
        {
            StateChanged?.Invoke(new GraphCommandEvent(
                changeType,
                command,
                graphDataOverride ?? _context.GraphData,
                command?.RefreshMode ?? GraphCommandRefreshMode.Repaint,
                CanUndo,
                CanRedo,
                IsDirty));
        }
    }

    /// <summary>
    /// 命令服务状态变化类型。
    /// </summary>
    public enum GraphCommandChangeType
    {
        /// <summary>
        /// 执行新命令。
        /// </summary>
        Execute,

        /// <summary>
        /// 撤销命令。
        /// </summary>
        Undo,

        /// <summary>
        /// 重做命令。
        /// </summary>
        Redo,

        /// <summary>
        /// 重置命令历史。
        /// </summary>
        Reset,

        /// <summary>
        /// 标记当前游标为保存点。
        /// </summary>
        MarkSaved
    }

    /// <summary>
    /// 命令系统对外广播的状态快照。
    /// </summary>
    public readonly struct GraphCommandEvent
    {
        /// <summary>
        /// 本次事件对应的变化类型。
        /// </summary>
        public GraphCommandChangeType ChangeType { get; }

        /// <summary>
        /// 触发事件的命令对象；某些全局事件下可能为 null。
        /// </summary>
        public IGraphCommand Command { get; }

        /// <summary>
        /// 当前关联的图对象。
        /// </summary>
        public HGraphData GraphData { get; }

        /// <summary>
        /// 推荐窗口执行的刷新模式。
        /// </summary>
        public GraphCommandRefreshMode RefreshMode { get; }

        /// <summary>
        /// 当前是否可以撤销。
        /// </summary>
        public bool CanUndo { get; }

        /// <summary>
        /// 当前是否可以重做。
        /// </summary>
        public bool CanRedo { get; }

        /// <summary>
        /// 当前图是否处于未保存状态。
        /// </summary>
        public bool IsDirty { get; }

        /// <summary>
        /// 创建一份命令状态事件数据。
        /// </summary>
        /// <param name="changeType">变化类型。</param>
        /// <param name="command">关联命令。</param>
        /// <param name="graphData">关联图对象。</param>
        /// <param name="refreshMode">建议刷新模式。</param>
        /// <param name="canUndo">是否可撤销。</param>
        /// <param name="canRedo">是否可重做。</param>
        /// <param name="isDirty">是否脏。</param>
        public GraphCommandEvent(
            GraphCommandChangeType changeType,
            IGraphCommand command,
            HGraphData graphData,
            GraphCommandRefreshMode refreshMode,
            bool canUndo,
            bool canRedo,
            bool isDirty)
        {
            ChangeType = changeType;
            Command = command;
            GraphData = graphData;
            RefreshMode = refreshMode;
            CanUndo = canUndo;
            CanRedo = canRedo;
            IsDirty = isDirty;
        }
    }
}
