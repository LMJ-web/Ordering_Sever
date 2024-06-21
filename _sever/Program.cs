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

//ע���쳣����Filter
builder.Services.Configure<MvcOptions>(options => options.Filters.Add<GeneralExceptionFilter>());

builder.Services.AddSwaggerGen(c =>
{
    //����SwaggerЯ��JWT����
    var scheme = new OpenApiSecurityScheme() { 
        Description = "����", 
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

//��ȡsecrets.json�����ļ�
//secrets.json�ļ���appsetting.json�ļ��ɿ����ʽ����
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
//ע��Ȩ�޼�Ȩ����
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

//���˵�͹��������
builder.Services.AddSingleton<Dictionary<string,List<WXOrderDetailDto>>>();
//����AutoRestEvent���ֵ䣬������������
builder.Services.AddSingleton<Dictionary<string,AutoResetEvent>>();

//��������
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
//ע��redis����
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisOptions = ConfigurationOptions.Parse(configurationRoot.GetValue<string>("redisStr"));
    //redisOptions.Password = "123456";
    options.ConfigurationOptions = redisOptions;
    options.ConfigurationOptions.DefaultDatabase = 0;
});
//ע��SignalR����
builder.Services.AddSignalR();

//ע��FluentValidation����
builder.Services.AddScoped<IValidator<UserVo>, AddUserValidator>();

//��RateLimit�漰��������ͷ��񣬹��м��ʹ�á�
//����Ӧ�ó����ڴ滺�棬���ڴ����������Ip
builder.Services.AddMemoryCache();
//1����ȡ������ͨIp��������Ϣ
builder.Services.Configure<IpRateLimitOptions>(configurationRoot.GetSection("IpRateLimiting"));
//2����ȡ��������Ip��������Ϣ
builder.Services.Configure<IpRateLimitPolicies>(configurationRoot.GetSection("IpRateLimitPolicies"));

// inject counter and rules stores
builder.Services.AddInMemoryRateLimiting();
//ע��RateLimit���� 
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

//ע���ļ��ϴ����Ͱ������������ļ��ϴ��ӿڵ��ø÷���
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".jpg"] = "image/jpeg";
provider.Mappings[".jpeg"] = "image/jpeg";
provider.Mappings[".png"] = "image/png";
provider.Mappings[".webp"] = "image/webp";
builder.Services.AddSingleton(provider);

//.NetCoreĬ���ļ��ϴ�����Ϊ30Mb��ֻ��Ҫ�����ã�����Զ���ȡ��
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20480000;
    options.MultipartHeadersCountLimit = 200;
    options.MultipartHeadersLengthLimit = 204800;
    options.MultipartBoundaryLengthLimit = 1024;
    options.ValueLengthLimit = 2048000;
    options.MemoryBufferThreshold = 1024;
});
//ע����־����
builder.Services.AddLogging(configure =>
{
    //configure.AddConsole(); //��־���������̨
    configure.AddNLog();    //��־�������target������nlog.config�ļ�����
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

//��̬��Դ�м������/StaticFiles����ӳ�䵽StaticDataSource�ļ�����
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider( Path.Combine(builder.Environment.ContentRootPath, "StaticDataSource")),
    RequestPath = "/StaticFiles"
});
//ʹ��RateLimit�м��
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

//ʹ�÷����������м��
app.UseResponseCaching();
app.MapHub<MyHub>("/Hub/MyHub");
app.MapControllers();
app.Run();
