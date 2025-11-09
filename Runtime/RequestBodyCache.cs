using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace LocalRestAPI.Runtime
{
    /// <summary>
    /// HttpListenerRequest 请求体读取与缓存工具（只读一次，后续复用；读取时不关闭底层流）
    /// </summary>
    internal static class RequestBodyCache
    {
        private class BodyHolder { public string Text; }

        // 与 request 关联的弱表，请求结束后自动可回收
        private static readonly ConditionalWeakTable<HttpListenerRequest, BodyHolder> CWT =
            new ConditionalWeakTable<HttpListenerRequest, BodyHolder>();

        /// <summary>
        /// 获取（或首次读取并缓存）请求体字符串。无实体或读取失败返回 null。
        /// </summary>
        public static string GetOrRead(HttpListenerRequest request)
        {
            if (request == null) return null;
            if (CWT.TryGetValue(request, out var holder))
                return holder.Text;

            var text = SafeRead(request);
            CWT.Add(request, new BodyHolder { Text = text });
            return text;
        }

        private static string SafeRead(HttpListenerRequest request)
        {
            try
            {
                if (!request.HasEntityBody) return null;

                // 兼容缺失 charset 的情况，使用 UTF8 回退；不关闭底层流
                var enc = request.ContentEncoding ?? Encoding.UTF8;
                using var reader = new StreamReader(request.InputStream, enc, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: true);
                return reader.ReadToEnd();
            }
            catch (ObjectDisposedException ex)
            {
                Debug.LogWarning($"[RequestBodyCache] Request body stream disposed: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RequestBodyCache] Failed to read request body: {ex.GetType().Name} {ex.Message}");
                return null;
            }
        }
    }
}