﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using DwFramework.Core;
using DwFramework.Web;
using DwFramework.Web.Socket;

namespace WebExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new ServiceHost();
            host.AddJsonConfig("Config.json");
            // host.ConfigureWebWithJson("Config.json", builder => builder.UseStartup<Startup>(), "web");
            // host.ConfigureSocket(ProtocolType.Tcp, "tcp");
            // host.ConfigureSocket(ProtocolType.Udp, "udp");
            // host.ConfigureLogging(builder => builder.UserNLog());

            host.ConfigureHostBuilder(b =>
            {

            });
            host.ConfigureServices(_ =>
            {

            });
            host.ConfigureLogging(_ =>
            {

            });
            host.ConfigureAppConfiguration(_ =>
            {

            });
            host.ConfigureHostConfiguration(_ =>
            {

            });
            host.ConfigureContainer(_ =>
            {

            });

            host.OnHostStarted += p =>
            {
                var web = p.GetWeb();
                web.OnWebSocketReceive += (c, a) => Console.WriteLine($"{c.ID} {Encoding.UTF8.GetString(a.Data)}");
                web.OnWebSocketClose += (c, a) => Console.WriteLine($"{c.ID} 断开连接");

                var tcp = p.GetTcp();
                tcp.OnConnect += (c, a) => Console.WriteLine($"{c.ID} connected");
                tcp.OnReceive += (c, a) =>
                {
                    Console.WriteLine($"{c.ID} received {Encoding.UTF8.GetString(a.Data)}");
                    var body = @"<h1>Hello World</h1><span>XXXXXXX</span>";
                    _ = c.SendAsync(Encoding.UTF8.GetBytes(
                        "HTTP/1.1 200 OK\r\n"
                        + "Date: Sat, 31 Dec 2005 23:59:59 GMT\r\n"
                        + "Content-Type: text/html;charset=UTF8\r\n"
                        + $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\n"
                        + $"{body}"
                    ));
                };
                tcp.OnSend += (c, a) => Console.WriteLine($"{c.ID} sent {Encoding.UTF8.GetString(a.Data)}");
                tcp.OnClose += (c, a) => Console.WriteLine($"{c.ID} closed");


                var udp = p.GetUdp();
                udp.OnReceive += (c, a) =>
                {
                    Console.WriteLine($"{c} received {Encoding.UTF8.GetString(a.Data)}");
                    udp.SendTo(Encoding.UTF8.GetBytes("World"), c);
                };
                udp.OnSend += (c, a) => Console.WriteLine($"{c} sent {Encoding.UTF8.GetString(a.Data)}");
            };
            await host.RunAsync();
        }
    }

    [Service]
    public interface IGreeterService
    {
        [Operation
        ]
        Task<HelloReply> SayHelloAsync(HelloRequest request,
            CallContext context = default);
    }

    [ProtoContract]
    public class HelloRequest
    {
        [ProtoMember(1)]
        public string Name { get; set; }
    }

    [ProtoContract]
    public class HelloReply
    {
        [ProtoMember(1)]
        public string Message { get; set; }
    }

    [RPC]
    public class GreeterService : IGreeterService
    {
        public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
        {
            return Task.FromResult(
                   new HelloReply
                   {
                       Message = $"Hello {request.Name}"
                   });
        }
    }

    public sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("any", builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("name", new OpenApiInfo()
                {
                    Title = "title",
                    Version = "version",
                    Description = "description"
                });
            });
            services.AddControllers(options =>
            {
                options.Filters.Add<ExceptionFilter>();
            }).AddJsonOptions(options =>
            {
                //不使用驼峰样式的key
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                //不使用驼峰样式的key
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
            });
            services.AddRpcImplements();

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddAntDesign();
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            app.UseCors("any");
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSwagger(c => c.RouteTemplate = "{documentName}/swagger.json");
            app.UseSwaggerUI(c => c.SwaggerEndpoint($"/{"name"}/swagger.json", "desc"));
            app.UseWebSocket();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRpcImplements();

                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
