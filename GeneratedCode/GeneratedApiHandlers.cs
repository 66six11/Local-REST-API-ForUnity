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
/// LocalRestAPI.SampleController.Hello 方法的参数解析器（编译期写死参数元数据）
/// 说明：
/// - 生成阶段直接把参数类型 / 名称 / 默认值写入静态数组，Parse 时不再做反射。
/// - 仍然委托统一的 ApiParameterParser 做 QueryString / JSON / Form 解析与类型转换。
/// </summary>
public class GETapisamplehelloParameterParser_C4E22ACA : IApiParameterParser
{
    // 参数类型数组（与方法参数顺序一致）
    private static readonly System.Type[] ParamTypes = new System.Type[]
    {
        typeof(System.String) // name
    };

    // 参数名称数组
    private static readonly string[] ParamNames = new string[]
    {
        "name"
    };

    // 参数默认值数组（无默认值用 null）
    private static readonly object[] ParamDefaultValues = new object[]
    {
        "World" // name (has default)
    };

    public object[] ParseParameters(HttpListenerRequest request)
    {
        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            ParamTypes,
            ParamNames,
            ParamDefaultValues
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.Hello API请求
/// </summary>
public class GETapisamplehelloHandler_C4E22ACA : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request,  ResponseBuffer buffer)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapisamplehelloParameterParser_C4E22ACA();
            var parameters = parser.ParseParameters(request);

            // 调用控制器方法
            var result = LocalRestAPI.SampleController.Hello(parameters[0] as string);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
            buffer.StatusCode = 200;
            buffer.ContentType = "application/json";
            buffer.Body = jsonResponse;
            buffer.RawBytes = null;
            
            return true;
           
        }
        catch (System.Exception ex)
        {
            buffer.StatusCode = 500;
            buffer.ContentType = "application/json";
            buffer.Body = "{\"error\":\"" + Escape(ex.Message) + "\"}";
            buffer.RawBytes = null;
            return false;
        }
    }

}

/// <summary>
/// LocalRestAPI.SampleController.Echo 方法的参数解析器（编译期写死参数元数据）
/// 说明：
/// - 生成阶段直接把参数类型 / 名称 / 默认值写入静态数组，Parse 时不再做反射。
/// - 仍然委托统一的 ApiParameterParser 做 QueryString / JSON / Form 解析与类型转换。
/// </summary>
public class POSTapisampleechoParameterParser_ED50351C : IApiParameterParser
{
    // 参数类型数组（与方法参数顺序一致）
    private static readonly System.Type[] ParamTypes = new System.Type[]
    {
        typeof(System.String) // message
    };

    // 参数名称数组
    private static readonly string[] ParamNames = new string[]
    {
        "message"
    };

    // 参数默认值数组（无默认值用 null）
    private static readonly object[] ParamDefaultValues = new object[]
    {
        null // message
    };

    public object[] ParseParameters(HttpListenerRequest request)
    {
        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            ParamTypes,
            ParamNames,
            ParamDefaultValues
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.Echo API请求
/// </summary>
public class POSTapisampleechoHandler_ED50351C : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request,  ResponseBuffer buffer)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.POSTapisampleechoParameterParser_ED50351C();
            var parameters = parser.ParseParameters(request);

            // 调用控制器方法
            var result = LocalRestAPI.SampleController.Echo(parameters[0] as string);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
            buffer.StatusCode = 200;
            buffer.ContentType = "application/json";
            buffer.Body = jsonResponse;
            buffer.RawBytes = null;
            
            return true;
           
        }
        catch (System.Exception ex)
        {
            buffer.StatusCode = 500;
            buffer.ContentType = "application/json";
            buffer.Body = "{\"error\":\"" + Escape(ex.Message) + "\"}";
            buffer.RawBytes = null;
            return false;
        }
    }

}

/// <summary>
/// LocalRestAPI.SampleController.GetRandom 方法的参数解析器（编译期写死参数元数据）
/// 说明：
/// - 生成阶段直接把参数类型 / 名称 / 默认值写入静态数组，Parse 时不再做反射。
/// - 仍然委托统一的 ApiParameterParser 做 QueryString / JSON / Form 解析与类型转换。
/// </summary>
public class GETapisamplerandomParameterParser_5F57CD01 : IApiParameterParser
{
    // 参数类型数组（与方法参数顺序一致）
    private static readonly System.Type[] ParamTypes = new System.Type[]
    {
        typeof(System.Int32), // min
        typeof(System.Int32) // max
    };

    // 参数名称数组
    private static readonly string[] ParamNames = new string[]
    {
        "min",
        "max"
    };

    // 参数默认值数组（无默认值用 null）
    private static readonly object[] ParamDefaultValues = new object[]
    {
        0, // min (has default)
        100 // max (has default)
    };

