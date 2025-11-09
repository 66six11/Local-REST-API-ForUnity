using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LocalRestAPI.Runtime;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// 预生成的API处理器类 - 自动从API控制器生成
    /// </summary>
    public static class PredefinedApiHandlers
    {
        /// <summary>
        /// SampleController.Hello 方法的参数解析器
        /// </summary>
        public class GETapisamplehelloParameterParser : IApiParameterParser
        {
            public object[] ParseParameters(HttpListenerRequest request)
            {
                // 准备参数信息
                var paramTypes = new System.Type[] { typeof(string) };
                var paramNames = new string[] { "name" };
                var paramDefaultValues = new object[] { "World" };

                // 使用公共参数解析器解析参数
                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);
            }
        }

        /// <summary>
        /// 处理 SampleController.Hello API请求
        /// </summary>
        public class GETapisamplehelloHandler : IApiHandler
        {
            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
            {
                try
                {
                    // 使用预生成的参数解析器
                    var parser = new PredefinedApiHandlers.GETapisamplehelloParameterParser();
                    var parameters = parser.ParseParameters(request);

                    // 直接创建控制器实例并调用方法
                    var controller = new LocalRestAPI.SampleController();
                    var result = controller.Hello(parameters[0] as string ?? "World");

                    // 生成JSON响应
                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new { success = true });
                    SendJsonResponse(response, jsonResponse, 200);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SendJsonResponse(response, $"Method execution error: {ex.Message}", 500);
                    return false;
                }
            }

            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// SampleController.Echo 方法的参数解析器
        /// </summary>
        public class POSTapisampleechoParameterParser : IApiParameterParser
        {
            public object[] ParseParameters(HttpListenerRequest request)
            {
                // 准备参数信息
                var paramTypes = new System.Type[] { typeof(string) };
                var paramNames = new string[] { "message" };
                var paramDefaultValues = new object[] { null };

                // 使用公共参数解析器解析参数
                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);
            }
        }

        /// <summary>
        /// 处理 SampleController.Echo API请求
        /// </summary>
        public class POSTapisampleechoHandler : IApiHandler
        {
            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
            {
                try
                {
                    // 使用预生成的参数解析器
                    var parser = new PredefinedApiHandlers.POSTapisampleechoParameterParser();
                    var parameters = parser.ParseParameters(request);

                    // 直接创建控制器实例并调用方法
                    var controller = new LocalRestAPI.SampleController();
                    var result = controller.Echo(parameters[0] as string);

                    // 生成JSON响应
                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new { success = true });
                    SendJsonResponse(response, jsonResponse, 200);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SendJsonResponse(response, $"Method execution error: {ex.Message}", 500);
                    return false;
                }
            }

            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// SampleController.GetRandom 方法的参数解析器
        /// </summary>
        public class GETapisamplerandomParameterParser : IApiParameterParser
        {
            public object[] ParseParameters(HttpListenerRequest request)
            {
                // 准备参数信息
                var paramTypes = new System.Type[] { typeof(int), typeof(int) };
                var paramNames = new string[] { "min", "max" };
                var paramDefaultValues = new object[] { 0, 100 };

                // 使用公共参数解析器解析参数
                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);
            }
        }

        /// <summary>
        /// 处理 SampleController.GetRandom API请求
        /// </summary>
        public class GETapisamplerandomHandler : IApiHandler
        {
            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
            {
                try
                {
                    // 使用预生成的参数解析器
                    var parser = new PredefinedApiHandlers.GETapisamplerandomParameterParser();
                    var parameters = parser.ParseParameters(request);

                    // 直接创建控制器实例并调用方法
                    var controller = new LocalRestAPI.SampleController();
                    var result = controller.GetRandom(parameters[0] != null ? (int)parameters[0] : 0, parameters[1] != null ? (int)parameters[1] : 100);

                    // 生成JSON响应
                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new { success = true });
                    SendJsonResponse(response, jsonResponse, 200);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SendJsonResponse(response, $"Method execution error: {ex.Message}", 500);
                    return false;
                }
            }

            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// SampleController.GetStatus 方法的参数解析器
        /// </summary>
        public class GETapisamplestatusParameterParser : IApiParameterParser
        {
            public object[] ParseParameters(HttpListenerRequest request)
            {
                // 准备参数信息
                var paramTypes = new System.Type[] {  };
                var paramNames = new string[] {  };
                var paramDefaultValues = new object[] {  };

                // 使用公共参数解析器解析参数
                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);
            }
        }

        /// <summary>
        /// 处理 SampleController.GetStatus API请求
        /// </summary>
        public class GETapisamplestatusHandler : IApiHandler
        {
            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
            {
                try
                {
                    // 使用预生成的参数解析器
                    var parser = new PredefinedApiHandlers.GETapisamplestatusParameterParser();
                    var parameters = parser.ParseParameters(request);

                    // 直接创建控制器实例并调用方法
                    var controller = new LocalRestAPI.SampleController();
                    var result = controller.GetStatus();

                    // 生成JSON响应
                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new { success = true });
                    SendJsonResponse(response, jsonResponse, 200);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SendJsonResponse(response, $"Method execution error: {ex.Message}", 500);
                    return false;
                }
            }

            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// UnityController.GetActiveScene 方法的参数解析器
        /// </summary>
        public class GETapiunitysceneParameterParser : IApiParameterParser
        {
            public object[] ParseParameters(HttpListenerRequest request)
            {
                // 准备参数信息
                var paramTypes = new System.Type[] {  };
                var paramNames = new string[] {  };
                var paramDefaultValues = new object[] {  };

                // 使用公共参数解析器解析参数
                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);
            }
        }

        /// <summary>
        /// 处理 UnityController.GetActiveScene API请求
        /// </summary>
        public class GETapiunitysceneHandler : IApiHandler
        {
            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
            {
                try
                {
                    // 使用预生成的参数解析器
                    var parser = new PredefinedApiHandlers.GETapiunitysceneParameterParser();
                    var parameters = parser.ParseParameters(request);

                    // 直接创建控制器实例并调用方法
                    var controller = new LocalRestAPI.UnityController();
                    var result = controller.GetActiveScene();

                    // 生成JSON响应
                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new { success = true });
                    SendJsonResponse(response, jsonResponse, 200);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SendJsonResponse(response, $"Method execution error: {ex.Message}", 500);
                    return false;
                }
            }

            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// UnityController.GetObjectsInScene 方法的参数解析器
        /// </summary>
        public class GETapiunityobjectsParameterParser : IApiParameterParser
        {
            public object[] ParseParameters(HttpListenerRequest request)
            {
                // 准备参数信息
                var paramTypes = new System.Type[] {  };
                var paramNames = new string[] {  };
                var paramDefaultValues = new object[] {  };

                // 使用公共参数解析器解析参数
                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);
            }
        }

        /// <summary>
        /// 处理 UnityController.GetObjectsInScene API请求
        /// </summary>
        public class GETapiunityobjectsHandler : IApiHandler
        {
            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
            {
                try
                {
                    // 使用预生成的参数解析器
                    var parser = new PredefinedApiHandlers.GETapiunityobjectsParameterParser();
                    var parameters = parser.ParseParameters(request);

                    // 直接创建控制器实例并调用方法
                    var controller = new LocalRestAPI.UnityController();
                    var result = controller.GetObjectsInScene();

                    // 生成JSON响应
                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new { success = true });
                    SendJsonResponse(response, jsonResponse, 200);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SendJsonResponse(response, $"Method execution error: {ex.Message}", 500);
                    return false;
                }
            }

            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// UnityController.LogMessage 方法的参数解析器
        /// </summary>
        public class POSTapiunitylogParameterParser : IApiParameterParser
        {
            public object[] ParseParameters(HttpListenerRequest request)
            {
                // 准备参数信息
                var paramTypes = new System.Type[] { typeof(string), typeof(string) };
                var paramNames = new string[] { "message", "type" };
                var paramDefaultValues = new object[] { null, "info" };

                // 使用公共参数解析器解析参数
                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);
            }
        }

        /// <summary>
        /// 处理 UnityController.LogMessage API请求
        /// </summary>
        public class POSTapiunitylogHandler : IApiHandler
        {
            public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
            {
                try
                {
                    // 使用预生成的参数解析器
                    var parser = new PredefinedApiHandlers.POSTapiunitylogParameterParser();
                    var parameters = parser.ParseParameters(request);

                    // 直接创建控制器实例并调用方法
                    var controller = new LocalRestAPI.UnityController();
                    var result = controller.LogMessage(parameters[0] as string, parameters[1] as string ?? "info");

                    // 生成JSON响应
                    string jsonResponse = result != null ? JsonUtility.ToJson(result) : JsonUtility.ToJson(new { success = true });
                    SendJsonResponse(response, jsonResponse, 200);
                    return true;
                }
                catch (System.Exception ex)
                {
                    SendJsonResponse(response, $"Method execution error: {ex.Message}", 500);
                    return false;
                }
            }

            private void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

    }
}
