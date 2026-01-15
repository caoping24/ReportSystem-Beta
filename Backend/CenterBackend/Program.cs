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
            #region  1. 创建Web应用构建器 
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            #endregion

            #region  2. 依赖注入 - 服务注册区 (按功能分类注册，规范有序) 
            // ===== 2.1 仓储层注入 - 泛型仓储+业务仓储 =====
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped(typeof(IReportRepository<>), typeof(ReportRepository<>));
            builder.Services.AddScoped(typeof(IReportRecordRepository<>), typeof(ReportRecordRepository<>));

            // ===== 2.2 EF������ע�� - SQLServer���ݿ����� =====
            string defaultConnection = configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(defaultConnection));
            builder.Services.AddDbContext<CenterReportDbContext>(options => options.UseSqlServer(defaultConnection));

            // ===== 2.3 ������Ԫע�� - ������� =====
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IReportUnitOfWork, ReportUnitOfWork>();

            // ===== 2.4 ҵ������ע�� - ����ҵ���߼����� =====
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IFileServices, FileService>();
            builder.Services.AddScoped<IReportRecordService, ReportRecordService>();

            // ===== 2.5 MVC控制器服务 =====
            builder.Services.AddControllers();

            // ===== 2.6 SPA静态文件服务 关键 - 解决Vue前端部署+刷新404
            // 配合下方app.UseSpaStaticFiles()/app.UseSpa()使用，托管wwwroot下Vue打包文件
            builder.Services.AddSpaStaticFiles(spaConfig =>
            {
                spaConfig.RootPath = "wwwroot";
            });

            // ===== 2.7 Session�Ự���� - �����ڴ滺�棬��Ч��20����
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;  // ��ֹǰ��JS��ȡCookie����XSS
                options.Cookie.IsEssential = true;// �ỰCookieΪ��Ҫ�������˽ģʽ
            });

            // ===== 2.8 跨域CORS服务 优化容错 - 解决前后端分离跨域问题
            var allowedOrigins = configuration["CorsPolicy:AllowedOrigins"]?.Split(',') ?? Array.Empty<string>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Policy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();// ����Я��Cookie/Token��ƾ֤
                });
            });

            // ===== 2.9 ������������ =====
            builder.Services.AddHttpContextAccessor();// ��ȡHttp�����Ķ���
            builder.Services.AddSwaggerGen(c =>// Swagger�ӿ��ĵ�
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "����ϵͳAPI", Version = "v1" });
            });
            #endregion

            #region  3. 服务器配置 核心 - HttpSys生产级配置 (微软推荐Windows最优解) 
            string serviceUrl = configuration["applicationUrl"];
            builder.WebHost.UseHttpSys(options =>
            {
                options.UrlPrefixes.Add(serviceUrl);    // �������ļ��ķ��ʵ�ַ
                options.MaxConnections = 1000;           // ��󲢷�������
                options.RequestQueueLimit = 1000;        // �����������
                options.AllowSynchronousIO = true;       // ����ͬ��IO��������ҵ��Ӱ��
            });
            #endregion

            #region  4. 构建应用+配置中间件管道 重中之重，顺序绝对不能乱  
            var app = builder.Build();

            // ===== 4.1 ��̬�ļ����� - ԭ����̬�ļ�֧�֣�����wwwroot���ļ�
            app.UseStaticFiles();

            // ===== 4.2 跨域中间件 必须靠前 - 所有请求先处理跨域规则
            app.UseCors("Policy");

            // ===== 4.3 Swagger接口文档 生产优化 - 只在开发环境启用，生产环境自动关闭
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "����ϵͳAPI v1");
                });
            }

            // ===== 4.4 Session�Ự�м�� - ��������֤/ҵ���߼�֮ǰ
            app.UseSession();

            // ===== 4.5 全局异常处理中间件 核心 - 统一捕获所有异常，格式化返回结果
            // 已修复：标准HTTP状态码+自定义业务码，无40101非法状态码报错
            app.UseMiddleware<GlobalExceptionMiddleware>();


            // ===== 4.7 SPA静态文件中间件 - 配合上方AddSpaStaticFiles使用
            app.UseSpaStaticFiles();
=========
         
>>>>>>>>> Temporary merge branch 2
=========
         
>>>>>>>>> Temporary merge branch 2

            // ===== 4.8 SPA核心配置 关键 - 解决Vue History路由刷新404终极方案
            // 拦截所有匹配不到后端接口的请求，统一返回wwwroot/index.html，交给Vue前端路由处理
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
            });
            #endregion

            #region  5. 启动应用 
            app.Run();
            #endregion
        }
    }
}