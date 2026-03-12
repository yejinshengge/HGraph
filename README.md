# HGraph 功能说明

本文档面向后续维护者与 AI Agent，说明当前 `HGraph` 编辑器的核心能力、数据模型、主流程和扩展方式。

## 1. 当前功能

- 支持创建 `HGraph` 子类实例并在编辑器窗口中打开
- 支持通过搜索窗口创建节点
- 支持节点拖拽移动
- 支持端口之间连线
- 支持删除节点、删除连线
- 支持自定义命令系统的撤销与重做
- 支持节点 Inspector 字段编辑，并纳入命令栈

当前尚未完成：

- `.hgraph` 的真实保存/加载
- 更细粒度的连线 diff 刷新
- 动态端口的完整编辑工作流

## 2. 目录概览

- `Assets/HGraph/Data/`
  - 图、节点、端口、连线的数据模型
- `Assets/HGraph/Editor/`
  - 编辑器窗口、GraphView、搜索窗口、命令系统
- `Assets/HGraph/Example/`
  - 示例图类型与示例节点

重点文件：

- `Assets/HGraph/Editor/HGraphWindow.cs`
- `Assets/HGraph/Editor/View/HGraphView.cs`
- `Assets/HGraph/Editor/View/HNodeView.cs`
- `Assets/HGraph/Editor/View/HPortView.cs`
- `Assets/HGraph/Editor/Command/GraphCommandService.cs`
- `Assets/HGraph/Editor/Command/GraphCommands.cs`

## 3. 数据模型

### 3.1 `HGraph`

根数据对象，持有：

- `Nodes`
- `Links`

### 3.2 `HNode`

节点数据对象，持有：

- `GUID`
- `GraphPosition`
- `Ports`

静态端口不是直接写在 `Ports` 里的，而是通过反射节点成员上声明的 `[Input]` / `[Output]` 特性，再由工具同步为实例端口。

### 3.3 `HPort`

端口实例数据，持有：

- `GUID`
- `NodeGUID`

端口自身只使用 `GUID` 作为唯一标识。  
静态端口与成员名的映射维护在 `HNode` 内部，连线的 `PortId` 也统一使用 `HPort.GUID`。

### 3.4 `HLink`

连线数据，持有：

- `FromNodeId`
- `ToNodeId`
- `FromPortId`
- `ToPortId`

它不直接引用节点对象或端口对象，而是完全依赖 GUID。

## 4. 命令系统

### 4.1 核心角色

- `IGraphCommand`
  - 命令接口，定义 `Execute()` / `Undo()`
- `GraphCommandService`
  - 管理命令历史、撤销栈、重做栈、保存点
- `GraphCommandContext`
  - 当前图数据上下文
- `GraphCommandRefreshMode`
  - 命令执行后窗口需要如何刷新

### 4.2 当前已有命令

- `CreateNodeCommand`
- `DeleteNodeCommand`
- `CreateLinkCommand`
- `DeleteLinkCommand`
- `ReplaceLinkCommand`
- `MoveNodeCommand`
- `EditNodeStateCommand`
- `CompositeGraphCommand`

### 4.3 命令历史规则

- `_history[0.._cursor)`：当前已经生效的命令
- `_history[_cursor..end)`：当前可被 `Redo` 的命令
- `_saveCursor`：保存点位置

当执行一条新命令且 `_cursor` 不在历史末尾时，会截断 redo 分支。

## 5. 编辑器主流程

### 5.1 创建节点

流程：

1. `HGraphView` 接收右键创建请求
2. 打开 `HNodeSearchWindow`
3. 用户选择节点类型
4. `HGraphView.CreateNode()` 执行 `CreateNodeCommand`
5. 命令成功后刷新结构视图

注意：

- 创建节点位置使用“打开搜索窗口时”的屏幕坐标
- 不是使用搜索弹窗列表项的点击坐标

### 5.2 连接端口

流程：

1. `HPortView` 监听拖拽连线
2. 拖拽完成后把 `(outputPort, inputPort)` 作为连接请求回调给 `HGraphView`
3. `HGraphView` 判断是否为重复连线、是否有单容量端口冲突
4. 执行 `CreateLinkCommand` 或 `ReplaceLinkCommand`
5. 窗口收到刷新事件后刷新连线视图

### 5.3 移动节点

流程：

