namespace HGraph.Editor
{
    /// <summary>
    /// 创建单条连线。
    /// </summary>
    public sealed class CreateLinkCommand : IGraphCommand
    {
        /// <summary>
        /// 待创建的连线对象。
        /// </summary>
        private readonly HLinkData _linkData;

        public string Description => "Create Link";

        public GraphCommandRefreshMode RefreshMode => GraphCommandRefreshMode.Links | GraphCommandRefreshMode.Repaint;

        /// <summary>
        /// 创建连线命令。
        /// </summary>
        /// <param name="linkData">目标连线。</param>
        public CreateLinkCommand(HLinkData linkData)
        {
            _linkData = linkData;
        }

        public bool Execute(GraphCommandContext context)
        {
            if (_linkData == null || HGraphCommandHelper.ContainsLink(context.GraphData, _linkData))
            {
                return false;
            }

            context.GraphData.Links.Add(_linkData);
            return true;
        }

        public void Undo(GraphCommandContext context)
        {
            HGraphCommandHelper.RemoveLink(context.GraphData, _linkData);
        }
    }
}
