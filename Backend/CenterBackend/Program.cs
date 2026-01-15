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
            #region 1. ����WebӦ�ù�����
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            #endregion

            #region 2. ����ע�� - ����ע���� (�����ܷ���ע�ᣬ�淶����)
            // ===== 2.1 �ִ���ע�� - ���Ͳִ�+ҵ��ִ� =====
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
          

            // ===== 2.5 MVC控制器服务 =====
>>>>>>>>> Temporary merge branch 2
>>>>>>>>> Temporary merge branch 2
            builder.Services.AddControllers();

            // ===== 2.6 SPA��̬�ļ�����ؼ�- ���Vueǰ�˲���+ˢ��404
            // ����·�app.UseSpaStaticFiles()/app.UseSpa()ʹ�ã��й�wwwroot��Vue����ļ�
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

            // ===== 2.8 ����CORS�����Ż��ݴ�- ���ǰ��˷����������
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

            #region 3. ���������ú���- HttpSys���������� (΢���Ƽ�Windows���Ž�)
            string serviceUrl = configuration["applicationUrl"];
            builder.WebHost.UseHttpSys(options =>
            {
                options.UrlPrefixes.Add(serviceUrl);    // �������ļ��ķ��ʵ�ַ
                options.MaxConnections = 1000;           // ��󲢷�������
                options.RequestQueueLimit = 1000;        // �����������
                options.AllowSynchronousIO = true;       // ����ͬ��IO��������ҵ��Ӱ��
            });
            #endregion

            #region 4. ����Ӧ��+�����м���ܵ�����֮�أ�˳����Բ�����
            var app = builder.Build();

            // ===== 4.1 ��̬�ļ����� - ԭ����̬�ļ�֧�֣�����wwwroot���ļ�
            app.UseStaticFiles();

            // ===== 4.2 �����м�����뿿ǰ- ���������ȴ���������
            app.UseCors("Policy");

            // ===== 4.3 Swagger�ӿ��ĵ������Ż�- ֻ�ڿ����������ã����������Զ��ر�
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

            // ===== 4.5 ȫ���쳣�����м������- ͳһ���������쳣����ʽ�����ؽ��
            // ����׼HTTP״̬��+�Զ���ҵ���룬40101��Ƿ�״̬�뱨��
            app.UseMiddleware<GlobalExceptionMiddleware>();


            // ===== 4.7 SPA静态文件中间件 - 配合上方AddSpaStaticFiles使用
            app.UseSpaStaticFiles();
=========
         
>>>>>>>>> Temporary merge branch 2
=========
         
>>>>>>>>> Temporary merge branch 2

            // ===== 4.8 SPA�������ùؼ�- ���Vue History·��ˢ��404�ռ�����
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
            });
            #endregion

            app.Run();
        }
    }
}