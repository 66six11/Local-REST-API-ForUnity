using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
                        var parameters = new object[1];
        
                        // 解析参数 name (string)
                        string paramValue_0 = null;
                        if (request.QueryString["name"] != null)
                        {
                            paramValue_0 = request.QueryString["name"];
                        }
                        if (paramValue_0 != null)
                        {
                            parameters[0] = paramValue_0;
                        }
                        else
                        {
                            // 如果没有找到参数值，使用默认值
                            parameters[0] = "World";
                        }
        
        
        
                        return parameters;
                    }
        
                    private string ExtractValueFromJson(string json, string paramName)
                    {
                        // 简单的JSON解析，查找指定参数名的值
                        // 格式: "paramName": "value" 或 "paramName":value
                        string escapedParamName = paramName.Replace("\"", "\\\"");
                        string pattern = $"\"{escapedParamName}\"\\s*:\\s*\"([^\"]*)\""; // 匹配字符串值
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (stringMatch.Success)
                        {
                            return stringMatch.Groups[1].Value;
                        }
        
                        // 尝试匹配非字符串值（数字、布尔值等）
                        pattern = $"\"{escapedParamName}\"\\s*:\\s*([^,}}]*)[\\s,}}]";
                        var valueMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (valueMatch.Success)
                        {
                            string value = valueMatch.Groups[1].Value.Trim();
                            // 移除可能的引号
                            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            return value;
                        }
        
                        return null;
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
                        var parameters = new object[1];
        
                        // 解析参数 message (string)
                        string paramValue_0 = null;
                        if (request.QueryString["message"] != null)
                        {
                            paramValue_0 = request.QueryString["message"];
                        }
                        else if (request.HttpMethod == "POST" && request.ContentLength64 > 0)
                        {
                            // 读取POST数据
                            string body = "";
                            if (request.InputStream.CanSeek)
                            {
                                // 如果流支持查找，保存当前位置
                                long originalPosition = request.InputStream.Position;
                                using (var reader = new StreamReader(request.InputStream))
                                {
                                    body = reader.ReadToEnd();
                                }
                                // 恢复原始位置
                                request.InputStream.Position = originalPosition;
                            }
                            else
                            {
                                // 如果流不支持查找，重新读取
                                using (var memoryStream = new MemoryStream())
                                {
                                    request.InputStream.CopyTo(memoryStream);
                                    body = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                                }
                            }
        
                            // 解析JSON请求体中的 message
                            if (!string.IsNullOrEmpty(body) && body.Trim().StartsWith("{"))
                            {
                                paramValue_0 = ExtractValueFromJson(body, "message");
                            }
                        }
                        if (paramValue_0 != null)
                        {
                            parameters[0] = paramValue_0;
                        }
                        else
                        {
                            // 如果没有找到参数值，使用默认值
                            parameters[0] = "";
                        }
        
        
        
                        return parameters;
                    }
        
                    private string ExtractValueFromJson(string json, string paramName)
                    {
                        // 简单的JSON解析，查找指定参数名的值
                        // 格式: "paramName": "value" 或 "paramName":value
                        string escapedParamName = paramName.Replace("\"", "\\\"");
                        string pattern = $"\"{escapedParamName}\"\\s*:\\s*\"([^\"]*)\""; // 匹配字符串值
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (stringMatch.Success)
                        {
                            return stringMatch.Groups[1].Value;
                        }
        
                        // 尝试匹配非字符串值（数字、布尔值等）
                        pattern = $"\"{escapedParamName}\"\\s*:\\s*([^,}}]*)[\\s,}}]";
                        var valueMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (valueMatch.Success)
                        {
                            string value = valueMatch.Groups[1].Value.Trim();
                            // 移除可能的引号
                            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            return value;
                        }
        
                        return null;
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
                            var result = controller.Echo(parameters[0] as string ?? "");
        
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
                        var parameters = new object[2];
        
                        // 解析参数 min (int)
                        string paramValue_0 = null;
                        if (request.QueryString["min"] != null)
                        {
                            paramValue_0 = request.QueryString["min"];
                        }
                        if (paramValue_0 != null)
                        {
                            parameters[0] = int.Parse(paramValue_0);
                        }
                        else
                        {
                            // 如果没有找到参数值，使用默认值
                            parameters[0] = 0;
                        }
        
                        // 解析参数 max (int)
                        string paramValue_1 = null;
                        if (request.QueryString["max"] != null)
                        {
                            paramValue_1 = request.QueryString["max"];
                        }
                        if (paramValue_1 != null)
                        {
                            parameters[1] = int.Parse(paramValue_1);
                        }
                        else
                        {
                            // 如果没有找到参数值，使用默认值
                            parameters[1] = 100;
                        }
        
        
        
                        return parameters;
                    }
        
                    private string ExtractValueFromJson(string json, string paramName)
                    {
                        // 简单的JSON解析，查找指定参数名的值
                        // 格式: "paramName": "value" 或 "paramName":value
                        string escapedParamName = paramName.Replace("\"", "\\\"");
                        string pattern = $"\"{escapedParamName}\"\\s*:\\s*\"([^\"]*)\""; // 匹配字符串值
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (stringMatch.Success)
                        {
                            return stringMatch.Groups[1].Value;
                        }
        
                        // 尝试匹配非字符串值（数字、布尔值等）
                        pattern = $"\"{escapedParamName}\"\\s*:\\s*([^,}}]*)[\\s,}}]";
                        var valueMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (valueMatch.Success)
                        {
                            string value = valueMatch.Groups[1].Value.Trim();
                            // 移除可能的引号
                            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            return value;
                        }
        
                        return null;
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
                        var parameters = new object[0];
        
        
        
                        return parameters;
                    }
        
                    private string ExtractValueFromJson(string json, string paramName)
                    {
                        // 简单的JSON解析，查找指定参数名的值
                        // 格式: "paramName": "value" 或 "paramName":value
                        string escapedParamName = paramName.Replace("\"", "\\\"");
                        string pattern = $"\"{escapedParamName}\"\\s*:\\s*\"([^\"]*)\""; // 匹配字符串值
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (stringMatch.Success)
                        {
                            return stringMatch.Groups[1].Value;
                        }
        
                        // 尝试匹配非字符串值（数字、布尔值等）
                        pattern = $"\"{escapedParamName}\"\\s*:\\s*([^,}}]*)[\\s,}}]";
                        var valueMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (valueMatch.Success)
                        {
                            string value = valueMatch.Groups[1].Value.Trim();
                            // 移除可能的引号
                            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            return value;
                        }
        
                        return null;
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
                        var parameters = new object[0];
        
        
        
                        return parameters;
                    }
        
                    private string ExtractValueFromJson(string json, string paramName)
                    {
                        // 简单的JSON解析，查找指定参数名的值
                        // 格式: "paramName": "value" 或 "paramName":value
                        string escapedParamName = paramName.Replace("\"", "\\\"");
                        string pattern = $"\"{escapedParamName}\"\\s*:\\s*\"([^\"]*)\""; // 匹配字符串值
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (stringMatch.Success)
                        {
                            return stringMatch.Groups[1].Value;
                        }
        
                        // 尝试匹配非字符串值（数字、布尔值等）
                        pattern = $"\"{escapedParamName}\"\\s*:\\s*([^,}}]*)[\\s,}}]";
                        var valueMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (valueMatch.Success)
                        {
                            string value = valueMatch.Groups[1].Value.Trim();
                            // 移除可能的引号
                            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            return value;
                        }
        
                        return null;
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
                        var parameters = new object[0];
        
        
        
                        return parameters;
                    }
        
                    private string ExtractValueFromJson(string json, string paramName)
                    {
                        // 简单的JSON解析，查找指定参数名的值
                        // 格式: "paramName": "value" 或 "paramName":value
                        string escapedParamName = paramName.Replace("\"", "\\\"");
                        string pattern = $"\"{escapedParamName}\"\\s*:\\s*\"([^\"]*)\""; // 匹配字符串值
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (stringMatch.Success)
                        {
                            return stringMatch.Groups[1].Value;
                        }
        
                        // 尝试匹配非字符串值（数字、布尔值等）
                        pattern = $"\"{escapedParamName}\"\\s*:\\s*([^,}}]*)[\\s,}}]";
                        var valueMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (valueMatch.Success)
                        {
                            string value = valueMatch.Groups[1].Value.Trim();
                            // 移除可能的引号
                            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            return value;
                        }
        
                        return null;
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
                        var parameters = new object[2];
        
                        // 解析参数 message (string)
                        string paramValue_0 = null;
                        if (request.QueryString["message"] != null)
                        {
                            paramValue_0 = request.QueryString["message"];
                        }
                        else if (request.HttpMethod == "POST" && request.ContentLength64 > 0)
                        {
                            // 读取POST数据
                            string body = "";
                            if (request.InputStream.CanSeek)
                            {
                                // 如果流支持查找，保存当前位置
                                long originalPosition = request.InputStream.Position;
                                using (var reader = new StreamReader(request.InputStream))
                                {
                                    body = reader.ReadToEnd();
                                }
                                // 恢复原始位置
                                request.InputStream.Position = originalPosition;
                            }
                            else
                            {
                                // 如果流不支持查找，重新读取
                                using (var memoryStream = new MemoryStream())
                                {
                                    request.InputStream.CopyTo(memoryStream);
                                    body = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                                }
                            }
        
                            // 解析JSON请求体中的 message
                            if (!string.IsNullOrEmpty(body) && body.Trim().StartsWith("{"))
                            {
                                paramValue_0 = ExtractValueFromJson(body, "message");
                            }
                        }
                        if (paramValue_0 != null)
                        {
                            parameters[0] = paramValue_0;
                        }
                        else
                        {
                            // 如果没有找到参数值，使用默认值
                            parameters[0] = "";
                        }
        
                        // 解析参数 type (string)
                        string paramValue_1 = null;
                        if (request.QueryString["type"] != null)
                        {
                            paramValue_1 = request.QueryString["type"];
                        }
                        else if (request.HttpMethod == "POST" && request.ContentLength64 > 0)
                        {
                            // 读取POST数据
                            string body = "";
                            if (request.InputStream.CanSeek)
                            {
                                // 如果流支持查找，保存当前位置
                                long originalPosition = request.InputStream.Position;
                                using (var reader = new StreamReader(request.InputStream))
                                {
                                    body = reader.ReadToEnd();
                                }
                                // 恢复原始位置
                                request.InputStream.Position = originalPosition;
                            }
                            else
                            {
                                // 如果流不支持查找，重新读取
                                using (var memoryStream = new MemoryStream())
                                {
                                    request.InputStream.CopyTo(memoryStream);
                                    body = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                                }
                            }
        
                            // 解析JSON请求体中的 type
                            if (!string.IsNullOrEmpty(body) && body.Trim().StartsWith("{"))
                            {
                                paramValue_1 = ExtractValueFromJson(body, "type");
                            }
                        }
                        if (paramValue_1 != null)
                        {
                            parameters[1] = paramValue_1;
                        }
                        else
                        {
                            // 如果没有找到参数值，使用默认值
                            parameters[1] = "info";
                        }
        
        
        
                        return parameters;
                    }
        
                    private string ExtractValueFromJson(string json, string paramName)
                    {
                        // 简单的JSON解析，查找指定参数名的值
                        // 格式: "paramName": "value" 或 "paramName":value
                        string escapedParamName = paramName.Replace("\"", "\\\"");
                        string pattern = $"\"{escapedParamName}\"\\s*:\\s*\"([^\"]*)\""; // 匹配字符串值
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (stringMatch.Success)
                        {
                            return stringMatch.Groups[1].Value;
                        }
        
                        // 尝试匹配非字符串值（数字、布尔值等）
                        pattern = $"\"{escapedParamName}\"\\s*:\\s*([^,}}]*)[\\s,}}]";
                        var valueMatch = System.Text.RegularExpressions.Regex.Match(json, pattern);
        
                        if (valueMatch.Success)
                        {
                            string value = valueMatch.Groups[1].Value.Trim();
                            // 移除可能的引号
                            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            return value;
                        }
        
                        return null;
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
                            var result = controller.LogMessage(parameters[0] as string ?? "", parameters[1] as string ?? "info");
        
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
