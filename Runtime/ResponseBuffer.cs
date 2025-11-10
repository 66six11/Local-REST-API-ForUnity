using System;

namespace LocalRestAPI.Runtime
{
    /// <summary>
    /// 标准缓冲响应：由 Handler 填充，ApiServer 统一写出与记录日志。
    /// </summary>
    public sealed class ResponseBuffer
    {
        public int StatusCode;
        public string ContentType;
        public string Body;     // 文本响应（UTF-8 编码写出）
        public byte[] RawBytes; // 二进制响应（优先级高于 Body；如不为空将直接写 RawBytes）

        public bool HasPayload =>
            RawBytes != null || !string.IsNullOrEmpty(Body);

        public void Set(int status, string contentType, string body)
        {
            StatusCode = status;
            ContentType = contentType;
            Body = body;
            RawBytes = null;
        }

        public void SetBytes(int status, string contentType, byte[] data)
        {
            StatusCode = status;
            ContentType = contentType;
            RawBytes = data;
            Body = null;
        }
    }
}