using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
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
        private System.Threading.CancellationTokenSource cancellationTokenSource;

        private readonly ConcurrentDictionary<string, ApiRoute> apiRoutes = new ConcurrentDictionary<string, ApiRoute>();

        public ApiServer(string serverUrl, string accessToken)
        {
            this.serverUrl = serverUrl;
            this.accessToken = accessToken;
            this.cancellationTokenSource = new System.Threading.CancellationTokenSource();
        }

        public void Start()
        {
            if (isRunning) return;

            if (!UrlUtility.IsUrl(serverUrl))
            {
                Logger.Log("服务器地址不正确，请检查是否包含http://或https://或者以/结尾", LogLevel.Error);
                return;
            }

            var url = new Uri(serverUrl);

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
                }

                return;
            }

            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(serverUrl);
                httpListener.Start();

                RegisterRoutes();

                listenerThread = new Thread(ListenForRequests) { IsBackground = true };
                listenerThread.Start();

                isRunning = true;
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

        private void RegisterRoutes()
        {
            PredefinedRouteRegistrar.RegisterRoutes(this);
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
                listenerThread = null;
                cancellationTokenSource = null;
            }
        }

        private void ListenForRequests()
        {
            while (isRunning && cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var context = httpListener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException) when (!isRunning)
                {
                    break; // stopping
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Logger.LogError($"处理请求时出错: {ex.Message}");
                    }

                    Thread.Sleep(100);
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var buffer = new ResponseBuffer(); // 核心：本次请求的响应缓冲

            string requestId = Guid.NewGuid().ToString();
            string method = request.HttpMethod;
            string url = request.Url.ToString();
            string path = request.Url.AbsolutePath;
            string clientIp = request.RemoteEndPoint?.ToString();
            DateTime startTime = DateTime.Now;

            string routeKey = GetRoutekey(method, path);
            bool isUnregisteredRoute = false;

            try
            {
                // 请求日志：使用缓存读取的 body（不会关闭流）
                var bodyForLog = RequestBodyCache.GetOrRead(request);
                ApiLogger.LogRequest(method, url, clientIp, GetHeaders(request.Headers), bodyForLog ?? "", false);

                // 内置路由：列出
                if (path == "/api/routes" && method == "GET")
                {
                    HandleRoutesRequest(buffer);
                    return;
                }

                if (!apiRoutes.ContainsKey(routeKey))
                {
                    isUnregisteredRoute = true;
                    FillBuffer(buffer, (int)HttpStatusCode.NotFound, "text/plain", "Route not found");
                    return;
                }

                if (!ValidateAccessToken(request))
                {
                    FillBuffer(buffer, (int)HttpStatusCode.Unauthorized, "text/plain", "Unauthorized");
                    return;
                }

                HandleApiRequest(request, buffer, routeKey, response);
            }
            catch (Exception ex)
            {
                Logger.LogError($"处理API请求时出错: {ex.Message}");
                FillBuffer(buffer, 500, "text/plain", $"Internal Server Error: {ex.Message}");
            }
            finally
            {
               
                WriteBufferedResponse(response, buffer);

                double duration = (DateTime.Now - startTime).TotalMilliseconds;
                ApiPerformanceMonitor.RecordApiCall(method, path, buffer.StatusCode == 0 ? response.StatusCode : buffer.StatusCode, duration, clientIp, isUnregisteredRoute);
                string respBodyForLog = buffer.Body ?? (buffer.RawBytes != null ? $"[bytes:{buffer.RawBytes.Length}]" : "");
               
                ApiLogger.LogResponse(requestId,
                    buffer.StatusCode == 0 ? response.StatusCode : buffer.StatusCode,
                    GetHeaders(response.Headers),
                    respBodyForLog,
                    duration,
                    isUnregisteredRoute);

                try
                {
                    response.OutputStream.Close();
                }
                catch
                {
                }
            }
        }

        private void HandleApiRequest(HttpListenerRequest request,
                                      ResponseBuffer buffer,
                                      string routeKey,
                                      HttpListenerResponse legacyResponseForFallback)
        {
            if (!apiRoutes.TryGetValue(routeKey, out var route))
            {
                FillBuffer(buffer, (int)HttpStatusCode.NotFound, "text/plain", "Route not found");
                return;
            }

            // 优先走新接口（缓冲）
            if (route.Handler is { } buffered)
            {
                bool success = MainThreadDispatcher.Invoke(() =>
                {
                    try
                    {
                        return buffered.HandleRequest(request, buffer);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"处理API请求时发生异常: {ex.Message}");
                        FillBuffer(buffer, 500, "text/plain", $"Handler error: {ex.Message}");
                        return false;
                    }
                });

                if (!success && !buffer.HasPayload)
                {
                    FillBuffer(buffer, 500, "text/plain", "Method execution error");
                }
            }
        }

        private void HandleRoutesRequest(ResponseBuffer buffer)
        {
            var routeList = new RouteListWrapper();
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

            string jsonResponse = JsonUtility.ToJson(routeList);
            FillBuffer(buffer, 200, "application/json", jsonResponse);
        }

        [Serializable]
        private class RouteListWrapper
        {
            public List<RouteInfo> routes;

            public void Add(RouteInfo route)
            {
                routes ??= new List<RouteInfo>();
                routes.Add(route);
            }
        }

        private void FillBuffer(ResponseBuffer buffer, int statusCode, string contentType, string body)
        {
            buffer.StatusCode = statusCode;
            buffer.ContentType = contentType;
            buffer.Body = body ?? "";
            buffer.RawBytes = null;
        }

        private void WriteBufferedResponse(HttpListenerResponse response, ResponseBuffer buffer)
        {
            // 若无缓冲载荷，说明走了旧 Handler 并已写出，这里不重复写
            if (!buffer.HasPayload) return;

            try
            {
                response.StatusCode = buffer.StatusCode == 0 ? 200 : buffer.StatusCode;
                response.ContentType = string.IsNullOrEmpty(buffer.ContentType) ? "text/plain" : buffer.ContentType;

                byte[] data = buffer.RawBytes ?? Encoding.UTF8.GetBytes(buffer.Body ?? "");
                response.ContentLength64 = data.Length;
                response.OutputStream.Write(data, 0, data.Length);
                response.OutputStream.Flush();
            }
            catch (Exception ex)
            {
                Logger.LogError($"写出响应失败: {ex.Message}");
                try
                {
                    response.Abort();
                }
                catch
                {
                }
            }
        }

        private Dictionary<string, string> GetHeaders(System.Collections.Specialized.NameValueCollection headers)
        {
            var result = new Dictionary<string, string>();
            foreach (string key in headers.AllKeys)
                result[key] = headers[key];
            return result;
        }

        private bool ValidateAccessToken(HttpListenerRequest request)
        {
            string token = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(token))
                token = request.QueryString["token"];

            if (string.IsNullOrEmpty(token))
                return false;

            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7);

            return token == accessToken;
        }

        public void RegisterRoute(string method, string path, IApiHandler handler,
                                  IApiParameterParser parameterParser, string controllerName, string methodName)
        {
            string routeKey = GetRoutekey(method, path);
            var route = new ApiRoute(method, path, handler, parameterParser, controllerName, methodName);

            apiRoutes.AddOrUpdate(routeKey, route, (key, _) => route);
            Logger.Log($"注册API路由: {routeKey} -> {controllerName}.{methodName}");
        }

        public void UnregisterRoute(string method, string path)
        {
            string routeKey = GetRoutekey(method, path);
            if (apiRoutes.TryRemove(routeKey, out _))
                Logger.Log($"注销API路由: {routeKey}");
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

        public string GetRoutekey(string method, string path) => $"{method} {path}";
    }
}