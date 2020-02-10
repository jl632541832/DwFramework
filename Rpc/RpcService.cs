﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;

using Hprose.RPC;

using DwFramework.Core;
using DwFramework.Core.Extensions;

namespace DwFramework.Rpc
{
    public static class RpcServiceExtension
    {
        /// <summary>
        /// 注册Rpc服务
        /// </summary>
        /// <param name="host"></param>
        public static void RegisterRpcService(this ServiceHost host)
        {
            host.RegisterType<IRpcService, RpcService>().SingleInstance();
        }

        /// <summary>
        /// 初始化Rpc服务
        /// </summary>
        /// <param name="provider"></param>
        public static Task InitRpcServiceAsync(this IServiceProvider provider)
        {
            return provider.GetService<IRpcService, RpcService>().OpenServiceAsync();
        }
    }

    public class RpcService : IRpcService
    {
        public class Config
        {
            public string[] Prefixes { get; set; }
        }

        private readonly IServiceProvider _provider;
        private readonly IRunEnvironment _environment;
        private readonly Config _config;

        public Service Service { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="environment"></param>
        public RpcService(IServiceProvider provider, IRunEnvironment environment)
        {
            _provider = provider;
            _environment = environment;
            _config = _environment.GetConfiguration().GetSection<Config>("Rpc");
        }

        /// <summary>
        /// 开启Rpc服务
        /// </summary>
        /// <returns></returns>
        public Task OpenServiceAsync()
        {
            return Task.Run(() =>
            {
                var listener = new HttpListener();
                foreach (var item in _config.Prefixes)
                {
                    listener.Prefixes.Add(item.EndsWith("/") ? item : $"{item}/");
                }
                listener.Start();
                Service = new Service();
                Service.Bind(listener);
            });
        }

        /// <summary>
        /// 从实例中注册Rpc函数
        /// </summary>
        /// <param name="instance"></param>
        public void RegisterFuncFromInstance(object instance)
        {
            var methods = instance.GetType().GetMethods();
            foreach (var item in methods)
            {
                var attr = item.GetCustomAttribute<RpcAttribute>() as RpcAttribute;
                if (attr != null)
                {
                    Service.AddMethod(item.Name, instance, attr.CallName ?? "");
                }
            }
        }

        /// <summary>
        /// 从服务中注册Rpc函数
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="T"></typeparam>
        public void RegisterFuncFromService<I, T>() where T : class where I : class
        {
            T service = _provider.GetService<I, T>();
            var methods = service.GetType().GetMethods();
            foreach (var item in methods)
            {
                var attr = item.GetCustomAttribute<RpcAttribute>() as RpcAttribute;
                if (attr != null)
                {
                    Service.AddMethod(item.Name, service, attr.CallName ?? "");
                }
            }
        }

        /// <summary>
        /// 从服务中注册Rpc函数
        /// </summary>
        /// <typeparam name="I"></typeparam>
        public void RegisterFuncFromService<I>() where I : class
        {
            var services = _provider.GetAllServices<I>();
            foreach (var service in services)
            {
                var methods = service.GetType().GetMethods();
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<RpcAttribute>() as RpcAttribute;
                    if (attr != null)
                    {
                        Console.WriteLine(method.Name);
                        Service.AddMethod(method.Name, service, attr.CallName ?? "");
                    }
                }
            }
        }

        /// <summary>
        /// 从程序集中注册Rpc函数
        /// 仅支持从程序集中注册的服务
        /// </summary>
        /// <param name="assemblyName"></param>
        public void RegisterFuncFromAssembly(string assemblyName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Where(item => item.FullName.Split(",").First() == assemblyName).FirstOrDefault();
            if (assembly == null)
                throw new Exception("未找到该程序集");
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                var typeAttr = type.GetCustomAttribute<RegisterableAttribute>() as RegisterableAttribute;
                if (typeAttr == null)
                    continue;
                var methods = type.GetMethods();
                foreach (var method in methods)
                {
                    var methodAttr = method.GetCustomAttribute<RpcAttribute>() as RpcAttribute;
                    if (methodAttr != null)
                    {
                        Service.AddMethod(method.Name, _provider.GetService(typeAttr.InterfaceType), methodAttr.CallName ?? "");
                    }
                }
            }
        }

        /// <summary>
        /// 从程序集中注册Rpc函数
        /// 仅支持从程序集中注册的服务
        /// </summary>
        public void RegisterFuncFromAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var typeAttr = type.GetCustomAttribute<RegisterableAttribute>() as RegisterableAttribute;
                    if (typeAttr == null)
                        continue;
                    var methods = type.GetMethods();
                    foreach (var method in methods)
                    {
                        var methodAttr = method.GetCustomAttribute<RpcAttribute>() as RpcAttribute;
                        if (methodAttr != null)
                        {
                            Service.AddMethod(method.Name, _provider.GetService(typeAttr.InterfaceType), methodAttr.CallName ?? "");
                        }
                    }
                }
            }
        }
    }
}