1. 用户拖拽 `HNodeView`
2. `SetPosition()` 实时把位置写回 `NodeData.GraphPosition`
3. 鼠标抬起时记录一次 `MoveNodeCommand`

这样做的目的是：

- 拖拽过程画面始终正确
- 命令栈中只留下“最终一次移动”

### 5.4 编辑 Inspector

流程：

1. `HNodeView` 绘制 Odin Inspector
2. 编辑前生成节点快照
3. 编辑后生成节点快照
4. 若前后不同，则执行 `EditNodeStateCommand`

这一套逻辑依赖：

- `GraphCommandSnapshotUtility.CreateCopy()`
- `GraphCommandSnapshotUtility.AreEquivalent()`
- `GraphCommandSnapshotUtility.ApplyState()`

## 6. 刷新机制

窗口层不直接在命令回调里立刻重建整个视图，而是累积刷新意图到 `_pendingRefreshMode`。

当前刷新类型：

- `Structure`
  - 节点集合发生变化，需要同步 `NodeView`
- `Links`
  - 仅连线发生变化，刷新 `Edge`
- `NodePositions`
  - 仅节点位置发生变化
- `Repaint`
  - 触发窗口重绘

执行点在：

- `HGraphWindow._onCommandStateChanged()`
- `HGraphWindow._applyPendingGraphRefresh()`
- `HGraphWindow._scheduleGraphRefresh()`

`HGraphView` 当前的局部刷新策略：

- `RefreshStructureFromModel()`
  - 删除失效节点视图
  - 创建缺失节点视图
  - 同步位置
  - 重建连线
- `RefreshLinksFromModel()`
  - 先断开并清理旧 `Edge`
  - 再根据 `_graph.Links` 重建

注意：

- 清理旧 `Edge` 时必须先 `Disconnect`
- 否则端口会残留“已连接”状态，表现为空心/实心异常、无法重连

## 7. 快捷键与工具栏

当前支持：

- `Cmd+Z` / `Ctrl+Z`
  - 撤销
- `Cmd+Shift+Z` / `Ctrl+Shift+Z`
  - 重做

工具栏中也提供：

- `撤销`
- `重做`

这套逻辑完全基于自定义命令系统，不使用 Unity 自带 Undo。

## 8. 后续扩展建议

### 8.1 新增一种编辑操作

步骤建议：

1. 先判断该操作修改的是哪类源数据
2. 为它创建一个新的 `IGraphCommand`
3. 明确 `RefreshMode`
4. 在 View 层只发起命令请求，不直接改数据

### 8.2 新增节点类型

步骤建议：

1. 继承 `HNode`
2. 添加 `[HGraphNode(NodeOf = typeof(YourGraphType))]`
3. 普通字段直接作为 Inspector 数据
4. 端口字段用 `[Input]` / `[Output]`

### 8.3 新增动态端口

当前代码对静态端口支持较完整，但动态端口仍需要补：

- 显式新增/删除端口命令
- 端口列表编辑 UI
- 动态端口的持久化
- 动态端口重建与连线恢复

## 9. 维护注意事项

- 不要在 View 层直接写 `HGraph` / `HNode` / `HLink`，优先走命令
- 涉及 GUID 的对象不要随意重建，否则会破坏连线恢复
- 节点删除要把节点和关联连线视为一个原子操作
- 单容量端口重连必须走 `ReplaceLinkCommand`
- 如果修改了刷新逻辑，优先验证：
  - 创建节点
  - 删除节点
  - 拖动节点
  - 连线/重连/删线
  - Inspector 编辑撤销/重做

## 10. AI 使用建议

后续 AI 在修改该项目时，优先遵守以下原则：

- 先区分“数据层修改”还是“视图层刷新”
- 数据层操作优先封装为命令
- View 层只负责交互采集与命令派发
- 若新增操作可撤销，必须接入 `GraphCommandService`
- 若新增结构变化，明确选择 `Structure`、`Links`、`NodePositions` 中的刷新粒度

如果 AI 需要快速理解入口，建议先读：

1. `Assets/HGraph/Editor/HGraphWindow.cs`
2. `Assets/HGraph/Editor/View/HGraphView.cs`
3. `Assets/HGraph/Editor/Command/GraphCommandService.cs`
4. `Assets/HGraph/Editor/Command/GraphCommands.cs`
