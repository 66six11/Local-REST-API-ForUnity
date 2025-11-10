using System;
using System.Net;

namespace LocalRestAPI.Runtime
{
    public class ApiRoute
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public IApiHandler Handler { get; set; }
        public IApiParameterParser ParameterParser { get; set; }
        public string ControllerName { get; set; }
        public string MethodName { get; set; }


        public ApiRoute(string method, string path, IApiHandler handler, IApiParameterParser parameterParser, string controllerName, string methodName)
        {
            Method = method;
            Path = path;
            Handler = handler;
            ParameterParser = parameterParser;
            ControllerName = controllerName;
            MethodName = methodName;
        }
    }

    [Serializable]
    public class RouteInfo

    {
        public string method;

        public string path;

        public string handler;
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


    public interface IApiHandler
    {
        /// <summary>
        /// 处理API请求
        /// </summary>
        /// <param name="request">HTTP请求</param>
        /// <param name="buffer">响应缓存</param>
        /// <returns>是否成功处理</returns>
        bool HandleRequest(HttpListenerRequest request, ResponseBuffer buffer);
    }


}