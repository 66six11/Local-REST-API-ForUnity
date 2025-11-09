using System;
using System.Net;
using System.IO;
using System.Text;
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
/// LocalRestAPI.SampleController.Hello 方法的参数解析器（统一调用共享解析器）
/// 说明：
/// - 优先从 QueryString 取参数，其次在 POST/PUT/PATCH 时从 JSON/Form Body 取参数。
/// - 实际解析委托给 LocalRestAPI.ApiParameterParser（基于 Newtonsoft.Json），避免重复实现与正则问题。
/// - 通过反射读取 LocalRestAPI.SampleController.Hello 的参数签名，避免模板展开错误。
/// </summary>
public class GETapisamplehelloParameterParser_C4E22ACA : IApiParameterParser
{
    public object[] ParseParameters(HttpListenerRequest request)
    {
        var controllerType = System.Type.GetType("LocalRestAPI.SampleController");
        if (controllerType == null)
            throw new System.Exception($"Parameter parser: 未找到控制器类型 LocalRestAPI.SampleController");

        var method = controllerType.GetMethod("Hello", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
            throw new System.Exception($"Parameter parser: 在 LocalRestAPI.SampleController 未找到方法 Hello");

        var paramInfos = method.GetParameters();
        var paramTypes = new System.Type[paramInfos.Length];
        var paramNames = new string[paramInfos.Length];
        object[] paramDefaults = paramInfos.Length > 0 ? new object[paramInfos.Length] : null;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            paramTypes[i] = p.ParameterType;
            paramNames[i] = p.Name;
            if (paramDefaults != null)
                paramDefaults[i] = p.HasDefaultValue ? p.DefaultValue : null;
        }

        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            paramTypes,
            paramNames,
            paramDefaults
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.Hello API请求
/// </summary>
public class GETapisamplehelloHandler_C4E22ACA : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapisamplehelloParameterParser_C4E22ACA();
            var parameters = parser.ParseParameters(request);

            // 直接创建控制器实例并调用方法
            var controller = new LocalRestAPI.SampleController();
            var result = controller.Hello(parameters[0] as string);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
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

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content ?? "{}");
        response.ContentLength64 = buffer.Length;

        using (var os = response.OutputStream)
        {
            os.Write(buffer, 0, buffer.Length);
        }
    }
}

/// <summary>
/// LocalRestAPI.SampleController.Echo 方法的参数解析器（统一调用共享解析器）
/// 说明：
/// - 优先从 QueryString 取参数，其次在 POST/PUT/PATCH 时从 JSON/Form Body 取参数。
/// - 实际解析委托给 LocalRestAPI.ApiParameterParser（基于 Newtonsoft.Json），避免重复实现与正则问题。
/// - 通过反射读取 LocalRestAPI.SampleController.Echo 的参数签名，避免模板展开错误。
/// </summary>
public class POSTapisampleechoParameterParser_ED50351C : IApiParameterParser
{
    public object[] ParseParameters(HttpListenerRequest request)
    {
        var controllerType = System.Type.GetType("LocalRestAPI.SampleController");
        if (controllerType == null)
            throw new System.Exception($"Parameter parser: 未找到控制器类型 LocalRestAPI.SampleController");

        var method = controllerType.GetMethod("Echo", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
            throw new System.Exception($"Parameter parser: 在 LocalRestAPI.SampleController 未找到方法 Echo");

        var paramInfos = method.GetParameters();
        var paramTypes = new System.Type[paramInfos.Length];
        var paramNames = new string[paramInfos.Length];
        object[] paramDefaults = paramInfos.Length > 0 ? new object[paramInfos.Length] : null;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            paramTypes[i] = p.ParameterType;
            paramNames[i] = p.Name;
            if (paramDefaults != null)
                paramDefaults[i] = p.HasDefaultValue ? p.DefaultValue : null;
        }

        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            paramTypes,
            paramNames,
            paramDefaults
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.Echo API请求
/// </summary>
public class POSTapisampleechoHandler_ED50351C : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.POSTapisampleechoParameterParser_ED50351C();
            var parameters = parser.ParseParameters(request);

