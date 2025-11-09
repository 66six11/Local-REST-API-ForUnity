using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// API处理器类生成器，用于生成处理API请求的类
    /// </summary>
    public static class HandlerClassGenerator
    {
        /// <summary>
        /// 生成API处理器类
        /// </summary>
        /// <param name="sb">用于构建代码的StringBuilder</param>
        /// <param name="handlerName">处理器类名</param>
        /// <param name="methodInfo">API方法信息</param>
        /// <param name="controllerType">控制器类型</param>
        /// <param name="parameterParserName">参数解析器类名</param>
        /// <param name="needsMainThread">是否需要在主线程执行</param>
        public static void GenerateHandlerClass(
            StringBuilder sb,
            string handlerName,
            MethodInfo methodInfo,
            Type controllerType,
            string parameterParserName)
        {
            GenerateDirectHandlerClass(sb, handlerName, methodInfo, controllerType, parameterParserName);
        }

        /// <summary>
        /// 生成需要主线程执行的处理器类
        /// </summary>
        private static void GenerateThreadSafeHandlerClass(
            StringBuilder sb,
            string handlerName,
            MethodInfo methodInfo,
            Type controllerType,
            string parameterParserName)
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 处理 {methodInfo.DeclaringType.Name}.{methodInfo.Name} API请求");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public class {handlerName} : ThreadSafeApiHandler");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            public override bool NeedsMainThreadExecution()");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                return true;");
            sb.AppendLine($"            }}");
            sb.AppendLine($"");
            sb.AppendLine($"            protected override object ExecuteOnMainThread(HttpListenerRequest request, HttpListenerResponse response)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                try");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    // 使用预生成的参数解析器");
            sb.AppendLine($"                    var parser = new PredefinedApiHandlers.{parameterParserName}();");
            sb.AppendLine($"                    var parameters = parser.ParseParameters(request);");
            sb.AppendLine($"");
            sb.AppendLine($"                    // 直接创建控制器实例并调用方法");
            sb.AppendLine($"                    var controller = new {(controllerType.FullName.Replace("+", "."))}();");
            sb.AppendLine($"                    var result = controller.{methodInfo.Name}({string.Join(", ", ParameterParserGenerator.GenerateParameterCastList(methodInfo))});");
            sb.AppendLine($"");
            sb.AppendLine($"                    // 生成JSON响应");
            sb.AppendLine($"                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new {{ success = true }});");
            sb.AppendLine($"                    SendJsonResponse(response, jsonResponse, 200);");
            sb.AppendLine($"                    return result;");
            sb.AppendLine($"                }}");
            sb.AppendLine($"                catch (System.Exception ex)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    SendJsonResponse(response, $\"Method execution error: {{ex.Message}}\", 500);");
            sb.AppendLine($"                    return null;");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine($"");
            sb.AppendLine($"            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                response.StatusCode = statusCode;");
            sb.AppendLine($"                response.ContentType = \"application/json\";");
            sb.AppendLine($"");
            sb.AppendLine($"                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);");
            sb.AppendLine($"                response.ContentLength64 = buffer.Length;");
            sb.AppendLine($"");
            sb.AppendLine($"                response.OutputStream.Write(buffer, 0, buffer.Length);");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine($"");
        }

        /// <summary>
        /// 生成直接执行的处理器类（不需要主线程）
        /// </summary>
        private static void GenerateDirectHandlerClass(
            StringBuilder sb,
            string handlerName,
            MethodInfo methodInfo,
            Type controllerType,
            string parameterParserName)
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 处理 {methodInfo.DeclaringType.Name}.{methodInfo.Name} API请求");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public class {handlerName} : IApiHandler");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                try");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    // 使用预生成的参数解析器");
            sb.AppendLine($"                    var parser = new PredefinedApiHandlers.{parameterParserName}();");
            sb.AppendLine($"                    var parameters = parser.ParseParameters(request);");
            sb.AppendLine($"\n                    // 直接创建控制器实例并调用方法");
            sb.AppendLine($"                    var controller = new {(controllerType.FullName.Replace("+", "."))}();");
            sb.AppendLine($"                    var result = controller.{methodInfo.Name}({string.Join(", ", ParameterParserGenerator.GenerateParameterCastList(methodInfo))});");
            sb.AppendLine($"\n                    // 生成JSON响应");
            sb.AppendLine($"                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new {{ success = true }});");
            sb.AppendLine($"                    SendJsonResponse(response, jsonResponse, 200);");
            sb.AppendLine($"                    return true;");
            sb.AppendLine($"                }}");
            sb.AppendLine($"                catch (System.Exception ex)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    SendJsonResponse(response, $\"Method execution error: {{ex.Message}}\", 500);");
            sb.AppendLine($"                    return false;");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine($"\n            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                response.StatusCode = statusCode;");
            sb.AppendLine($"                response.ContentType = \"application/json\";");
            sb.AppendLine($"\n                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);");
            sb.AppendLine($"                response.ContentLength64 = buffer.Length;");
            sb.AppendLine($"\n                response.OutputStream.Write(buffer, 0, buffer.Length);");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine($"");
        }
    }
}