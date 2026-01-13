using CenterBackend.IReportServices;
using CenterBackend.IUserServices;
using CenterBackend.Middlewares;
using CenterBackend.Services;
using CenterReport.Repository;
using CenterUser.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;


namespace CenterBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // 读取配置
            var configuration = builder.Configuration;

            //1:泛型仓储注入
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped(typeof(IReportRepository<>), typeof(ReportRepository<>));

            // 添加 DbContext 到服务容器
            string defaultConnection = configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(defaultConnection));
            builder.Services.AddDbContext<CenterReportDbContext>(options =>
                options.UseSqlServer(defaultConnection));
            //2:手动注册Service
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IReportUnitOfWork, ReportUnitOfWork>();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IReportService, ReportService>();

            builder.Services.AddControllers();

            // 添加会话服务
            //基于内存的Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20); // 设置超时时间
                options.Cookie.HttpOnly = true; // 确保 Cookie 只能通过 HTTP 访问
                options.Cookie.IsEssential = true; // 标记会话 Cookie 为必要
            });

            // 读取CORS策略配置
            var allowedOrigins = builder.Configuration["CorsPolicy:AllowedOrigins"];
            // 添加CORS服务，并定义一个策略
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Policy",
                    builder => builder.WithOrigins(allowedOrigins) // 允许的源
                                      .AllowAnyHeader() // 允许所有头部
                                      .AllowAnyMethod() // 允许所有HTTP方法
                                     .AllowCredentials()); // 如果需要支持凭证，则必须设置
            });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
            });

            // 指定启动url
            string url = builder.Configuration["applicationUrl"];
            builder.WebHost.UseUrls(url);

            var app = builder.Build();

            // Configure the HTTP request pipeline.          
            // 使用CORS中间件
            app.UseCors("Policy");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
            // 使用Session中间件
            app.UseSession();
            // 配置全局异常处理中间件
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.MapControllers();
            //app.Run();
            app.Run("http://0.0.0.0:5000");


        }
    }
}
