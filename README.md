简介
-
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;本项目是一个堂食点餐系统，实现了顾客浏览菜品、添加购物车、多人协同点餐、提交订单、查看历史订单；管理员管理菜品信息、查看订单中心、员工管理等业务。
同时补充了用户认证(Authentication)、角色权限校验(Authorization)、接口流量限流(RateLimit)、日志管理(NLog)、数据实时刷新等功能。

技术架构
-
1、后台系统使用.NET Core Web API开发；客户端以微信小程序形式展示，使用Uniapp开发；后台管理员页面使用Vite+Element Plus实现。  
2、关系型数据库选择SQL Server2022，持久层使用EF Core框架。  
3、使用IdentifyFramework框架实现用户认证和权限校验功能。  
4、使用Redis数据库缓存热点数据。  
5、使用Swagger进行接口测试。

功能点
-
* 权限校验
  
用户登录成功时，生成包含用户角色的Token并写入前端session中，后续只有具有相应角色属性的用户才能访问相关接口。
```C#
public async Task<IActionResult> CheckUser(LoginVo loginVo)
{
    //用户名密码正确  
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
    SymmetricSecurityKey secKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecretKey));
    var credential = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256Signature);
    DateTime expire = DateTime.Now.AddDays(1);
    JwtSecurityToken tokenDescriptor = new JwtSecurityToken(claims: claimList, expires: expire, signingCredentials: credential);
    string token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    return Ok(token);
}
```
Program.cs文件中注册校验中间件
```C#
app.UseAuthentication();
app.UseAuthorization();
```
用特性声明限制访问的接口。以下接口，仅限具有admin角色的用户才能访问
```C#
[Authorize(Roles = "admin")]
public async Task<IActionResult> AddUser(UserVo userVo){}
```
* 接口限流
  
使用AspNetCoreRateLimit中间件实现接口限流，每个IP 1秒内只能请求10次。限流配置如下appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "IpWhitelist": [],
    "EndpointWhitelist": ["*:/Hub/MyHub"],
    "ClientWhitelist": [],
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1s",
        "Limit": 10
      },
      {
        "Endpoint": "*",
        "Period": "15m",
        "Limit": 100
      },
      {
        "Endpoint": "*",
        "Period": "12h",
        "Limit": 1000
      },
      {
        "Endpoint": "*",
        "Period": "7d",
        "Limit": 10000
      }
    ]
  }
}
```
Program.cs文件中配置限流服务，并注册RateLimit中间件
```C#
//1、读取限制普通Ip的配置信息并注册
builder.Services.Configure<IpRateLimitOptions>(configurationRoot.GetSection("IpRateLimiting"));

//使用RateLimit中间件
app.UseIpRateLimiting();
```
* 日志模块
   
使用NLog实现日志模块，配置如下nlog.config
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      internalLogLevel="Info"
      internalLogFile="c:\temp\internal-nlog-AspNetCore.txt">

	<!-- enable asp.net core layout renderers -->
	<extensions>
		<add assembly="NLog.Web.AspNetCore"/>
	</extensions>

	<!-- the targets to write to -->
	<targets>
		<!-- File Target for all log messages with basic details -->
		<!--所有日志-->
		<target xsi:type="File" name="allfile" fileName="nlog-AspNetCore-all-${shortdate}.log"
				layout="${longdate}|${event-properties:item=EventId:whenEmpty=0}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}" />

		<!-- File Target for own log messages with extra web details using some ASP.NET core renderers -->
		<!--与web有关的日志-->
		<target xsi:type="File" name="ownFile-web" fileName="nlog-AspNetCore-own-${shortdate}.log"
				layout="${longdate}|${event-properties:item=EventId:whenEmpty=0}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}|url: ${aspnet-request-url}|action: ${aspnet-mvc-action}" />

		<!--向控制台输出的log -->
		<target xsi:type="Console" name="lifetimeConsole" layout="${MicrosoftConsoleLayout}" />
	</targets>

	<!-- rules to map from logger name to target -->
	<rules>
		<!--All logs, including from Microsoft-->
		<logger name="*" minlevel="Trace" writeTo="allfile" />

		<!--Output hosting lifetime messages to console target for faster startup detection -->
		<logger name="Microsoft.Hosting.Lifetime" minlevel="Info" writeTo="lifetimeConsole, ownFile-web" final="true" />

		<!--可忽略的log-->
		<logger name="Microsoft.*" maxlevel="Info" final="true" />
		<logger name="System.Net.Http.*" maxlevel="Info" final="true" />

		<logger name="*" minlevel="Trace" writeTo="ownFile-web" />
	</rules>
</nlog>
```
Program.cs文件中注册NLog服务
```C#
builder.Services.AddLogging(configure =>
{
    configure.AddNLog();    //日志输出到多target，在nlog.config文件中配置
});
```
在接口中使用NLog服务。例如用户登录失败时，记录失败原因
```C#
[ApiController]
[Route("[controller]/[action]")]
public class LoginController:ControllerBase
{
  private readonly ILogger<LoginController> logger;
  public LoginController(UserManager<User> userManager, IDistributedCache _cache, ILogger<LoginController> logger,IConfiguration configuration)
  {
    this.logger = logger;
  }
  public async Task<IActionResult> CheckUser(LoginVo loginVo)
  {
    logger.LogInformation($"用户：'{loginVo.UserName}'验证码错误");
    return BadRequest("验证码错误！");
  }
}
```
* 图片上传限制

