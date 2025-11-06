using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiServer
    {
        private HttpListener httpListener;
        private Thread listenerThread;
        private bool isRunning = false;
        private string accessToken;
        private string serverUrl;
        
        // 存储API路由
        private Dictionary<string, MethodInfo> routes = new Dictionary<string, MethodInfo>();
        private Dictionary<string, Type> controllerTypes = new Dictionary<string, Type>();
        
        public ApiServer(string url, string token)
        {
            serverUrl = url;
            accessToken = token;
        }
        
        public void Start()
        {
            if (isRunning) return;
            
            try
            {
                // 验证URL格式
                if (!IsValidUrl(serverUrl))
                {
                    RestApiMainWindow.Log($"无效的服务器URL格式: {serverUrl}");
                    return;
                }
                
                // 检查端口是否可用
                try
                {
                    var uri = new Uri(serverUrl);
                    if (!PortChecker.IsPortAvailable(uri.Port))
                    {
                        RestApiMainWindow.Log($"警告: 端口 {uri.Port} 当前可能已被其他进程占用。这可能是启动失败的原因。");
                    }
                }
                catch (Exception)
                {
                    // 如果无法解析URL或检查端口，继续启动尝试
                }
                
                httpListener = new HttpListener();
                
                // 添加URL前缀
                try
                {
                    httpListener.Prefixes.Add(serverUrl);
                }
                catch (ArgumentException ex)
                {
                    RestApiMainWindow.Log($"无效的URL前缀: {ex.Message}. 请确保URL以/结尾，例如: http://localhost:8080/");
                    Stop();
                    return;
                }
                
                // 尝试启动HTTP监听器
                httpListener.Start();
                
                isRunning = true;
                
                // 扫描并注册API控制器
                RegisterApiControllers();
                
                // 启动监听线程
                listenerThread = new Thread(ListenForRequests);
                listenerThread.Start();
                
                RestApiMainWindow.Log($"API服务器已启动，监听地址: {serverUrl}");
            }
            catch (HttpListenerException ex)
            {
                string errorMessage = GetHttpListenerErrorMessage(ex);
                RestApiMainWindow.Log($"启动API服务器失败: {errorMessage}");
                Stop();
            }
            catch (PlatformNotSupportedException)
            {
                RestApiMainWindow.Log("启动API服务器失败: 当前平台不支持HttpListener。请确保您使用的是支持的.NET平台。");
                Stop();
            }
            catch (ObjectDisposedException)
            {
                RestApiMainWindow.Log("启动API服务器失败: HttpListener已被释放。");
                Stop();
            }
            catch (Exception ex)
            {
                RestApiMainWindow.Log($"启动API服务器失败: {ex.Message} (类型: {ex.GetType().Name})");
                Stop();
            }
        }
        
        public void Stop()
        {
            if (!isRunning) return;
            
            isRunning = false;
            
            if (httpListener != null)
            {
                httpListener.Stop();
                httpListener.Close();
                httpListener = null;
            }
            
            if (listenerThread != null)
            {
                listenerThread.Join(1000); // 等待最多1秒
                listenerThread = null;
            }
            
            RestApiMainWindow.Log("API服务器已停止");
        }
        
        private void ListenForRequests()
        {
            while (isRunning)
            {
                try
                {
                    var context = httpListener.GetContext();
                    Task.Run(() => ProcessRequest(context));
                }
                catch (Exception ex)
                {
                    if (isRunning) // 如果服务器仍在运行，则记录错误
                    {
                        RestApiMainWindow.Log($"处理请求时出错: {ex.Message}");
                    }
                    break;
                }
            }
        }
        
        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            DateTime startTime = DateTime.Now;
            string method = request.HttpMethod;
            string url = request.Url.ToString();
            string path = request.Url.AbsolutePath;
            string clientIp = request.RemoteEndPoint?.ToString();
            
            // 检查是否为已注册的路由
            string routeKey = $"{method} {path}";
            bool isRegisteredRoute = routes.ContainsKey(routeKey);
            
            // 如果是已注册路由，则记录请求开始
            if (isRegisteredRoute)
            {
                ApiLogger.Instance.LogRequest(method, url, clientIp, null, "");
            }
            
            try
            {
                // 验证访问令牌
                if (!ValidateAccessToken(request))
                {
                    SendResponse(response, "Unauthorized", 401);
                    // 如果是已注册路由，则记录性能指标
                    if (isRegisteredRoute)
                    {
                        ApiPerformanceMonitor.Instance.RecordApiCall(method, path, 401, (DateTime.Now - startTime).TotalMilliseconds, clientIp, false);
                    }
                    return;
                }
                
                // 处理API请求
                HandleApiRequest(request, response, isRegisteredRoute);
            }
            catch (Exception ex)
            {
                // 如果是已注册路由，则记录错误日志
                if (isRegisteredRoute)
                {
                    ApiLogger.Instance.LogError($"处理请求时出错: {ex.Message}", ex);
                }
                SendResponse(response, $"Internal Server Error: {ex.Message}", 500);
            }
            finally
            {
                DateTime endTime = DateTime.Now;
                double duration = (endTime - startTime).TotalMilliseconds;
                
                // 如果是已注册路由，则记录性能指标和请求完成
                if (isRegisteredRoute)
                {
                    ApiPerformanceMonitor.Instance.RecordApiCall(method, path, response.StatusCode, duration, clientIp, false);
                    ApiLogger.Instance.LogResponse("", response.StatusCode, null, "", duration);
                }
                
                response.Close();
            }
        }
        
        private bool ValidateAccessToken(HttpListenerRequest request)
        {
            // 检查请求头中的访问令牌
            string token = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(token))
            {
                // 检查查询参数中的访问令牌
                token = request.QueryString["token"];
            }
            
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            
            // 移除 "Bearer " 前缀（如果存在）
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7);
            }
            
            return token == accessToken;
        }
        
        private void HandleApiRequest(HttpListenerRequest request, HttpListenerResponse response, bool isRegisteredRoute = true)
        {
            string path = request.Url.AbsolutePath;
            string method = request.HttpMethod;
            
            // 检查是否为内置路由
            if (path == "/api/routes" && method == "GET")
            {
                HandleRoutesRequest(response);
                return;
            }
            
            // 查找匹配的API路由
            string routeKey = $"{method} {path}";
            if (routes.ContainsKey(routeKey))
            {
                var methodInfo = routes[routeKey];
                
                try
                {
                    // 获取参数
                    var parameters = GetParametersFromRequest(request, methodInfo);
                    
                    // 调用控制器方法
                    object controllerInstance = Activator.CreateInstance(controllerTypes[methodInfo.DeclaringType.FullName]);
                    var result = methodInfo.Invoke(controllerInstance, parameters);
                    
                    // 发送响应
                    string jsonResponse = JsonUtility.ToJson(result != null ? result : new { success = true });
                    SendResponse(response, jsonResponse, 200, "application/json");
                }
                catch (Exception ex)
                {
                    RestApiMainWindow.Log($"调用API方法时出错: {ex.Message}");
                    // 只对已注册路由记录错误日志
                    if (isRegisteredRoute)
                    {
                        ApiLogger.Instance.LogError($"调用API方法时出错: {method} {path}", ex);
                    }
                    SendResponse(response, $"Method execution error: {ex.Message}", 500);
                }
            }
            else
            {
                // 对于未注册的路由，不记录到日志和性能监控
                SendResponse(response, "Not Found", 404);
            }
        }
        
        private void HandleRoutesRequest(HttpListenerResponse response)
        {
            var routeList = new List<RouteInfo>();
            foreach (var route in routes)
            {
                var parts = route.Key.Split(' ');
                routeList.Add(new RouteInfo
                {
                    method = parts[0],
                    path = parts[1],
                    handler = route.Value.DeclaringType.Name + "." + route.Value.Name
                });
            }
            
            string jsonResponse = JsonUtility.ToJson(new { routes = routeList });
            SendResponse(response, jsonResponse, 200, "application/json");
        }
        
        [Serializable]
        private class RouteInfo
        {
            public string method;
            public string path;
            public string handler;
        }
        
        private object[] GetParametersFromRequest(HttpListenerRequest request, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var parameterValues = new object[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                string paramValue = null;
                
                // 首先从查询字符串中查找
                if (request.QueryString[param.Name] != null)
                {
                    paramValue = request.QueryString[param.Name];
                }
                // 然后尝试从POST数据中查找
                else if (request.HttpMethod == "POST" && request.ContentLength64 > 0)
                {
                    using (var reader = new StreamReader(request.InputStream))
                    {
                        string body = reader.ReadToEnd();
                        // 简单的JSON解析，实际项目中应该使用更健壮的JSON库
                        // 这里我们只处理简单的键值对
                        if (body.Contains("{"))
                        {
                            // TODO: 实现更完整的JSON参数解析
                        }
                    }
                }
                
                // 转换参数类型
                if (paramValue != null)
                {
                    if (param.ParameterType == typeof(string))
                    {
                        parameterValues[i] = paramValue;
                    }
                    else if (param.ParameterType == typeof(int))
                    {
                        parameterValues[i] = int.Parse(paramValue);
                    }
                    else if (param.ParameterType == typeof(float))
                    {
                        parameterValues[i] = float.Parse(paramValue);
                    }
                    else if (param.ParameterType == typeof(bool))
                    {
                        parameterValues[i] = bool.Parse(paramValue);
                    }
                    else
                    {
                        // 对于复杂类型，我们暂时只支持基本类型
                        parameterValues[i] = paramValue;
                    }
                }
                else
                {
                    // 如果没有找到参数值，使用默认值
                    parameterValues[i] = param.DefaultValue ?? GetDefault(param.ParameterType);
                }
            }
            
            return parameterValues;
        }
        
        private object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        
        private void SendResponse(HttpListenerResponse response, string content, int statusCode, string contentType = "text/plain")
        {
            response.StatusCode = statusCode;
            response.ContentType = contentType;
            
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        
        public Dictionary<string, MethodInfo> GetRoutes()
        {
            return new Dictionary<string, MethodInfo>(routes);
        }
        
        private void RegisterApiControllers()
        {
            routes.Clear();
            controllerTypes.Clear();
            
            // 查找所有标记了ApiRouteAttribute的类和方法
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Namespace != null && type.Namespace.StartsWith("LocalRestAPI"))
                        {
                            // 检查类中的所有方法
                            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                            {
                                var routeAttr = method.GetCustomAttribute<ApiRouteAttribute>();
                                if (routeAttr != null)
                                {
                                    string routeKey = $"{routeAttr.Method} {routeAttr.Path}";
                                    
                                    // 注册路由
                                    routes[routeKey] = method;
                                    controllerTypes[type.FullName] = type;
                                    
                                    RestApiMainWindow.Log($"注册API路由: {routeKey} -> {type.Name}.{method.Name}");
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
            
            // 注册内置路由
            RestApiMainWindow.Log($"已注册 {routes.Count} 个API路由");
        }
        
        public bool IsRunning()

        {

            return isRunning;

        }

        

        private bool IsValidUrl(string url)

        {

            try

            {

                var uri = new Uri(url);

                return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

            }

            catch

            {

                return false;

            }

        }

        

        private string GetHttpListenerErrorMessage(HttpListenerException ex)

        {

            switch (ex.ErrorCode)

            {

                case 5: // ERROR_ACCESS_DENIED

                    return "访问被拒绝。可能需要管理员权限，或URL保留已由其他进程占用。错误代码: 5";

                case 183: // ERROR_ALREADY_EXISTS

                    return "URL前缀已被其他进程保留。请尝试更改端口或检查是否有其他实例正在运行。错误代码: 183";

                case 32: // ERROR_SHARING_VIOLATION

                    return "端口被占用。另一个应用程序可能正在使用相同的端口。错误代码: 32";

                default:

                    return $"HTTP监听器错误 (错误代码: {ex.ErrorCode}): {ex.Message}。" +

                          "请确保您使用正确的端口，没有其他实例在运行，并且有足够的权限。";

            }

        }

    }

    

    // API路由属性

    [AttributeUsage(AttributeTargets.Method)]

    public class ApiRouteAttribute : Attribute

    {

        public string Method { get; set; }

        public string Path { get; set; }

        

        public ApiRouteAttribute(string method, string path)

        {

            Method = method.ToUpper();

            Path = path;

        }

    }

    

    // GET请求属性

    [AttributeUsage(AttributeTargets.Method)]

    public class GetRouteAttribute : ApiRouteAttribute

    {

        public GetRouteAttribute(string path) : base("GET", path) { }

    }

    

    // POST请求属性

    [AttributeUsage(AttributeTargets.Method)]

    public class PostRouteAttribute : ApiRouteAttribute

    {

        public PostRouteAttribute(string path) : base("POST", path) { }

    }

}
