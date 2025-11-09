using System.Collections.Generic;
using LocalRestAPI.Runtime;
namespace LocalRestAPI
{
    /// <summary>
    /// 预定义的API路由注册器 - 自动从API控制器生成
    /// </summary>
    public static class PredefinedRouteRegistrar
    {
        /// <summary>
        /// 注册所有预定义的API路由
        /// </summary>
        /// <param name="apiServer">API服务器实例</param>
        public static void RegisterRoutes(ApiServer apiServer)
        {
            apiServer.RegisterRoute("GET", "/api/sample/hello", 
                new PredefinedApiHandlers.GETapisamplehelloHandler(), 
                new PredefinedApiHandlers.GETapisamplehelloParameterParser(), 
                "SampleController", "Hello");
            
            apiServer.RegisterRoute("POST", "/api/sample/echo", 
                new PredefinedApiHandlers.POSTapisampleechoHandler(), 
                new PredefinedApiHandlers.POSTapisampleechoParameterParser(), 
                "SampleController", "Echo");
            
            apiServer.RegisterRoute("GET", "/api/sample/random", 
                new PredefinedApiHandlers.GETapisamplerandomHandler(), 
                new PredefinedApiHandlers.GETapisamplerandomParameterParser(), 
                "SampleController", "GetRandom");
            
            apiServer.RegisterRoute("GET", "/api/sample/status", 
                new PredefinedApiHandlers.GETapisamplestatusHandler(), 
                new PredefinedApiHandlers.GETapisamplestatusParameterParser(), 
                "SampleController", "GetStatus");
            
            apiServer.RegisterRoute("GET", "/api/unity/scene", 
                new PredefinedApiHandlers.GETapiunitysceneHandler(), 
                new PredefinedApiHandlers.GETapiunitysceneParameterParser(), 
                "UnityController", "GetActiveScene");
            
            apiServer.RegisterRoute("GET", "/api/unity/objects", 
                new PredefinedApiHandlers.GETapiunityobjectsHandler(), 
                new PredefinedApiHandlers.GETapiunityobjectsParameterParser(), 
                "UnityController", "GetObjectsInScene");
            
            apiServer.RegisterRoute("POST", "/api/unity/log", 
                new PredefinedApiHandlers.POSTapiunitylogHandler(), 
                new PredefinedApiHandlers.POSTapiunitylogParameterParser(), 
                "UnityController", "LogMessage");
            

        }
    }
}