            // 直接创建控制器实例并调用方法
            var controller = new LocalRestAPI.SampleController();
            var result = controller.Echo(parameters[0] as string);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
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

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content ?? "{}");
        response.ContentLength64 = buffer.Length;

        using (var os = response.OutputStream)
        {
            os.Write(buffer, 0, buffer.Length);
        }
    }
}

/// <summary>
/// LocalRestAPI.SampleController.GetRandom 方法的参数解析器（统一调用共享解析器）
/// 说明：
/// - 优先从 QueryString 取参数，其次在 POST/PUT/PATCH 时从 JSON/Form Body 取参数。
/// - 实际解析委托给 LocalRestAPI.ApiParameterParser（基于 Newtonsoft.Json），避免重复实现与正则问题。
/// - 通过反射读取 LocalRestAPI.SampleController.GetRandom 的参数签名，避免模板展开错误。
/// </summary>
public class GETapisamplerandomParameterParser_5F57CD01 : IApiParameterParser
{
    public object[] ParseParameters(HttpListenerRequest request)
    {
        var controllerType = System.Type.GetType("LocalRestAPI.SampleController");
        if (controllerType == null)
            throw new System.Exception($"Parameter parser: 未找到控制器类型 LocalRestAPI.SampleController");

        var method = controllerType.GetMethod("GetRandom", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
            throw new System.Exception($"Parameter parser: 在 LocalRestAPI.SampleController 未找到方法 GetRandom");

        var paramInfos = method.GetParameters();
        var paramTypes = new System.Type[paramInfos.Length];
        var paramNames = new string[paramInfos.Length];
        object[] paramDefaults = paramInfos.Length > 0 ? new object[paramInfos.Length] : null;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            paramTypes[i] = p.ParameterType;
            paramNames[i] = p.Name;
            if (paramDefaults != null)
                paramDefaults[i] = p.HasDefaultValue ? p.DefaultValue : null;
        }

        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            paramTypes,
            paramNames,
            paramDefaults
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.GetRandom API请求
/// </summary>
public class GETapisamplerandomHandler_5F57CD01 : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapisamplerandomParameterParser_5F57CD01();
            var parameters = parser.ParseParameters(request);

            // 直接创建控制器实例并调用方法
            var controller = new LocalRestAPI.SampleController();
            var result = controller.GetRandom((System.Int32)parameters[0], (System.Int32)parameters[1]);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
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

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content ?? "{}");
        response.ContentLength64 = buffer.Length;

        using (var os = response.OutputStream)
        {
            os.Write(buffer, 0, buffer.Length);
        }
    }
}

/// <summary>
/// LocalRestAPI.SampleController.GetStatus 方法的参数解析器（统一调用共享解析器）
/// 说明：
/// - 优先从 QueryString 取参数，其次在 POST/PUT/PATCH 时从 JSON/Form Body 取参数。
/// - 实际解析委托给 LocalRestAPI.ApiParameterParser（基于 Newtonsoft.Json），避免重复实现与正则问题。
/// - 通过反射读取 LocalRestAPI.SampleController.GetStatus 的参数签名，避免模板展开错误。
/// </summary>
public class GETapisamplestatusParameterParser_4089A289 : IApiParameterParser
{
    public object[] ParseParameters(HttpListenerRequest request)
    {
        var controllerType = System.Type.GetType("LocalRestAPI.SampleController");
        if (controllerType == null)
            throw new System.Exception($"Parameter parser: 未找到控制器类型 LocalRestAPI.SampleController");

