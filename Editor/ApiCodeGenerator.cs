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
    public class ApiCodeGenerator
    {
        /// <summary>
        /// 生成API处理代码
        /// </summary>
        public static void GenerateApiHandlerCode()
        {
            try
            {
                var routes = FindApiRoutes();
                GenerateHandlerClasses(routes);
                GenerateRouteRegistrationCode(routes);
                
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
        /// 查找所有API路由
        /// </summary>
        private static List<(string method, string path, MethodInfo methodInfo, Type controllerType)> FindApiRoutes()
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
                                    routes.Add((routeAttr.Method, routeAttr.Path, method, type));
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
        
        /// <summary>
        /// 生成API处理器类
        /// </summary>
        private static void GenerateHandlerClasses(List<(string method, string path, MethodInfo methodInfo, Type controllerType)> routes)
        {
            var generatedCode = new System.Text.StringBuilder();
            generatedCode.AppendLine("using System;");
            generatedCode.AppendLine("using System.Net;");
            generatedCode.AppendLine("using System.IO;");
            generatedCode.AppendLine("using System.Text;");
            generatedCode.AppendLine("using System.Text.RegularExpressions;");
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
                
                // 生成参数解析器类
                GenerateParameterParserClass(generatedCode, parameterParserName, methodInfo);
                
                // 生成API处理器类
                GenerateHandlerClass(generatedCode, handlerName, methodInfo, controllerType, parameterParserName);
            }
            
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("}");
            
            // 写入文件
            var outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Editor", "GeneratedApiHandlers.cs");
            File.WriteAllText(outputPath, generatedCode.ToString(), Encoding.UTF8);
        }
        
        /// <summary>
        /// 生成参数解析器类
        /// </summary>
        private static void GenerateParameterParserClass(System.Text.StringBuilder sb, string className, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            
            // 读取模板
            var templatePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Editor", "ParameterParserTemplate.txt");
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"参数解析器模板文件不存在: {templatePath}");
                return;
            }
            
            string template = File.ReadAllText(templatePath, Encoding.UTF8);
            
            // 替换模板变量
            template = template.Replace("{{PARAMETER_PARSER_CLASS_NAME}}", className);
            template = template.Replace("{{CONTROLLER_NAME}}", methodInfo.DeclaringType.Name);
            template = template.Replace("{{METHOD_NAME}}", methodInfo.Name);
            template = template.Replace("{{PARAMETER_COUNT}}", parameters.Length.ToString());
            
            // 生成参数解析代码
            var paramParsingCode = new System.Text.StringBuilder();
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name;
                var paramType = GetCSTypeName(param.ParameterType);
                
                paramParsingCode.AppendLine($"                // 解析参数 {paramName} ({paramType})");
                
                // 从查询字符串中获取参数
                paramParsingCode.AppendLine($"                string paramValue_{i} = null;");
                paramParsingCode.AppendLine($"                if (request.QueryString[\"{paramName}\"] != null)");
                paramParsingCode.AppendLine($"                {{");
                paramParsingCode.AppendLine($"                    paramValue_{i} = request.QueryString[\"{paramName}\"];");
                paramParsingCode.AppendLine($"                }}");
                
                // 如果是POST请求，从请求体中获取参数
                if (methodInfo.GetCustomAttribute<ApiRouteAttribute>()?.Method == "POST")
                {
                    paramParsingCode.AppendLine($"                else if (request.HttpMethod == \"POST\" && request.ContentLength64 > 0)");
                    paramParsingCode.AppendLine($"                {{");
                    paramParsingCode.AppendLine($"                    // 读取POST数据");
                    paramParsingCode.AppendLine($"                    string body = \"\";");
                    paramParsingCode.AppendLine($"                    if (request.InputStream.CanSeek)");
                    paramParsingCode.AppendLine($"                    {{");
                    paramParsingCode.AppendLine($"                        // 如果流支持查找，保存当前位置");
                    paramParsingCode.AppendLine($"                        long originalPosition = request.InputStream.Position;");
                    paramParsingCode.AppendLine($"                        using (var reader = new StreamReader(request.InputStream))");
                    paramParsingCode.AppendLine($"                        {{");
                    paramParsingCode.AppendLine($"                            body = reader.ReadToEnd();");
                    paramParsingCode.AppendLine($"                        }}");
                    paramParsingCode.AppendLine($"                        // 恢复原始位置");
                    paramParsingCode.AppendLine($"                        request.InputStream.Position = originalPosition;");
                    paramParsingCode.AppendLine($"                    }}");
                    paramParsingCode.AppendLine($"                    else");
                    paramParsingCode.AppendLine($"                    {{");
                    paramParsingCode.AppendLine($"                        // 如果流不支持查找，重新读取");
                    paramParsingCode.AppendLine($"                        using (var memoryStream = new MemoryStream())");
                    paramParsingCode.AppendLine($"                        {{");
                    paramParsingCode.AppendLine($"                            request.InputStream.CopyTo(memoryStream);");
                    paramParsingCode.AppendLine($"                            body = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());");
                    paramParsingCode.AppendLine($"                        }}");
                    paramParsingCode.AppendLine($"                    }}");
                    paramParsingCode.AppendLine();
                    paramParsingCode.AppendLine($"                    // 解析JSON请求体中的 {paramName}");
                    paramParsingCode.AppendLine($"                    if (!string.IsNullOrEmpty(body) && body.Trim().StartsWith(\"{{\"))");
                    paramParsingCode.AppendLine($"                    {{");
                    paramParsingCode.AppendLine($"                        paramValue_{i} = ExtractValueFromJson(body, \"{paramName}\");");
                    paramParsingCode.AppendLine($"                    }}");
                    paramParsingCode.AppendLine($"                }}");
                }
                
                // 类型转换
                paramParsingCode.AppendLine($"                if (paramValue_{i} != null)");
                paramParsingCode.AppendLine($"                {{");
                
                if (paramType == "string")
                {
                    paramParsingCode.AppendLine($"                    parameters[{i}] = paramValue_{i};");
                }
                else if (paramType == "int")
                {
                    paramParsingCode.AppendLine($"                    parameters[{i}] = int.Parse(paramValue_{i});");
                }
                else if (paramType == "float")
                {
                    paramParsingCode.AppendLine($"                    parameters[{i}] = float.Parse(paramValue_{i});");
                }
                else if (paramType == "bool")
                {
                    paramParsingCode.AppendLine($"                    parameters[{i}] = bool.Parse(paramValue_{i});");
                }
                else
                {
                    paramParsingCode.AppendLine($"                    parameters[{i}] = paramValue_{i}; // 默认转换");
                }
                
                paramParsingCode.AppendLine($"                }}");
                paramParsingCode.AppendLine($"                else");
                paramParsingCode.AppendLine($"                {{");
                paramParsingCode.AppendLine($"                    // 如果没有找到参数值，使用默认值");
                if (param.DefaultValue != null)
                {
                    if (param.ParameterType == typeof(string))
                    {
                        if (param.DefaultValue == null)
                        {
                            paramParsingCode.AppendLine($"                    parameters[{i}] = null;");
                        }
                        else
                        {
                            paramParsingCode.AppendLine($"                    parameters[{i}] = \"{param.DefaultValue}\";");
                        }
                    }
                    else
                    {
                        paramParsingCode.AppendLine($"                    parameters[{i}] = {param.DefaultValue};");
                    }
                }
                else
                {
                    paramParsingCode.AppendLine($"                    parameters[{i}] = default({paramType});");
                }
                paramParsingCode.AppendLine($"                }}");
                paramParsingCode.AppendLine();
            }
            
            template = template.Replace("{{PARAMETER_PARSING_CODE}}", paramParsingCode.ToString());
            
            // 为模板中的每一行添加适当的缩进
            string indentedTemplate = "        " + template.Replace("\n", "\n        ");
            sb.AppendLine(indentedTemplate);
        }
        
        /// <summary>
        /// 生成API处理器类
        /// </summary>
        private static void GenerateHandlerClass(System.Text.StringBuilder sb, string className, MethodInfo methodInfo, Type controllerType, string parameterParserName)
        {
            // 读取模板
            var templatePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Editor", "ApiHandlerTemplate.txt");
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"API处理器模板文件不存在: {templatePath}");
                return;
            }
            
            string template = File.ReadAllText(templatePath, Encoding.UTF8);
            
            // 替换模板变量
            template = template.Replace("{{HANDLER_CLASS_NAME}}", className);
            template = template.Replace("{{PARAMETER_PARSER_CLASS_NAME}}", parameterParserName);
            template = template.Replace("{{CONTROLLER_NAME}}", methodInfo.DeclaringType.Name);
            template = template.Replace("{{METHOD_NAME}}", methodInfo.Name);
            template = template.Replace("{{FULL_CONTROLLER_NAME}}", controllerType.FullName.Replace("+", "."));
            template = template.Replace("{{PARAMETER_LIST}}", string.Join(", ", GenerateParameterList(methodInfo)));
            template = template.Replace("{{PARAMETER_CAST_LIST}}", string.Join(", ", GenerateParameterCastList(methodInfo)));
            
            // 为模板中的每一行添加适当的缩进
            string indentedTemplate = "        " + template.Replace("\n", "\n        ");
            sb.AppendLine(indentedTemplate);
        }
        
        /// <summary>
        /// 生成路由注册代码
        /// </summary>
        private static void GenerateRouteRegistrationCode(List<(string method, string path, MethodInfo methodInfo, Type controllerType)> routes)
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
            var routeRegistrationCode = new System.Text.StringBuilder();
            foreach (var (method, path, methodInfo, controllerType) in routes)
            {
                var handlerName = GenerateHandlerName(method, path, methodInfo);
                var parameterParserName = GenerateParameterParserName(method, path, methodInfo);
                
                routeRegistrationCode.AppendLine($"            apiServer.RegisterPredefinedRoute(\"{method}\", \"{path}\", ");
                routeRegistrationCode.AppendLine($"                new PredefinedApiHandlers.{handlerName}(), ");
                routeRegistrationCode.AppendLine($"                new PredefinedApiHandlers.{parameterParserName}(), ");
                routeRegistrationCode.AppendLine($"                \"{controllerType.Name}\", \"{methodInfo.Name}\");");
                routeRegistrationCode.AppendLine($"            ");
            }
            
            template = template.Replace("{{ROUTE_REGISTRATION_CODE}}", routeRegistrationCode.ToString());
            
            // 写入文件
            var outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Editor", "GeneratedRouteRegistrar.cs");
            File.WriteAllText(outputPath, template, Encoding.UTF8);
        }
        
        /// <summary>
        /// 生成处理器类名
        /// </summary>
        private static string GenerateHandlerName(string method, string path, MethodInfo methodInfo)
        {
            // 生成安全的类名，将特殊字符替换为下划线
            var cleanPath = new string(path.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            cleanPath = cleanPath.Replace("/", "_");
            return $"{method}{cleanPath}Handler";
        }
        
        /// <summary>
        /// 生成参数解析器类名
        /// </summary>
        private static string GenerateParameterParserName(string method, string path, MethodInfo methodInfo)
        {
            var cleanPath = new string(path.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            cleanPath = cleanPath.Replace("/", "_");
            return $"{method}{cleanPath}ParameterParser";
        }
        
        /// <summary>
        /// 获取C#类型名称
        /// </summary>
        private static string GetCSTypeName(System.Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(long)) return "long";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(object)) return "object";
            
            return type.FullName.Replace("+", ".");
        }
        
        /// <summary>
        /// 生成参数列表字符串
        /// </summary>
        private static string GenerateParameterList(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var paramNames = new List<string>();
            
            for (int i = 0; i < parameters.Length; i++)
            {
                paramNames.Add($"parameters[{i}]");
            }
            
            return string.Join(", ", paramNames);
        }
        
        /// <summary>
        /// 生成类型转换后的参数列表字符串
        /// </summary>
        private static string GenerateParameterCastList(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var paramNames = new List<string>();
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramType = GetCSTypeName(param.ParameterType);
                
                if (paramType == "string")
                {
                    if (param.DefaultValue == null)
                    {
                        paramNames.Add($"parameters[{i}] as string");
                    }
                    else
                    {
                        paramNames.Add($"parameters[{i}] as string ?? \"{param.DefaultValue}\"");
                    }
                }
                else if (paramType == "int")
                {
                    paramNames.Add($"parameters[{i}] != null ? (int)parameters[{i}] : {(param.DefaultValue != null ? param.DefaultValue : 0)}");
                }
                else if (paramType == "float")
                {
                    paramNames.Add($"parameters[{i}] != null ? (float)parameters[{i}] : {(param.DefaultValue != null ? param.DefaultValue : "0.0f")}");
                }
                else if (paramType == "bool")
                {
                    paramNames.Add($"parameters[{i}] != null ? (bool)parameters[{i}] : {(param.DefaultValue != null ? param.DefaultValue.ToString().ToLower() : "false")}");
                }
                else
                {
                    paramNames.Add($"parameters[{i}] != null ? ({paramType})parameters[{i}] : null");
                }
            }
            
            return string.Join(", ", paramNames);
        }
    }
    
    /// <summary>
    /// 自动代码生成工具菜单
    /// </summary>
    public class ApiCodeGenerationMenu
    {
        [MenuItem("Tools/Local REST API/生成API处理代码")]
        public static void GenerateApiCode()
        {
            ApiCodeGenerator.GenerateApiHandlerCode();
        }
    }
}