using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityFramework
{
    public class IFDbContext: IdentityDbContext<User, Role, long>
    {
        public IFDbContext(DbContextOptions<IFDbContext> options) : base(options) { }
    }
}
