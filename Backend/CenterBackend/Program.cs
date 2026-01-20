using CenterBackend;
using CenterBackend.IFileService;
using CenterBackend.IReportServices;
using CenterBackend.IUserServices;
using CenterBackend.Middlewares;
using CenterBackend.Services;
using CenterReport.Repository;
using CenterUser.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;


    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureBackendServices(); // 调用扩展方法配置服务

    var app = builder.Build();
    app.ConfigureBackendPipeline(); // 调用扩展方法配置中间件
    app.Run();
