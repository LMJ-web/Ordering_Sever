using IdentityFramework;
using Microsoft.EntityFrameworkCore;

namespace _sever.EF_Core.NavigationMenu
{
    public class NavigationDbContext: DbContext
    {
        public NavigationDbContext(DbContextOptions<NavigationDbContext> options) : base(options) { }
        public DbSet<NavigationRecord> navigationRecords { get; set; }

        //用于数据迁移时连接数据库
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            string connStr = "Server=LMJ\\SQLEXPRESS;Database=Db;Trusted_Connection=True;";
            optionsBuilder.UseSqlServer(connStr);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //从当前类所在的程序集获取配置类
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }
    }
}
