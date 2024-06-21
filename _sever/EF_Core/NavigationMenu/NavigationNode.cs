namespace _sever.EF_Core.NavigationMenu
{
    public record NavigationNode(int? Id, string NodeName, int ParentId, string ParentName, int? PriorityLevel, List<NavigationNode>? ChildrenNodes, int Type, string Path);
}
