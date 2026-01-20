using CenterBackend.IFileService;
using CenterBackend.IReportServices;
using CenterBackend.IUserServices;
using CenterBackend.Middlewares;
using CenterBackend.Services;
using CenterReport.Repository;
using CenterUser.Repository;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;

namespace CenterBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped(typeof(IReportRepository<>), typeof(ReportRepository<>));
            builder.Services.AddScoped(typeof(IReportRecordRepository<>), typeof(ReportRecordRepository<>));

            string defaultConnection = configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(defaultConnection));
            builder.Services.AddDbContext<CenterReportDbContext>(options => options.UseSqlServer(defaultConnection));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IReportUnitOfWork, ReportUnitOfWork>();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IFileServices, FileService>();
            builder.Services.AddScoped<IReportRecordService, ReportRecordService>();

            builder.Services.AddControllers();

            builder.Services.AddSpaStaticFiles(spaConfig =>
            {
                spaConfig.RootPath = "wwwroot/dist";
            });
            builder.Services.RemoveAll<ISessionStore>();
            builder.Services.RemoveAll<IDistributedCache>();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None; // 允许跨站携带Cookie
                options.Cookie.SecurePolicy = CookieSecurePolicy.None; // 开发环境关闭HTTPS强制校验
                options.Cookie.Name = "ReportSystem_SessionId"; //手动指定Cookie名称，避免默认随机名导致丢失
            });

            var allowedOrigins = configuration["CorsPolicy:AllowedOrigins"]?.Split(',') ?? Array.Empty<string>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Policy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
            builder.Services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = "ReportSystem_SessionId";
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                    options.SlidingExpiration = true;

                    options.Cookie.SameSite = SameSiteMode.None;//SameSite配置，和Session一致
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "报表系统API", Version = "v1" });
            });

            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(5260);
                options.Limits.MaxConcurrentConnections = 1000;
                options.AllowSynchronousIO = true;// 允许同步IO操作，避免Excel/NPOI文件流同步读写报错
                options.Limits.MaxConcurrentUpgradedConnections = 1000;
            });/*.UseUrls(configuration["applicationUrl"]);*/

            var app = builder.Build();

            if (app.Environment.IsDevelopment())// 1. 开发环境Swagger
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "报表系统API v1");
                    c.RoutePrefix = "swagger";
                });
            }
            // ==========顺序从上到下 ==========
            app.UseSpaStaticFiles();          // 1. SPA静态文件
            app.UseMiddleware<GlobalExceptionMiddleware>(); // 全局异常中间件
            app.UseCors("Policy");            // 2. 跨域（必须在路由前）
            app.UseSession();                 // 3. Session会话（必须在认证前）
            app.UseRouting();                 // 4. 路由中间件
            app.UseAuthentication();          // 5. 【新增】认证中间件 → 读取登录态
            app.UseAuthorization();           // 6. 授权中间件 → 校验权限
            app.MapControllers();             // 7. API路由映射
            app.MapFallbackToFile("dist/index.html"); //8. SPA刷新兜底

            app.Run();
        }
    }
}