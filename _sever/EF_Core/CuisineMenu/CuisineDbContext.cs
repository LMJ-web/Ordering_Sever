using _sever.EF_Core.NavigationMenu;
using Microsoft.EntityFrameworkCore;

namespace _sever.EF_Core.CuisineMenu
{
    public class CuisineDbContext:DbContext
    {
        private readonly IConfiguration _configuration;
        public CuisineDbContext(DbContextOptions<CuisineDbContext> options,IConfiguration configuration) : base(options) { 
        this._configuration = configuration;
        }

        public DbSet<Cuisine> Cuisines { get; set; }
        public DbSet<CuisineType> CuisineTypes { get; set; }

        //用于数据迁移时连接数据库
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            //string connStr = "Server=LMJ\\SQLEXPRESS;Database=Db;Trusted_Connection=True;";
            string connStr = _configuration.GetValue<string>("connStr");
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
