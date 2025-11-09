using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LocalRestAPI.Runtime
{
    public class ApiServer : IDisposable
    {
        public string accessToken;
        public string serverUrl;
        public bool isRunning = false;

        private HttpListener httpListener;
        private Thread listenerThread;
        private CancellationTokenSource cancellationTokenSource;

        // 使用线程安全的并发字典
        private ConcurrentDictionary<string, ApiRoute> apiRoutes = new ConcurrentDictionary<string, ApiRoute>();

        public ApiServer(string serverUrl, string accessToken)
        {
            this.serverUrl = serverUrl;
            this.accessToken = accessToken;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            if (isRunning)
            {
                return;
            }

            Uri url = null;
            if (UrlUtility.IsUrl(serverUrl))
            {
                url = new Uri(serverUrl);
            }
            else
            {
                Logger.Log("服务器地址不正确，请检查是否包含http://或https://或者以/结尾", LogLevel.Error);
                return;
            }

            if (url == null)
            {
                Logger.Log("服务器地址不正确，请检查", LogLevel.Error);
                return;
            }

            // 检查端口是否被占用
            try
            {
                PortChecker.ValidatePort(url.Port);
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
                try
                {
                    var newPort = PortChecker.FindAvailablePort(url.Port);
                    Logger.Log("端口" + url.Port + "已被占用，尝试使用端口" + newPort + "启动服务器", LogLevel.Error);
                }
                catch (Exception e2)
                {
                    Logger.Log(e2.Message);
                    return;
                }

                return;
            }

            //启动服务器
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(serverUrl);
                httpListener.Start();
                isRunning = true;

                // 启动监听线程
                listenerThread = new Thread(ListenForRequests);
                listenerThread.IsBackground = true;
                listenerThread.Start();

                Logger.Log("服务器已启动，监听地址为: " + serverUrl);
                Logger.Log("访问令牌: " + accessToken);
            }
            catch (HttpListenerException e)
            {
                Debug.LogError("启动API服务器失败: " + e.Message);
                Logger.LogError(e.Message);
            }
            catch (PlatformNotSupportedException)
            {
                Logger.Log("启动API服务器失败: 当前平台不支持HttpListener。请确保您使用的是支持的.NET平台。", LogLevel.Error);
                Stop();
            }
            catch (ObjectDisposedException)
            {
                Logger.LogError("启动API服务器失败: HttpListener已被释放。");
                Stop();
            }
            catch (Exception ex)
            {
                Logger.LogError($"启动API服务器失败: {ex.Message} (类型: {ex.GetType().Name})");
                Stop();
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            cancellationTokenSource?.Cancel();

            try
            {
                httpListener?.Stop();
                httpListener?.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"停止服务器时出错: {ex.Message}");
            }
            finally
            {
                httpListener = null;
            }
        }

        private void ListenForRequests()
        {
            while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // 使用异步方式获取上下文，避免阻塞
                    var context = httpListener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException) when (!isRunning)
                {
                    // 服务器停止时的正常异常
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Logger.LogError($"处理请求时出错: {ex.Message}");
                    }

                    Thread.Sleep(100); // 避免频繁错误循环
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string requestId = Guid.NewGuid().ToString();

            string method = request.HttpMethod;
            string url = request.Url.ToString();
            string path = request.Url.AbsolutePath;
            string clientIp = request.RemoteEndPoint?.ToString();
            DateTime startTime = DateTime.Now;

            string routeKey = $"{method} {path}";
            bool isUnregisteredRoute = false;

            try
            {
                // 记录请求日志
                ApiLogger.LogRequest(method, url, clientIp, GetHeaders(request.Headers), GetRequestBody(request), false);

                //检查是否是注册的API路由
                if (!apiRoutes.ContainsKey(routeKey))
                {
                    isUnregisteredRoute = true;
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.StatusDescription = "Not Found";
                    SendResponse(response, "Route not found", (int)HttpStatusCode.NotFound);
                    return;
                }

                //验证访问令牌
                if (!ValidateAccessToken(request))
                {
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    Logger.Log("访问令牌验证失败", LogLevel.Error);
                    SendResponse(response, "Unauthorized", (int)HttpStatusCode.Unauthorized);
                    return;
                }

                //处理API请求
                HandleApiRequest(request, response, startTime, clientIp, requestId);
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理API请求时出错: {ex.Message}");
                try
                {
                    SendResponse(response, $"Internal Server Error: {ex.Message}", 500);
                }
                catch
                {
                    // 忽略响应发送失败
                }
            }
            finally
            {
                // 记录性能指标
                double duration = (DateTime.Now - startTime).TotalMilliseconds;
                ApiPerformanceMonitor.RecordApiCall(method, path, response.StatusCode, duration, clientIp, isUnregisteredRoute);

                // 记录响应日志
                ApiLogger.LogResponse(requestId, response.StatusCode, GetHeaders(response.Headers),
                    GetResponseBody(response), duration, isUnregisteredRoute);
            }
        }

        private void HandleApiRequest(HttpListenerRequest request, HttpListenerResponse response,
                                      DateTime startTime, string clientIp, string requestId)
        {
            string path = request.Url.AbsolutePath;
            string method = request.HttpMethod;

            // 检查是否为内置路由
            if (path == "/api/routes" && method == "GET")
            {
                HandleRoutesRequest(response);
                return;
            }

            string routeKey = $"{method} {path}";

            if (apiRoutes.TryGetValue(routeKey, out var route))
            {
                // 同步调用主线程并等待完成
                bool success = MainThreadDispatcher.Invoke(() =>
                {
                    try
                    {
                        return route.Handler.HandleRequest(request, response);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"处理API请求时发生异常: {ex.Message}");
                        SendResponse(response, $"Handler error: {ex.Message}", 500);
                        return false;
                    }
                });

                if (!success)
                {
                    SendResponse(response, "Method execution error", 500);
                }
            }
        }

        private Dictionary<string, string> GetHeaders(System.Collections.Specialized.NameValueCollection headers)
        {
            var result = new Dictionary<string, string>();
            foreach (string key in headers.AllKeys)
            {
                result[key] = headers[key];
            }

            return result;
        }

        private string GetRequestBody(HttpListenerRequest request)
        {
            if (request.HasEntityBody)
            {
                try
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch
                {
                    return "[Unable to read request body]";
                }
            }

            return string.Empty;
        }

        private string GetResponseBody(HttpListenerResponse response)
        {
            // 注意：这里需要特殊处理来获取响应体
            // 在实际实现中，可能需要修改响应处理方式来捕获响应内容
            return "[Response body logged separately]";
        }

        private void HandleRoutesRequest(HttpListenerResponse response)
        {
            var routeList = new List<RouteInfo>();

            foreach (var route in apiRoutes)
            {
                var parts = route.Key.Split(' ');
                routeList.Add(new RouteInfo
                {
                    method = parts[0],
                    path = parts[1],
                    handler = route.Value.ControllerName + "." + route.Value.MethodName
                });
            }

            string jsonResponse = JsonUtility.ToJson(new { routes = routeList });
            SendResponse(response, jsonResponse, 200, "application/json");
        }

        private void SendResponse(HttpListenerResponse response, string content, int statusCode, string contentType = "text/plain")
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = contentType;

                byte[] buffer = Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"发送响应时出错: {ex.Message}");
                try
                {
                    response.Abort();
                }
                catch
                {
                    // 忽略中止异常
                }
            }
        }

        private bool ValidateAccessToken(HttpListenerRequest request)
        {
            string token = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(token))
            {
                token = request.QueryString["token"];
            }

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7);
            }

            return token == accessToken;
        }

        public void RegisterRoute(string method, string path, IApiHandler handler,
                                  IApiParameterParser parameterParser, string controllerName, string methodName)
        {
            string routeKey = $"{method} {path}";
            var route = new ApiRoute(method, path, handler, parameterParser, controllerName, methodName);

            apiRoutes.AddOrUpdate(routeKey, route, (key, existing) => route);
            Logger.Log($"注册API路由: {routeKey} -> {controllerName}.{methodName}");
        }

        public void UnregisterRoute(string method, string path)
        {
            string routeKey = $"{method} {path}";
            if (apiRoutes.TryRemove(routeKey, out _))
            {
                Logger.Log($"注销API路由: {routeKey}");
            }
        }

        public void Dispose()
        {
            Stop();
            cancellationTokenSource?.Dispose();
        }

        public List<RouteInfo> GetAllRoutes()
        {
            var routeList = new List<RouteInfo>();

            foreach (var route in apiRoutes)
            {
                var parts = route.Key.Split(' ');
                routeList.Add(new RouteInfo
                {
                    method = parts[0],
                    path = parts[1],
                    handler = route.Value.ControllerName + "." + route.Value.MethodName
                });
            }

            return routeList;
        }
    }
}