添加菜品时，需要对菜品图片的类型和大小做限制
```C#
//注册文件上传类型白名单服务，在文件上传接口调用该服务
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".jpg"] = "image/jpeg";
provider.Mappings[".jpeg"] = "image/jpeg";
provider.Mappings[".png"] = "image/png";
provider.Mappings[".webp"] = "image/webp";
builder.Services.AddSingleton(provider);

//.NetCore默认文件上传限制为30Mb，只需要改配置，框架自动读取。
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20480000;
    options.MultipartHeadersCountLimit = 200;
    options.MultipartHeadersLengthLimit = 204800;
    options.MultipartBoundaryLengthLimit = 1024;
    options.ValueLengthLimit = 2048000;
    options.MemoryBufferThreshold = 1024;
});
```
在上传图片接口调用校验服务
```C#
//依赖注入单例
public readonly FileExtensionContentTypeProvider provider;

[HttpPost]
[Authorize(Roles = "admin")]
public IActionResult UploadPicture(IFormFile file)
{
  if (!provider.TryGetContentType(file.FileName, out _))
  {
    return BadRequest("上传的文件类型错误！");
  }
}
```
* 数据实时刷新

多人协同点餐时，某顾客点餐后同桌的其他顾客需要及时同步点餐数据。使用SignalR中间件实现数据实时刷新功能流程如下：  
1、客户端和服务器端使用SignalR组件建立WebSocket连接。  
2、服务器端将同餐桌的连接分为同一个group。  
3、顾客顾客点餐时向服务器端发送餐桌号，服务器端向同组的其他客户端发送signal。  
4、客户端收到服务器端发送的signal后，发起http请求获取最新数据。
```C#
namespace _sever.MyHub
{
    public class MyHub : Hub
    {
        public async Task AddConnectionToGroup(string table, string nickname, string avatarUrl)
        {
             await this.Groups.AddToGroupAsync(this.Context.ConnectionId, table);
             await this.Clients.Client(this.Context.ConnectionId).SendAsync("sameTableCustomer", nickname, avatarUrl);
        }
        public async Task NotifySameTableCustomer(string tableNo)
        {
            await this.Clients.GroupExcept(tableNo, new List<string> { this.Context.ConnectionId }).SendAsync("IsRefreshOrderDetails", true);
        }
        public async Task RefreshOrderState(bool state)
        {
            await this.Clients.All.SendAsync("refreshOrderState", state);
            
        }
    }
}
```
Program.cs文件配置Hub中间件映射
```C#
app.MapHub<MyHub>("/Hub/MyHub");
```


项目截图
-
* Swagger接口管理  
![Swagger](/_sever/Screenshot/Swagger.png)
  
* 管理员登录  
![Login](/_sever/Screenshot/Login.png)
  
* 导航管理  
![Navigation](/_sever/Screenshot/NavigationMenu.png)
  
* 人员管理  
![Employee](/_sever/Screenshot/Employee.png)
  
* 菜品管理  
![CuisineInfo](/_sever/Screenshot/CuisineInfo.png)
  
* 添加菜品  
![AddCuisine](/_sever/Screenshot/AddCuisine.png)
  
* 订单管理  
![OrderRecord](/_sever/Screenshot/OrderRecord.png)
  
* 微信登录  
![WX_Login](/_sever/Screenshot/WX_Login.png)
  
* 菜品展示  
![WX_ShowCuisine](/_sever/Screenshot/ShowCuisine.png)
   
* 购物车  
![ShoppingCar](/_sever/Screenshot/ShoppingCar.png)
  
* 提交订单  
![SubmitOrder](/_sever/Screenshot/SubmitOrder.png)
  
* 订单明细  
![WX_OrderRecord](/_sever/Screenshot/WX_OrderRecord.png)
  
* 个人历史订单    
![HistoryOrder](/_sever/Screenshot/HistoryOrder.png)


