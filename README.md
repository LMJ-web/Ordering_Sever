简介
-
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;本项目是一个堂食点餐系统，实现了顾客浏览菜品、添加购物车、多人协同点餐、提交订单、查看历史订单；管理员管理菜品信息、查看订单中心、员工管理等业务。
同时补充了用户认证(Authentication)、角色权限校验(Authorization)、接口流量限流(RateLimit)、日志管理(NLog)、实时数据刷新等功能。

技术架构
-
1、后台系统使用.NET Core Web API开发；客户端以微信小程序形式展示，使用Uniapp开发；后台管理员页面使用Vue3+Element Plus实现。  
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
用特性声明限制访问的接口。以下接口，仅限具有admin角色的用户才能访问
```C#
[Authorize(Roles = "admin")]
public async Task<IActionResult> AddUser(UserVo userVo){}
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
  
* 历史订单明细  
![HistoryOrderDetail](/_sever/Screenshot/HistoryOrderDetail.png)

