using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityFramework
{
    public class UserDto
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Sexuality { get; set; }
        public IList<string> Roles { get; set; }
    }
}