        var method = controllerType.GetMethod("GetStatus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
            throw new System.Exception($"Parameter parser: 在 LocalRestAPI.SampleController 未找到方法 GetStatus");

        var paramInfos = method.GetParameters();
        var paramTypes = new System.Type[paramInfos.Length];
        var paramNames = new string[paramInfos.Length];
        object[] paramDefaults = paramInfos.Length > 0 ? new object[paramInfos.Length] : null;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            paramTypes[i] = p.ParameterType;
            paramNames[i] = p.Name;
            if (paramDefaults != null)
                paramDefaults[i] = p.HasDefaultValue ? p.DefaultValue : null;
        }

        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            paramTypes,
            paramNames,
            paramDefaults
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.GetStatus API请求
/// </summary>
public class GETapisamplestatusHandler_4089A289 : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapisamplestatusParameterParser_4089A289();
            var parameters = parser.ParseParameters(request);

            // 直接创建控制器实例并调用方法
            var controller = new LocalRestAPI.SampleController();
            var result = controller.GetStatus();

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
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

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content ?? "{}");
        response.ContentLength64 = buffer.Length;

        using (var os = response.OutputStream)
        {
            os.Write(buffer, 0, buffer.Length);
        }
    }
}

/// <summary>
/// LocalRestAPI.UnityController.GetActiveScene 方法的参数解析器（统一调用共享解析器）
/// 说明：
/// - 优先从 QueryString 取参数，其次在 POST/PUT/PATCH 时从 JSON/Form Body 取参数。
/// - 实际解析委托给 LocalRestAPI.ApiParameterParser（基于 Newtonsoft.Json），避免重复实现与正则问题。
/// - 通过反射读取 LocalRestAPI.UnityController.GetActiveScene 的参数签名，避免模板展开错误。
/// </summary>
public class GETapiunitysceneParameterParser_F164E5DB : IApiParameterParser
{
    public object[] ParseParameters(HttpListenerRequest request)
    {
        var controllerType = System.Type.GetType("LocalRestAPI.UnityController");
        if (controllerType == null)
            throw new System.Exception($"Parameter parser: 未找到控制器类型 LocalRestAPI.UnityController");

        var method = controllerType.GetMethod("GetActiveScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
            throw new System.Exception($"Parameter parser: 在 LocalRestAPI.UnityController 未找到方法 GetActiveScene");

        var paramInfos = method.GetParameters();
        var paramTypes = new System.Type[paramInfos.Length];
        var paramNames = new string[paramInfos.Length];
        object[] paramDefaults = paramInfos.Length > 0 ? new object[paramInfos.Length] : null;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            paramTypes[i] = p.ParameterType;
            paramNames[i] = p.Name;
            if (paramDefaults != null)
                paramDefaults[i] = p.HasDefaultValue ? p.DefaultValue : null;
        }

        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            paramTypes,
            paramNames,
            paramDefaults
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.UnityController.GetActiveScene API请求
/// </summary>
public class GETapiunitysceneHandler_F164E5DB : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapiunitysceneParameterParser_F164E5DB();
            var parameters = parser.ParseParameters(request);

            // 直接创建控制器实例并调用方法
            var controller = new LocalRestAPI.UnityController();
            var result = controller.GetActiveScene();

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
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

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content ?? "{}");
        response.ContentLength64 = buffer.Length;

        using (var os = response.OutputStream)
        {
            os.Write(buffer, 0, buffer.Length);
        }
    }
}

/// <summary>
/// LocalRestAPI.UnityController.GetObjectsInScene 方法的参数解析器（统一调用共享解析器）
/// 说明：
/// - 优先从 QueryString 取参数，其次在 POST/PUT/PATCH 时从 JSON/Form Body 取参数。
/// - 实际解析委托给 LocalRestAPI.ApiParameterParser（基于 Newtonsoft.Json），避免重复实现与正则问题。
/// - 通过反射读取 LocalRestAPI.UnityController.GetObjectsInScene 的参数签名，避免模板展开错误。
/// </summary>
public class GETapiunityobjectsParameterParser_28E21CDD : IApiParameterParser
{
    public object[] ParseParameters(HttpListenerRequest request)
    {
        var controllerType = System.Type.GetType("LocalRestAPI.UnityController");
        if (controllerType == null)
            throw new System.Exception($"Parameter parser: 未找到控制器类型 LocalRestAPI.UnityController");

