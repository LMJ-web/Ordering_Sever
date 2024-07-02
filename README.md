简介
-
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;本项目是一个堂食点餐系统，实现了顾客浏览菜品、添加购物车、多人协同点餐、提交订单、查看历史订单；管理员管理菜品信息、查看订单中心、员工管理等业务。
同时补充了用户认证(Authentication)、角色权限校验(Authorization)、接口流量限流(RateLimit)、日志管理(NLog)、实时数据刷新等功能。

技术架构
-
1、后台系统使用.NET Core Web API开发，客户端微信小程序使用Uniapp开发，后台管理员页面使用Vue3+Element Plus实现。  
2、关系型数据库选择SQL Server2022，持久层使用EF Core框架。  
3、使用IdentifyFramework框架实现用户认证和权限校验功能。  
4、使用Redis数据库缓存热点数据。  
5、使用Swagger进行接口测试。
