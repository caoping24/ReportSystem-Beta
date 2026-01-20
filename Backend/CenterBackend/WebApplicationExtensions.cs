using CenterBackend.IFileService;
using CenterBackend.IReportServices;
using CenterBackend.IUserServices;
using CenterBackend.Middlewares;
using CenterBackend.Services;
using CenterReport.Repository;
using CenterUser.Repository;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CenterBackend
{
    /// <summary>
    /// 后端服务与管道配置扩展类（供WPF直接调用）
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// 配置后端所有依赖服务（复制Program.cs中builder.Services的全部逻辑）
        /// </summary>
        public static WebApplicationBuilder ConfigureBackendServices(this WebApplicationBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var configuration = builder.Configuration;

            // 1. 仓储层依赖注入（泛型仓储+业务仓储）
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped(typeof(IReportRepository<>), typeof(ReportRepository<>));
            builder.Services.AddScoped(typeof(IReportRecordRepository<>), typeof(ReportRecordRepository<>));

            // 2. 数据库上下文配置（读取ConnectionStrings）
            string defaultConnection = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("appsettings中未配置DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(defaultConnection, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            builder.Services.AddDbContext<CenterReportDbContext>(options =>
                options.UseSqlServer(defaultConnection, b => b.MigrationsAssembly(typeof(CenterReportDbContext).Assembly.FullName)));

            // 3. 工作单元模式注入
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IReportUnitOfWork, ReportUnitOfWork>();

            // 4. 业务服务层注入
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IFileServices, FileService>();
            builder.Services.AddScoped<IReportRecordService, ReportRecordService>();

            // 5. Web核心配置（控制器+SPA静态文件）
            builder.Services.AddControllers(options =>
            {
                // 可选：添加全局过滤器（如异常过滤器）
                // options.Filters.Add<CustomExceptionFilter>();
            });
            builder.Services.AddSpaStaticFiles(spaConfig =>
            {
                spaConfig.RootPath = "wwwroot/dist"; // Vue静态文件目录（与后端原有配置一致）
            });

            // 6. Session配置（复制原有逻辑，确保会话正常）
            builder.Services.RemoveAll<ISessionStore>();
            builder.Services.RemoveAll<IDistributedCache>();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                options.Cookie.Name = "ReportSystem_SessionId";
            });

            // 7. 跨域配置（读取appsettings中的CorsPolicy）
            var allowedOrigins = configuration["CorsPolicy:AllowedOrigins"]?.Split(',')
                ?? Array.Empty<string>();
            if (!allowedOrigins.Any())
            {
                throw new InvalidOperationException("appsettings中未配置CorsPolicy:AllowedOrigins");
            }
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

            // 8. 认证配置（Cookie认证）
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
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                });

            // 9. 其他辅助服务
            builder.Services.AddHttpContextAccessor(); // 提供HttpContext访问
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "报表系统API",
                    Version = "v1",
                    Description = "WPF集成启动后端API"
                });
            });

            // 10. Kestrel服务器配置（监听端口、并发限制等）
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(5260); // 监听所有IP的5260端口（可修改）
                options.Limits.MaxConcurrentConnections = 1000;
                options.AllowSynchronousIO = true; // 兼容Excel/NPOI同步读写
                options.Limits.MaxConcurrentUpgradedConnections = 1000;
                // 可选：配置HTTPS（生产环境用）
                // options.ListenAnyIP(5261, o => o.UseHttps());
            });

            return builder;
        }

        /// <summary>
        /// 配置后端中间件管道（复制Program.cs中app的全部逻辑）
        /// </summary>
        public static WebApplication ConfigureBackendPipeline(this WebApplication app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            // 1. 开发环境配置（Swagger）
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "报表系统API v1");
                    c.RoutePrefix = "swagger"; // Swagger访问路径：/swagger
                });
            }

            // 2. 中间件顺序（严格按照此顺序，否则功能异常）
            app.UseSpaStaticFiles(); // 1. 静态文件（Vue）
            app.UseMiddleware<GlobalExceptionMiddleware>(); // 2. 全局异常中间件
            app.UseCors("Policy"); // 3. 跨域（必须在路由前）
            app.UseSession(); // 4. Session（必须在认证前）
            app.UseRouting(); // 5. 路由
            app.UseAuthentication(); // 6. 认证（读取登录态）
            app.UseAuthorization(); // 7. 授权（校验权限）
            app.MapControllers(); // 8. API路由映射
            app.MapFallbackToFile("dist/index.html"); // 9. SPA刷新兜底（Vue路由模式为history时必需）

            return app;
        }
    }
}