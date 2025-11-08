using System;
using System.Collections.Generic;
using System.Net;

namespace LocalRestAPI
{
    /// <summary>
    /// API处理器接口，用于处理HTTP请求而不使用反射
    /// </summary>
    public interface IApiHandler
    {
        /// <summary>
        /// 处理API请求
        /// </summary>
        /// <param name="request">HTTP请求</param>
        /// <param name="response">HTTP响应</param>
        /// <returns>是否成功处理</returns>
        bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response);
    }
    
    /// <summary>
    /// API参数解析器接口
    /// </summary>
    public interface IApiParameterParser
    {
        /// <summary>
        /// 从请求中解析参数
        /// </summary>
        /// <param name="request">HTTP请求</param>
        /// <returns>参数数组</returns>
        object[] ParseParameters(HttpListenerRequest request);
    }
    
    /// <summary>
    /// 预注册的API路由信息
    /// </summary>
    public class PredefinedApiRoute
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public IApiHandler Handler { get; set; }
        public IApiParameterParser ParameterParser { get; set; }
        public string ControllerName { get; set; }
        public string MethodName { get; set; }
        
        public PredefinedApiRoute(string method, string path, IApiHandler handler, IApiParameterParser parameterParser, string controllerName, string methodName)
        {
            Method = method;
            Path = path;
            Handler = handler;
            ParameterParser = parameterParser;
            ControllerName = controllerName;
            MethodName = methodName;
        }
    }
}