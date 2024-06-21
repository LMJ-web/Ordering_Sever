using FluentValidation;
using FluentValidation.Results;
using IdentityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections;
using System.Transactions;
using Role = IdentityFramework.Role;

namespace _sever.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize(Roles = "admin,common")]
    public class UserManageController: ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly RoleManager<Role> roleManager;
        private readonly IValidator<UserVo> addUserValidator;
        public UserManageController(UserManager<User> userManager, RoleManager<Role> roleManager,IValidator<UserVo> addUserValidator)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.addUserValidator = addUserValidator;
        }
        [HttpGet]
        public async Task<IActionResult> AddRole(String RoleName) {
            Role roleInDb = await roleManager.FindByNameAsync(RoleName);
            if (roleInDb == null)
            {
                Role role = new Role { Name = RoleName };
                IdentityResult result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return Ok(role.Name + "添加成功！");
                }
                else return BadRequest(role.Name + "添加失败！");
            }
            else return BadRequest("角色已经存在！");
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(UserVo userVo)
        {
            ValidationResult validationResult = addUserValidator.Validate(userVo);
            if (!validationResult.IsValid)
            {
                string errors = "";
                foreach (var error in validationResult.Errors)
                {
                    errors += error.ErrorMessage;
                }
                return BadRequest(errors);
            }
            //判断用户是否存在
            User? userInDb = await userManager.FindByNameAsync(userVo.UserName);
            if (!(userInDb == null)) return BadRequest($"用户名{userVo.UserName}已经存在！");
            //判断角色是否存在
            foreach (string roleName in userVo.Roles) {
                Role roleInDb = await roleManager.FindByNameAsync(roleName);
                if (roleInDb == null) return BadRequest($"角色{roleName}不存在!");
            }
            //添加用户
            User user = new User { UserName = userVo.UserName, PhoneNumber = userVo.PhoneNumber, Email = userVo.Email, Sexuality = userVo.Sexuality };
            IdentityResult result = await userManager.CreateAsync(user, userVo.Password );
            if (!result.Succeeded) return Ok($"用户{userVo.UserName}添加失败！");
            //绑定用户和角色
            //var bindResult = await userManager.AddToRoleAsync(user, roleName);
            IdentityResult bindResult = await userManager.AddToRolesAsync(user, userVo.Roles);
            if (bindResult.Succeeded) { return Ok($"用户{userVo.UserName}添加成功"); }
            else {
                await userManager.DeleteAsync(user);
                return BadRequest($"用户角色绑定失败！");
            } 
        }
        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            List<User> users = userManager.Users.ToList();
            IEnumerator enumerator = users.GetEnumerator();
            List<UserDto> userDtos = new List<UserDto>();
            while (enumerator.MoveNext())
            {
                UserDto userDto = new UserDto();
                User user = (User)enumerator.Current;
                userDto.Id = user.Id;
                userDto.UserName = user.UserName;
                userDto.PhoneNumber = user.PhoneNumber;
                userDto.Email = user.Email;
                userDto.Sexuality = user.Sexuality;
                IList<string> roles = await userManager.GetRolesAsync((User)enumerator.Current);
                userDto.Roles = roles;
                userDtos.Add(userDto);
            }
            return Ok(userDtos);
        }
        [HttpGet]
        public IActionResult FindUserByName(string? userName)
        {
            if(userName.IsNullOrEmpty())
            {
                return Ok(userManager.Users);
            }
            IQueryable<User> users = userManager.Users.Where(x => x.UserName.Contains(userName));
            return Ok(users);
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateUser(UserVo userVo)
        {
            User userInDb = await userManager.FindByIdAsync(userVo.Id.ToString());
            userInDb.UserName = userVo.UserName;
            userInDb.PhoneNumber = userVo.PhoneNumber;
            userInDb.Email = userVo.Email;
            userInDb.Sexuality = userVo.Sexuality;
            using (TransactionScope tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) {
                await userManager.UpdateAsync(userInDb);
                IList<string> roles =await userManager.GetRolesAsync(userInDb);
                await userManager.RemoveFromRolesAsync(userInDb, roles);
                await userManager.AddToRolesAsync(userInDb,userVo.Roles);
                tx.Complete();
                return Ok("修改成功！");
            }  
        }
        [HttpGet]
        public IActionResult GetAllRole()
        {
            IQueryable<Role> roles = roleManager.Roles;
            return Ok(roles);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteUser(string userId) {
            User userInDb = await userManager.FindByIdAsync(userId);
            IList<string> roles = await userManager.GetRolesAsync(userInDb);
            using (TransactionScope tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) {
                await userManager.DeleteAsync(userInDb);
                await userManager.RemoveFromRolesAsync(userInDb, roles); 
                tx.Complete();
            }
            return Ok("删除成功!");   
        }
    }
}
