using _sever.Controllers.Filter.Exception_Filter;
using _sever.EF_Core.CuisineMenu;
using _sever.EF_Core.NavigationMenu;
using _sever.MyHub;
using IdentityFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using _sever.EF_Core.OrderAndDetail;
using FluentValidation.AspNetCore;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Http.Features;
using NLog.Extensions.Logging;
using _sever.entity;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using System;
using _sever.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<CompletNode>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//注册异常处理Filter
builder.Services.Configure<MvcOptions>(options => options.Filters.Add<GeneralExceptionFilter>());

builder.Services.AddSwaggerGen(c =>
{
    //开启Swagger携带JWT功能
    var scheme = new OpenApiSecurityScheme() { 
        Description = "描述", 
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Authorization"},
        Scheme = "oauth2",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    };
    c.AddSecurityDefinition("Authorization", scheme);
    var requirementDictionary = new OpenApiSecurityRequirement();
    requirementDictionary[scheme] = new List<string>();
    c.AddSecurityRequirement(requirementDictionary);
});

//读取secrets.json配置文件
//secrets.json文件和appsetting.json文件由框架隐式加载
IConfigurationBuilder configurationBuilder = builder.Configuration;
IConfigurationRoot configurationRoot = configurationBuilder.Build();
string DateBaseConnStr = configurationRoot.GetValue<string>("connStr");


builder.Services.AddDbContext<IFDbContext>(option =>
{
    option.UseSqlServer(DateBaseConnStr);
    
},ServiceLifetime.Transient);
builder.Services.AddDbContext<NavigationDbContext>(option =>
{
    option.UseSqlServer(DateBaseConnStr);
});

builder.Services.AddDbContext<CuisineDbContext>(option =>
{
    option.UseSqlServer(DateBaseConnStr);
});

builder.Services.AddDbContext<OrderDbContext>(option =>
{
    option.UseSqlServer(DateBaseConnStr);
});

builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
});
var identityBuilder = new IdentityBuilder(typeof(User), typeof(IdentityFramework.Role), builder.Services);
identityBuilder.AddEntityFrameworkStores<IFDbContext>()
    .AddUserManager<UserManager<User>>()
    .AddRoleManager<RoleManager<IdentityFramework.Role>>()
    .AddDefaultTokenProviders();
//注册权限鉴权服务
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(option =>
{
    byte[] keyBytes= Encoding.UTF8.GetBytes(configurationRoot.GetValue<string>("tokenSecretKey"));
    SymmetricSecurityKey secKey = new SymmetricSecurityKey(keyBytes);
    option.TokenValidationParameters = new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = secKey
    };
});

//多人点餐共享的数据
builder.Services.AddSingleton<Dictionary<string,List<WXOrderDetailDto>>>();
//储存AutoRestEvent的字典，用于锁定订单
builder.Services.AddSingleton<Dictionary<string,AutoResetEvent>>();

//开启跨域
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(browser =>
    {
        browser.WithOrigins(new string[] { "http://127.0.0.1:5173" })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
//注册redis服务
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisOptions = ConfigurationOptions.Parse(configurationRoot.GetValue<string>("redisStr"));
    //redisOptions.Password = "123456";
    options.ConfigurationOptions = redisOptions;
    options.ConfigurationOptions.DefaultDatabase = 0;
});
//注册SignalR服务
builder.Services.AddSignalR();

//注册FluentValidation服务
builder.Services.AddScoped<IValidator<UserVo>, AddUserValidator>();

//配RateLimit涉及的配置类和服务，供中间件使用。
//开启应用程序内存缓存，用于储存计数器和Ip
builder.Services.AddMemoryCache();
//1、读取限制普通Ip的配置信息
builder.Services.Configure<IpRateLimitOptions>(configurationRoot.GetSection("IpRateLimiting"));
//2、读取限制特殊Ip的配置信息
builder.Services.Configure<IpRateLimitPolicies>(configurationRoot.GetSection("IpRateLimitPolicies"));

// inject counter and rules stores
builder.Services.AddInMemoryRateLimiting();
//注册RateLimit服务 
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

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
//注册日志服务
builder.Services.AddLogging(configure =>
{
    //configure.AddConsole(); //日志输出到控制台
    configure.AddNLog();    //日志输出到多target，可在nlog.config文件配置
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();

app.UseHttpsRedirection();

//静态资源中间件，将/StaticFiles请求映射到StaticDataSource文件夹下
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider( Path.Combine(builder.Environment.ContentRootPath, "StaticDataSource")),
    RequestPath = "/StaticFiles"
});
//使用RateLimit中间件
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

//使用服务器缓存中间件
app.UseResponseCaching();
app.MapHub<MyHub>("/Hub/MyHub");
app.MapControllers();
app.Run();
