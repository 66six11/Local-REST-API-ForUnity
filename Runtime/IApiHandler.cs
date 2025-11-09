using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

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

    
}