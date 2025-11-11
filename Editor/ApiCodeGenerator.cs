using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// API代码生成器，用于预生成路由注册和方法调用代码
    /// </summary>
    public static class ApiCodeGenerator
    {
        /// <summary>
        /// 生成API处理代码
        /// </summary>
        public static void GenerateApiHandlerCode()
        {
            try
            {
                var routes = ApiRouteFinder.FindApiRoutes();
                GenerateHandlerClasses(routes);
                RouteRegistrarGenerator.GenerateRouteRegistrationCode(routes);

                Debug.Log($"已生成API处理代码，共处理 {routes.Count} 个路由");

                // 刷新AssetDatabase以确保新生成的文件被Unity识别
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成API处理代码时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成API处理器类
        /// </summary>
        private static void GenerateHandlerClasses(List<(string method, string path, MethodInfo methodInfo, Type controllerType)> routes)
        {
            var generatedCode = new StringBuilder();
            generatedCode.AppendLine("using System;");
            generatedCode.AppendLine("using System.Net;");
            generatedCode.AppendLine("using System.IO;");
            generatedCode.AppendLine("using System.Text;");
            generatedCode.AppendLine("using LocalRestAPI.Runtime;");
            generatedCode.AppendLine("using UnityEngine;");
            generatedCode.AppendLine();
            generatedCode.AppendLine("namespace LocalRestAPI");
            generatedCode.AppendLine("{");
            generatedCode.AppendLine("    /// <summary>");
            generatedCode.AppendLine("    /// 预生成的API处理器类 - 自动从API控制器生成");
            generatedCode.AppendLine("    /// </summary>");
            generatedCode.AppendLine("    public static class PredefinedApiHandlers");
            generatedCode.AppendLine("    {");


            // 为每个API路由生成对应的处理器类和参数解析器类
            foreach (var (method, path, methodInfo, controllerType) in routes)
            {
                var handlerName = GenerateHandlerName(method, path, methodInfo);
                var parameterParserName = GenerateParameterParserName(method, path, methodInfo);

                // 生成参数解析器类（生成硬编码的签名数组 -> 调用共享解析器）
                ParameterParserGenerator.GenerateParameterParserClass(generatedCode, parameterParserName, methodInfo);

                // 生成API处理器类
                HandlerClassGenerator.GenerateHandlerClass(generatedCode, handlerName, methodInfo, controllerType, parameterParserName);
            }
            generatedCode.AppendLine("        // 预生成的API处理器类 - 自动从API控制器生成");
            generatedCode.AppendLine("    private static string Escape(string s) =>\n                s?.Replace(\"\\\\\", \"\\\\\\\\\").Replace(\"\\\"\", \"\\\\\\\"\") ?? \"\";      ");
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("}");

            // 确保生成代码的目录存在
            CodeGenerationConstants.EnsureDirectoryExists(CodeGenerationConstants.GeneratedCodeDirectory);
            
            // 写入文件
            var outputPath = Path.Combine(CodeGenerationConstants.GeneratedCodeDirectory, "GeneratedApiHandlers.cs");
            File.WriteAllText(outputPath, generatedCode.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 生成处理器类名（追加一个短哈希降低冲突风险）
        /// </summary>
        public static string GenerateHandlerName(string method, string path, MethodInfo methodInfo)
        {
            var cleanPath = new string(path.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            cleanPath = cleanPath.Replace("/", "_");
            return $"{method}{cleanPath}Handler_{GetShortHash(path)}";
        }

        /// <summary>
        /// 生成参数解析器类名（同样追加短哈希）
        /// </summary>
        public static string GenerateParameterParserName(string method, string path, MethodInfo methodInfo)
        {
            var cleanPath = new string(path.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            cleanPath = cleanPath.Replace("/", "_");
            return $"{method}{cleanPath}ParameterParser_{GetShortHash(path)}";
        }

        private static string GetShortHash(string input)
        {
            using var sha = System.Security.Cryptography.SHA1.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
            // 取前 4 字节 -> 8 个十六进制字符
            return BitConverter.ToString(bytes, 0, 4).Replace("-", "");
        }
    }
}