namespace _sever.EF_Core.NavigationMenu
{
    public class CompletNode
    {
        private readonly NavigationDbContext navigationDbContext;

        public CompletNode(NavigationDbContext navigationDbContext)
        {
            this.navigationDbContext = navigationDbContext;
        }

        public NavigationNode CompletTheChildrenOfNode(NavigationNode node)
        {
            List<NavigationRecord> childenRecordList = navigationDbContext.navigationRecords.Where(entity => entity.ParentId == node.Id).OrderBy(entity => entity.PriorityLevel).ToList();
            foreach (NavigationRecord childenRecord in childenRecordList)
            { 
                NavigationNode childrenNode = new NavigationNode(childenRecord.Id, childenRecord.NavigationName, childenRecord.ParentId, childenRecord.ParentName, childenRecord.PriorityLevel, new List<NavigationNode>(), childenRecord.Type, childenRecord.Path);
                NavigationNode iterateNode = CompletTheChildrenOfNode(childrenNode);
                node.ChildrenNodes.Add(iterateNode);
            }
            return node;
        }
    }
}