    public object[] ParseParameters(HttpListenerRequest request)
    {
        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            ParamTypes,
            ParamNames,
            ParamDefaultValues
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.GetRandom API请求
/// </summary>
public class GETapisamplerandomHandler_5F57CD01 : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request,  ResponseBuffer buffer)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapisamplerandomParameterParser_5F57CD01();
            var parameters = parser.ParseParameters(request);

            // 调用控制器方法
            var result = LocalRestAPI.SampleController.GetRandom((int)parameters[0], (int)parameters[1]);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
            buffer.StatusCode = 200;
            buffer.ContentType = "application/json";
            buffer.Body = jsonResponse;
            buffer.RawBytes = null;
            
            return true;
           
        }
        catch (System.Exception ex)
        {
            buffer.StatusCode = 500;
            buffer.ContentType = "application/json";
            buffer.Body = "{\"error\":\"" + Escape(ex.Message) + "\"}";
            buffer.RawBytes = null;
            return false;
        }
    }

}

/// <summary>
/// LocalRestAPI.SampleController.GetStatus 方法的参数解析器（编译期写死参数元数据）
/// 说明：
/// - 生成阶段直接把参数类型 / 名称 / 默认值写入静态数组，Parse 时不再做反射。
/// - 仍然委托统一的 ApiParameterParser 做 QueryString / JSON / Form 解析与类型转换。
/// </summary>
public class GETapisamplestatusParameterParser_4089A289 : IApiParameterParser
{
    // 参数类型数组（与方法参数顺序一致）
    private static readonly System.Type[] ParamTypes = new System.Type[]
    {
        // (no parameters)
    };

    // 参数名称数组
    private static readonly string[] ParamNames = new string[]
    {
        // (no parameters)
    };

    // 参数默认值数组（无默认值用 null）
    private static readonly object[] ParamDefaultValues = new object[]
    {
        // (no parameters)
    };

    public object[] ParseParameters(HttpListenerRequest request)
    {
        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            ParamTypes,
            ParamNames,
            ParamDefaultValues
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.SampleController.GetStatus API请求
/// </summary>
public class GETapisamplestatusHandler_4089A289 : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request,  ResponseBuffer buffer)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapisamplestatusParameterParser_4089A289();
            var parameters = parser.ParseParameters(request);

            // 调用控制器方法
            var result = LocalRestAPI.SampleController.GetStatus();

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
            buffer.StatusCode = 200;
            buffer.ContentType = "application/json";
            buffer.Body = jsonResponse;
            buffer.RawBytes = null;
            
            return true;
           
        }
        catch (System.Exception ex)
        {
            buffer.StatusCode = 500;
            buffer.ContentType = "application/json";
            buffer.Body = "{\"error\":\"" + Escape(ex.Message) + "\"}";
            buffer.RawBytes = null;
            return false;
        }
    }

}

/// <summary>
/// LocalRestAPI.UnityController.GetActiveScene 方法的参数解析器（编译期写死参数元数据）
/// 说明：
/// - 生成阶段直接把参数类型 / 名称 / 默认值写入静态数组，Parse 时不再做反射。
/// - 仍然委托统一的 ApiParameterParser 做 QueryString / JSON / Form 解析与类型转换。
/// </summary>
public class GETapiunitysceneParameterParser_F164E5DB : IApiParameterParser
{
    // 参数类型数组（与方法参数顺序一致）
    private static readonly System.Type[] ParamTypes = new System.Type[]
    {
        // (no parameters)
    };

    // 参数名称数组
    private static readonly string[] ParamNames = new string[]
    {
        // (no parameters)
    };

    // 参数默认值数组（无默认值用 null）
    private static readonly object[] ParamDefaultValues = new object[]
    {
        // (no parameters)
    };

    public object[] ParseParameters(HttpListenerRequest request)
    {
        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            ParamTypes,
            ParamNames,
            ParamDefaultValues
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.UnityController.GetActiveScene API请求
/// </summary>
public class GETapiunitysceneHandler_F164E5DB : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request,  ResponseBuffer buffer)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapiunitysceneParameterParser_F164E5DB();
            var parameters = parser.ParseParameters(request);

            // 调用控制器方法
            var result = LocalRestAPI.UnityController.GetActiveScene();

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
            buffer.StatusCode = 200;
            buffer.ContentType = "application/json";
            buffer.Body = jsonResponse;
            buffer.RawBytes = null;
            
            return true;
           
        }
        catch (System.Exception ex)
        {
            buffer.StatusCode = 500;
            buffer.ContentType = "application/json";
            buffer.Body = "{\"error\":\"" + Escape(ex.Message) + "\"}";
            buffer.RawBytes = null;
            return false;
        }
    }

}

