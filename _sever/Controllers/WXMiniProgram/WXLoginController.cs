using _sever.entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace _sever.Controllers.WXMiniProgram
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WXLoginController:ControllerBase
    {
        private readonly IDistributedCache redis_cache;
        private readonly IConfiguration configuration;

        public WXLoginController(IDistributedCache redis_cache, IConfiguration configuration)
        {
            this.redis_cache = redis_cache;
            this.configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> GetKeyOfOpenId(string authorizationCode)
        {
            /*Console.WriteLine(authorizationCode);*/
            string appId = configuration.GetValue<string>("appId");
            string secretKey = configuration.GetValue<string>("secretKey");
            var url = $"https://api.weixin.qq.com/sns/jscode2session?appid={appId}&secret={secretKey}&js_code={authorizationCode}&grant_type=authorization_code";
            string res = await new HttpClient().GetStringAsync(url);
            Console.WriteLine(res);
            if (!string.IsNullOrEmpty(res))
            {
                //反序列化res
                Res json = Newtonsoft.Json.JsonConvert.DeserializeObject<Res>(res);
                //将openid存入redis中，设置过期时间
                var options = new DistributedCacheEntryOptions();
                options.SlidingExpiration = TimeSpan.FromDays(1);
                redis_cache.SetString(json.Session_key, json.Openid, options);
                return Ok(json.Session_key);
            }
            else
            {
                return BadRequest("openid获取失败！");
            }
            
        }
    }
}
