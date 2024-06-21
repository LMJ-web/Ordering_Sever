using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityFramework
{
    //本类只用于数据迁移
    public class IFDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IFDbContext>
    {
        public IFDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<IFDbContext> builder = new DbContextOptionsBuilder<IFDbContext>();
            builder.UseSqlServer("Server=LMJ\\SQLEXPRESS;Database=Db;Trusted_Connection=True;");
            return new IFDbContext(builder.Options);
        }
    }
}
