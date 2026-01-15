using CenterBackend.IFileService;
using CenterBackend.IReportServices;
using CenterBackend.IUserServices;
using CenterBackend.Middlewares;
using CenterBackend.Services;
using CenterReport.Repository;
using CenterUser.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.SpaServices;

namespace CenterBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region 1. 创建Web应用构建器
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            #endregion

            #region 2. 依赖注入 - 服务注册区 (按功能分类注册，规范有序)
            // ===== 2.1 仓储层注入 - 泛型仓储+业务仓储 =====
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped(typeof(IReportRepository<>), typeof(ReportRepository<>));
            builder.Services.AddScoped(typeof(IReportRecordRepository<>), typeof(ReportRecordRepository<>));

            // ===== 2.2 EF上下文注入 - SQLServer数据库连接 =====
            string defaultConnection = configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(defaultConnection));
            builder.Services.AddDbContext<CenterReportDbContext>(options => options.UseSqlServer(defaultConnection));

            // ===== 2.3 工作单元注入 - 事务管理 =====
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IReportUnitOfWork, ReportUnitOfWork>();

            // ===== 2.4 业务服务层注入 - 所有业务逻辑服务 =====
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IFileServices, FileService>();
            builder.Services.AddScoped<IReportRecordService, ReportRecordService>();

            // ===== 2.5 MVC控制器服务 =====
            builder.Services.AddControllers();

            // ===== 2.6 SPA静态文件服务关键- 解决Vue前端部署+刷新404
            // 配合下方app.UseSpaStaticFiles()/app.UseSpa()使用，托管wwwroot下Vue打包文件
            builder.Services.AddSpaStaticFiles(spaConfig =>
            {
                spaConfig.RootPath = "wwwroot";
            });

            // ===== 2.7 Session会话服务 - 基于内存缓存，有效期20分钟
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;  // 防止前端JS读取Cookie，防XSS
                options.Cookie.IsEssential = true;// 会话Cookie为必要项，兼容隐私模式
            });

            // ===== 2.8 跨域CORS服务优化容错- 解决前后端分离跨域问题
            var allowedOrigins = configuration["CorsPolicy:AllowedOrigins"]?.Split(',') ?? Array.Empty<string>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Policy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();// 允许携带Cookie/Token等凭证
                });
            });

            // ===== 2.9 其他基础服务 =====
            builder.Services.AddHttpContextAccessor();// 获取Http上下文对象
            builder.Services.AddSwaggerGen(c =>// Swagger接口文档
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "报表系统API", Version = "v1" });
            });
            #endregion

            #region 3. 服务器配置核心- HttpSys生产级配置 (微软推荐Windows最优解)
            string serviceUrl = configuration["applicationUrl"];
            builder.WebHost.UseHttpSys(options =>
            {
                options.UrlPrefixes.Add(serviceUrl);    // 绑定配置文件的访问地址
                options.MaxConnections = 1000;           // 最大并发连接数
                options.RequestQueueLimit = 1000;        // 请求队列上限
                options.AllowSynchronousIO = true;       // 兼容同步IO操作，无业务影响
            });
            #endregion

            #region 4. 构建应用+配置中间件管道重中之重，顺序绝对不能乱
            var app = builder.Build();

            // ===== 4.1 静态文件访问 - 原生静态文件支持，访问wwwroot下文件
            app.UseStaticFiles();

            // ===== 4.2 跨域中间件必须靠前- 所有请求先处理跨域规则
            app.UseCors("Policy");

            // ===== 4.3 Swagger接口文档生产优化- 只在开发环境启用，生产环境自动关闭
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "报表系统API v1");
                });
            }

            // ===== 4.4 Session会话中间件 - 必须在认证/业务逻辑之前
            app.UseSession();

            // ===== 4.5 全局异常处理中间件核心- 统一捕获所有异常，格式化返回结果
            // ：标准HTTP状态码+自定义业务码，40101会非法状态码报错
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // ===== 4.6 MVC路由映射 - 所有API接口路由入口
            app.MapControllers();

            // ===== 4.7 SPA静态文件中间件 - 配合上方AddSpaStaticFiles使用
            app.UseSpaStaticFiles();

            // ===== 4.8 SPA核心配置关键- 解决Vue History路由刷新404终极方案
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
            });
            #endregion

            app.Run();
        }
    }
}