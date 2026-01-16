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
                spaConfig.RootPath = "wwwroot";
            });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
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

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "报表系统API", Version = "v1" });
            });

            string serviceUrl = configuration["applicationUrl"];
            builder.WebHost.UseHttpSys(options =>
            {
                options.UrlPrefixes.Add(serviceUrl);
                options.MaxConnections = 1000;
                options.RequestQueueLimit = 1000;
                options.AllowSynchronousIO = true;
            });

            var app = builder.Build();

            app.UseStaticFiles();

            app.UseCors("Policy");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "报表系统API v1");
                });
            }

            app.UseSession();
         
            app.UseSessionCheck();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.MapControllers();

            app.UseSpaStaticFiles();

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
            });

            app.Run();
        }
    }
}