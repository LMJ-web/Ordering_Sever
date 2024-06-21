using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityFramework
{
    public class User: IdentityUser<long> {
        public string? Sexuality { get; set; }
        /*public List<string>? Roles { get; set; }*/
    }
}
