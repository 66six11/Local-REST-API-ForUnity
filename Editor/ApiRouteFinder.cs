using System;
using System.Collections.Generic;
using System.Reflection;
using LocalRestAPI.Runtime;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// API路由查找器，用于查找所有标记了ApiRouteAttribute的方法
    /// </summary>
    public static class ApiRouteFinder
    {
        /// <summary>
        /// 查找所有API路由
        /// </summary>
        /// <returns>包含路由信息的列表</returns>
        public static List<(string method, string path, MethodInfo methodInfo, Type controllerType)> FindApiRoutes()
        {
            var routes = new List<(string, string, MethodInfo, Type)>();
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Namespace != null && 
                            (type.Namespace.StartsWith("LocalRestAPI") || 
                             type.Name.Contains("Controller")))
                        {
                            // 检查类中的所有方法
                            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                            {
                                var routeAttr = method.GetCustomAttribute<ApiRouteAttribute>();
                                if (routeAttr != null)
                                {
                                    routes.Add((
                                        routeAttr.Method, 
                                        routeAttr.Path, 
                                        method, 
                                        type
                                    ));
                                    Debug.Log($"发现API路由: {routeAttr.Method} {routeAttr.Path} -> {type.Name}.{method.Name}");
                                }
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 忽略无法加载类型的程序集
                }
            }
            
            return routes;
        }
    }
}