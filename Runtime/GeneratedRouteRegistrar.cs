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
                new PredefinedApiHandlers.GETapisamplehelloHandler_C4E22ACA(), 
                new PredefinedApiHandlers.GETapisamplehelloParameterParser_C4E22ACA(), 
                "SampleController", "Hello");

            apiServer.RegisterRoute("POST", "/api/sample/echo", 
                new PredefinedApiHandlers.POSTapisampleechoHandler_ED50351C(), 
                new PredefinedApiHandlers.POSTapisampleechoParameterParser_ED50351C(), 
                "SampleController", "Echo");

            apiServer.RegisterRoute("GET", "/api/sample/random", 
                new PredefinedApiHandlers.GETapisamplerandomHandler_5F57CD01(), 
                new PredefinedApiHandlers.GETapisamplerandomParameterParser_5F57CD01(), 
                "SampleController", "GetRandom");

            apiServer.RegisterRoute("GET", "/api/sample/status", 
                new PredefinedApiHandlers.GETapisamplestatusHandler_4089A289(), 
                new PredefinedApiHandlers.GETapisamplestatusParameterParser_4089A289(), 
                "SampleController", "GetStatus");

            apiServer.RegisterRoute("GET", "/api/unity/scene", 
                new PredefinedApiHandlers.GETapiunitysceneHandler_F164E5DB(), 
                new PredefinedApiHandlers.GETapiunitysceneParameterParser_F164E5DB(), 
                "UnityController", "GetActiveScene");

            apiServer.RegisterRoute("GET", "/api/unity/objects", 
                new PredefinedApiHandlers.GETapiunityobjectsHandler_28E21CDD(), 
                new PredefinedApiHandlers.GETapiunityobjectsParameterParser_28E21CDD(), 
                "UnityController", "GetObjectsInScene");

            apiServer.RegisterRoute("POST", "/api/unity/log", 
                new PredefinedApiHandlers.POSTapiunitylogHandler_43967BDA(), 
                new PredefinedApiHandlers.POSTapiunitylogParameterParser_43967BDA(), 
                "UnityController", "LogMessage");


        }
    }
}