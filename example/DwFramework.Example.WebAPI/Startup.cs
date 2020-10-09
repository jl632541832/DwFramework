﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DwFramework.Core;
using DwFramework.Core.Extensions;
//using DwFramework.WebAPI.Swagger;
//using DwFramework.WebAPI.RequestFilter;

namespace DwFramework.Example.WebAPI
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //if (ServiceHost.Environment.EnvironmentType == EnvironmentType.Develop)
            //    services.AddSwagger("IndexServiceDoc", "索引服务", "v1");
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            // 请求过滤器
            //app.UseRequestFilter(new Dictionary<string, Action<HttpContext>>
            //{
            //    // 请求日志
            //    {"/*",context =>Console.WriteLine($"接收到请求:{context.Request.Path} ({GetIP(context)})")}
            //}, async (context, ex) =>
            //{
            //    Console.WriteLine(ex.Message);
            //    context.Response.Headers.Add("Content-type", "application/json");
            //    await context.Response.WriteAsync(ResultInfo.Fail(ex.Message).ToJson());
            //    return;
            //});
            //if (ServiceHost.Environment.EnvironmentType == EnvironmentType.Develop)
            //    app.UseSwagger("IndexServiceDoc", "索引服务");
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private string GetIP(HttpContext context)
        {
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
                ip = IPAddress.Parse(ip).MapToIPv4().ToString();
            if (string.IsNullOrEmpty(ip))
                ip = context.Connection.RemoteIpAddress.MapToIPv4().ToString();
            return ip;
        }
    }
}
