namespace _sever.EF_Core.NavigationMenu
{
    public class NavigationRecord
    {
        public int? Id { get; set; }
        public string NavigationName { get; set; }
        public int ParentId { get; set; }
        public string ParentName { get; set; }
        public int? PriorityLevel { get; set; }
        public int Type { get; set;}
        public string? Path { get; set;}

        public override bool Equals(object? obj)
        {
            return obj is NavigationRecord record &&
                   Id == record.Id &&
                   NavigationName == record.NavigationName &&
                   ParentId == record.ParentId &&
                   ParentName == record.ParentName &&
                   PriorityLevel == record.PriorityLevel &&
                   Type == record.Type &&
                   Path == record.Path;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, NavigationName, ParentId, ParentName, PriorityLevel, Type, Path);
        }
    }
        
    
}
