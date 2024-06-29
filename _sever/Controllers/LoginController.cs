using _sever.Vo;
using IdentityFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace _sever.Controllers
{

    [ApiController]
    [Route("[controller]/[action]")]
    public class LoginController:ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly IDistributedCache _cache;
        private readonly ILogger<LoginController> logger;
        private readonly IConfiguration configuration;
        public LoginController(UserManager<User> userManager, IDistributedCache _cache, ILogger<LoginController> logger,IConfiguration configuration)
        {
            this.userManager = userManager;
            this._cache = _cache;
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CheckUser(LoginVo loginVo)
        {
            Console.WriteLine(HttpContext.Connection.Id);
            string code = _cache.GetString(HttpContext.Connection.Id);
            if (!loginVo.Code.Equals(code, StringComparison.CurrentCultureIgnoreCase))
            {
                logger.LogInformation($"用户：'{loginVo.UserName}'验证码错误");
                return BadRequest("验证码错误！");
            }
            User userInDb = await userManager.FindByNameAsync(loginVo.UserName);
            if (userInDb == null) {
                logger.LogInformation($"用户：'{loginVo.UserName}'不存在");
                return BadRequest($"用户名{loginVo.UserName}不存在！"); 
            }
            bool result = await userManager.CheckPasswordAsync(userInDb, loginVo.Password);
            if (result)
            {
                //获取用户的角色
                IList<string> roleList = await userManager.GetRolesAsync(userInDb);
                //生成并返回token
                List<Claim> claimList = new List<Claim>();
                claimList.Add(new Claim(ClaimTypes.Name, loginVo.UserName));
                foreach (string role in roleList)
                {
                    claimList.Add(new Claim(ClaimTypes.Role, role));
                }
                string tokenSecretKey = configuration.GetValue<string>("tokenSecretKey");
                byte[] keyBytes = Encoding.UTF8.GetBytes(tokenSecretKey);
                SymmetricSecurityKey secKey = new SymmetricSecurityKey(keyBytes);
                var credential = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256Signature);
                DateTime expire = DateTime.Now.AddDays(1);
                JwtSecurityToken tokenDescriptor = new JwtSecurityToken(claims: claimList, expires: expire, signingCredentials: credential);
                string jwt = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
                logger.LogInformation($"用户：'{loginVo.UserName}'登录成功");
                return Ok(jwt);
            }
            else {
                logger.LogInformation($"用户：'{loginVo.UserName}'密码错误");
                return BadRequest("用户名或密码错误！");
            }
        }
    }
}
