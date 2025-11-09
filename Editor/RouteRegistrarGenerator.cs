using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// 路由注册器生成器，用于生成API路由注册代码
    /// </summary>
    public static class RouteRegistrarGenerator
    {
        /// <summary>
        /// 生成路由注册代码
        /// </summary>
        /// <param name="routes">路由信息列表</param>
        public static void GenerateRouteRegistrationCode(List<(string method, string path, MethodInfo methodInfo, Type controllerType)> routes)
        {
            // 读取模板
            var templatePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Editor", "RouteRegistrarTemplate.txt");
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"路由注册器模板文件不存在: {templatePath}");
                return;
            }

            string template = File.ReadAllText(templatePath, Encoding.UTF8);

            // 生成路由注册代码
            var routeRegistrationCode = new StringBuilder();
            foreach (var (method, path, methodInfo, controllerType) in routes)
            {
                var handlerName = ApiCodeGenerator.GenerateHandlerName(method, path, methodInfo);
                var parameterParserName = ApiCodeGenerator.GenerateParameterParserName(method, path, methodInfo);

                routeRegistrationCode.AppendLine($"            apiServer.RegisterRoute(\"{method}\", \"{path}\", ");
                routeRegistrationCode.AppendLine($"                new PredefinedApiHandlers.{handlerName}(), ");
                routeRegistrationCode.AppendLine($"                new PredefinedApiHandlers.{parameterParserName}(), ");
                routeRegistrationCode.AppendLine($"                \"{controllerType.Name}\", \"{methodInfo.Name}\");");
                routeRegistrationCode.AppendLine();
            }

            template = template.Replace("{{ROUTE_REGISTRATION_CODE}}", routeRegistrationCode.ToString());

            // 写入文件
            var outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Runtime", "GeneratedRouteRegistrar.cs");
            File.WriteAllText(outputPath, template, Encoding.UTF8);
        }
    }
}