        var method = controllerType.GetMethod("GetObjectsInScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
            throw new System.Exception($"Parameter parser: 在 LocalRestAPI.UnityController 未找到方法 GetObjectsInScene");

        var paramInfos = method.GetParameters();
        var paramTypes = new System.Type[paramInfos.Length];
        var paramNames = new string[paramInfos.Length];
        object[] paramDefaults = paramInfos.Length > 0 ? new object[paramInfos.Length] : null;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            paramTypes[i] = p.ParameterType;
            paramNames[i] = p.Name;
            if (paramDefaults != null)
                paramDefaults[i] = p.HasDefaultValue ? p.DefaultValue : null;
        }

        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            paramTypes,
            paramNames,
            paramDefaults
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.UnityController.GetObjectsInScene API请求
/// </summary>
public class GETapiunityobjectsHandler_28E21CDD : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapiunityobjectsParameterParser_28E21CDD();
            var parameters = parser.ParseParameters(request);

            // 直接创建控制器实例并调用方法
            var controller = new LocalRestAPI.UnityController();
            var result = controller.GetObjectsInScene();

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
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

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content ?? "{}");
        response.ContentLength64 = buffer.Length;

        using (var os = response.OutputStream)
        {
            os.Write(buffer, 0, buffer.Length);
        }
    }
}

/// <summary>
/// LocalRestAPI.UnityController.LogMessage 方法的参数解析器（统一调用共享解析器）
/// 说明：
/// - 优先从 QueryString 取参数，其次在 POST/PUT/PATCH 时从 JSON/Form Body 取参数。
/// - 实际解析委托给 LocalRestAPI.ApiParameterParser（基于 Newtonsoft.Json），避免重复实现与正则问题。
/// - 通过反射读取 LocalRestAPI.UnityController.LogMessage 的参数签名，避免模板展开错误。
/// </summary>
public class POSTapiunitylogParameterParser_43967BDA : IApiParameterParser
{
    public object[] ParseParameters(HttpListenerRequest request)
    {
        var controllerType = System.Type.GetType("LocalRestAPI.UnityController");
        if (controllerType == null)
            throw new System.Exception($"Parameter parser: 未找到控制器类型 LocalRestAPI.UnityController");

        var method = controllerType.GetMethod("LogMessage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
            throw new System.Exception($"Parameter parser: 在 LocalRestAPI.UnityController 未找到方法 LogMessage");

        var paramInfos = method.GetParameters();
        var paramTypes = new System.Type[paramInfos.Length];
        var paramNames = new string[paramInfos.Length];
        object[] paramDefaults = paramInfos.Length > 0 ? new object[paramInfos.Length] : null;

        for (int i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            paramTypes[i] = p.ParameterType;
            paramNames[i] = p.Name;
            if (paramDefaults != null)
                paramDefaults[i] = p.HasDefaultValue ? p.DefaultValue : null;
        }

        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            paramTypes,
            paramNames,
            paramDefaults
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.UnityController.LogMessage API请求
/// </summary>
public class POSTapiunitylogHandler_43967BDA : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.POSTapiunitylogParameterParser_43967BDA();
            var parameters = parser.ParseParameters(request);

            // 直接创建控制器实例并调用方法
            var controller = new LocalRestAPI.UnityController();
            var result = controller.LogMessage(parameters[0] as string, parameters[1] as string);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
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

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content ?? "{}");
        response.ContentLength64 = buffer.Length;

        using (var os = response.OutputStream)
        {
            os.Write(buffer, 0, buffer.Length);
        }
    }
}

    }
}
