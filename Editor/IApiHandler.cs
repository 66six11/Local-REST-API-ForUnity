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
    /// 支持主线程执行的API处理器基类
    /// </summary>
    public abstract class ThreadSafeApiHandler : IApiHandler

    {
        public virtual bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)

        {
            // 检查是否需要在主线程上执行

            if (NeedsMainThreadExecution())

            {
                // 使用主线程调度器执行方法

                object result = null;

                bool completed = false;

                Exception exception = null;


                MainThreadDispatcher.Enqueue(() =>

                {
                    try

                    {
                        // 记录开始执行的日志
                        RestApiMainWindow.Log($"开始执行主线程API: {this.GetType().Name}");
                        result = ExecuteOnMainThread(request, response);
                        RestApiMainWindow.Log($"主线程API执行完成: {this.GetType().Name}");
                    }

                    catch (Exception ex)

                    {
                        exception = ex;
                        RestApiMainWindow.Log($"主线程API执行异常: {this.GetType().Name}, 错误: {ex.Message}");
                    }

                    finally

                    {
                        completed = true;
                    }
                });


                // 等待主线程执行完成（带超时）
                // 在等待期间，主动处理主线程队列
                // 这次，我们使用更长的超时时间并更频繁地处理队列

                int timeout = 0;

                while (!completed && timeout < 200) // 最多等待20秒

                {
                    // 短暂休眠，让线程有机会处理

                    System.Threading.Thread.Sleep(25); // 减少休眠时间，增加检查频率

                    // 确保主线程队列被处理
                    // 在Unity编辑器中，我们依赖EditorApplication.update来处理队列
                    // 所以我们不再手动调用ProcessQueue，以避免潜在的问题

                    timeout++;
                }


                if (exception != null)

                {
                    SendJsonResponse(response, $"Method execution error: {exception.Message}", 500);

                    return false;
                }


                if (!completed)

                {
                    RestApiMainWindow.Log($"主线程API执行超时: {this.GetType().Name}");
                    SendJsonResponse(response, "Method execution timeout", 500);

                    return false;
                }


                return true;
            }

            else

            {
                // 不需要主线程的方法，直接执行

                return ExecuteOnBackgroundThread(request, response);
            }
        }


        /// <summary>
        /// 检查是否需要在主线程上执行
        /// </summary>
        /// <returns></returns>
        protected virtual bool NeedsMainThreadExecution()

        {
            // 子类可以重写此方法来确定是否需要主线程执行

            return false;
        }


        /// <summary>
        /// 在主线程上执行方法
        /// </summary>
        /// <param name="request">HTTP请求</param>
        /// <param name="response">HTTP响应</param>
        /// <returns>执行结果</returns>
        protected abstract object ExecuteOnMainThread(HttpListenerRequest request, HttpListenerResponse response);


        /// <summary>
        /// 在后台线程上执行方法
        /// </summary>
        /// <param name="request">HTTP请求</param>
        /// <param name="response">HTTP响应</param>
        /// <returns>执行结果</returns>
        protected virtual bool ExecuteOnBackgroundThread(HttpListenerRequest request, HttpListenerResponse response)

        {
            // 默认实现：在后台线程执行主线程方法

            var result = ExecuteOnMainThread(request, response);

            return result != null;
        }


        /// <summary>
        /// 发送JSON响应
        /// </summary>
        /// <param name="response">HTTP响应</param>
        /// <param name="content">响应内容</param>
        /// <param name="statusCode">状态码</param>
        protected void SendJsonResponse(HttpListenerResponse response, string content, int statusCode)

        {
            response.StatusCode = statusCode;

            response.ContentType = "application/json";


            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);

            response.ContentLength64 = buffer.Length;


            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
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