/// <summary>
/// LocalRestAPI.UnityController.GetObjectsInScene 方法的参数解析器（编译期写死参数元数据）
/// 说明：
/// - 生成阶段直接把参数类型 / 名称 / 默认值写入静态数组，Parse 时不再做反射。
/// - 仍然委托统一的 ApiParameterParser 做 QueryString / JSON / Form 解析与类型转换。
/// </summary>
public class GETapiunityobjectsParameterParser_28E21CDD : IApiParameterParser
{
    // 参数类型数组（与方法参数顺序一致）
    private static readonly System.Type[] ParamTypes = new System.Type[]
    {
        // (no parameters)
    };

    // 参数名称数组
    private static readonly string[] ParamNames = new string[]
    {
        // (no parameters)
    };

    // 参数默认值数组（无默认值用 null）
    private static readonly object[] ParamDefaultValues = new object[]
    {
        // (no parameters)
    };

    public object[] ParseParameters(HttpListenerRequest request)
    {
        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            ParamTypes,
            ParamNames,
            ParamDefaultValues
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.UnityController.GetObjectsInScene API请求
/// </summary>
public class GETapiunityobjectsHandler_28E21CDD : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request,  ResponseBuffer buffer)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.GETapiunityobjectsParameterParser_28E21CDD();
            var parameters = parser.ParseParameters(request);

            // 调用控制器方法
            var result = LocalRestAPI.UnityController.GetObjectsInScene();

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
            buffer.StatusCode = 200;
            buffer.ContentType = "application/json";
            buffer.Body = jsonResponse;
            buffer.RawBytes = null;
            
            return true;
           
        }
        catch (System.Exception ex)
        {
            buffer.StatusCode = 500;
            buffer.ContentType = "application/json";
            buffer.Body = "{\"error\":\"" + Escape(ex.Message) + "\"}";
            buffer.RawBytes = null;
            return false;
        }
    }

}

/// <summary>
/// LocalRestAPI.UnityController.LogMessage 方法的参数解析器（编译期写死参数元数据）
/// 说明：
/// - 生成阶段直接把参数类型 / 名称 / 默认值写入静态数组，Parse 时不再做反射。
/// - 仍然委托统一的 ApiParameterParser 做 QueryString / JSON / Form 解析与类型转换。
/// </summary>
public class POSTapiunitylogParameterParser_43967BDA : IApiParameterParser
{
    // 参数类型数组（与方法参数顺序一致）
    private static readonly System.Type[] ParamTypes = new System.Type[]
    {
        typeof(System.String), // message
        typeof(System.String) // type
    };

    // 参数名称数组
    private static readonly string[] ParamNames = new string[]
    {
        "message",
        "type"
    };

    // 参数默认值数组（无默认值用 null）
    private static readonly object[] ParamDefaultValues = new object[]
    {
        null, // message
        "info" // type (has default)
    };

    public object[] ParseParameters(HttpListenerRequest request)
    {
        return LocalRestAPI.ApiParameterParser.ParseParameters(
            request,
            ParamTypes,
            ParamNames,
            ParamDefaultValues
        );
    }
}

/// <summary>
/// 处理 LocalRestAPI.UnityController.LogMessage API请求
/// </summary>
public class POSTapiunitylogHandler_43967BDA : IApiHandler
{
    public bool HandleRequest(HttpListenerRequest request,  ResponseBuffer buffer)
    {
        try
        {
            // 使用预生成的参数解析器
            var parser = new PredefinedApiHandlers.POSTapiunitylogParameterParser_43967BDA();
            var parameters = parser.ParseParameters(request);

            // 调用控制器方法
            var result = LocalRestAPI.UnityController.LogMessage(parameters[0] as string, parameters[1] as string);

            // 生成JSON响应（使用可序列化的响应类型，避免匿名类型）
            string jsonResponse = result != null 
                ? UnityEngine.JsonUtility.ToJson(result) 
                : UnityEngine.JsonUtility.ToJson(new LocalRestAPI.Runtime.BasicSuccessResponse());
            buffer.StatusCode = 200;
            buffer.ContentType = "application/json";
            buffer.Body = jsonResponse;
            buffer.RawBytes = null;
            
            return true;
           
        }
        catch (System.Exception ex)
        {
            buffer.StatusCode = 500;
            buffer.ContentType = "application/json";
            buffer.Body = "{\"error\":\"" + Escape(ex.Message) + "\"}";
            buffer.RawBytes = null;
            return false;
        }
    }

}

        // 预生成的API处理器类 - 自动从API控制器生成
    private static string Escape(string s) =>
                s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";      
    }
}
