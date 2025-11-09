using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalRestAPI
{
    /// <summary>
    /// API参数解析器，负责解析HTTP请求中的参数（Newtonsoft.Json 版本）
    /// </summary>
    public static class ApiParameterParser
    {
        /// <summary>
        /// 解析请求参数。
        /// 优先来源：QueryString，其次 POST/PUT/PATCH 的请求体(JSON 或 form)。
        /// </summary>
        /// <param name="request">HTTP请求</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <param name="paramNames">参数名称数组</param>
        /// <param name="paramDefaultValues">参数默认值数组</param>
        /// <returns>解析后的参数值数组</returns>
        public static object[] ParseParameters(HttpListenerRequest request,
                                               Type[] paramTypes,
                                               string[] paramNames,
                                               object[] paramDefaultValues)
        {
            if (paramTypes == null || paramNames == null)
                throw new ArgumentNullException();

            if (paramTypes.Length != paramNames.Length)
                throw new ArgumentException("paramTypes 与 paramNames 长度不一致");

            var result = new object[paramTypes.Length];

            // 仅在需要时读取一次请求体
            string body = null;
            bool canHaveBody = request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase)
                               || request.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                               || request.HttpMethod.Equals("PATCH", StringComparison.OrdinalIgnoreCase);

            JObject jsonObj = null;
            bool isJson = false;
            bool isForm = false;

            if (canHaveBody && request.ContentLength64 > 0)
            {
                body = ReadRequestBody(request);

                if (!string.IsNullOrWhiteSpace(body))
                {
                    string contentType = request.ContentType ?? string.Empty;

                    isJson = contentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0;
                    isForm = contentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isJson)
                    {
                        try
                        {
                            // 仅当顶层是对象时才有意义（顶层数组时无法按键名取值）
                            var token = JToken.Parse(body);
                            if (token is JObject jo)
                            {
                                jsonObj = jo;
                            }
                            else
                            {
                                isJson = false;
                            }
                        }
                        catch
                        {
                            // JSON解析失败，不抛异常，后续将无法从 JSON 获取参数
                            jsonObj = null;
                            isJson = false;
                        }
                    }
                }
            }

            for (int i = 0; i < paramTypes.Length; i++)
            {
                string name = paramNames[i];
                Type type = paramTypes[i];
                object defaultValue = (paramDefaultValues != null && i < paramDefaultValues.Length)
                    ? paramDefaultValues[i]
                    : GetDefaultForType(type);

                string rawValue = null;

                // 1) QueryString 优先
                if (request.QueryString[name] != null)
                {
                    rawValue = request.QueryString[name];
                }
                // 2) Body(JSON / form)
                else if (canHaveBody && !string.IsNullOrEmpty(body))
                {
                    if (isJson && jsonObj != null)
                    {
                        rawValue = ExtractValueFromJson(jsonObj, name);
                    }
                    else if (isForm)
                    {
                        rawValue = ExtractValueFromFormData(body, name);
                    }
                }

                result[i] = ConvertToTargetType(rawValue, type, defaultValue);
            }

            return result;
        }

        /// <summary>
        /// 从 JSON 对象读取顶层键的字符串表示（数字/布尔保持语义，null 返回 null，对象/数组返回其最小化 JSON）
        /// 忽略键大小写。
        /// </summary>
        private static string ExtractValueFromJson(JObject jo, string name)
        {
            if (jo == null || string.IsNullOrEmpty(name)) return null;

            // 忽略大小写匹配
            if (!jo.TryGetValue(name, StringComparison.Ordinal, out var token) &&
                !jo.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out token))
            {
                return null;
            }

            if (token == null || token.Type == JTokenType.Null)
                return null;

            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Boolean:
                    return token.Value<bool>() ? "true" : "false";
                case JTokenType.Integer:
                case JTokenType.Float:
                    // 使用不变文化区格式化，确保小数点为 '.'
                    // 这里通过 ToString(Formatting.None) 也能获得 JSON 合法数字表示
                    return Convert.ToString(token.Value<double>(), CultureInfo.InvariantCulture);
                default:
                    // 对象或数组等，返回紧凑 JSON
                    return token.ToString(Formatting.None);
            }
        }

        /// <summary>
        /// 读取请求体（HttpListenerRequest 的 InputStream 通常只可读一次）
        /// </summary>
        private static string ReadRequestBody(HttpListenerRequest request)
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: true);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 application/x-www-form-urlencoded 数据
        /// </summary>
        private static string ExtractValueFromFormData(string form, string paramName)
        {
            if (string.IsNullOrEmpty(form)) return null;
            var segments = form.Split('&');
            foreach (var seg in segments)
            {
                if (string.IsNullOrEmpty(seg)) continue;
                var kv = seg.Split('=', 2);
                if (kv.Length == 2)
                {
                    // WebUtility.UrlDecode 会将 '+' 转为空格，行为更符合 x-www-form-urlencoded
                    string key = WebUtility.UrlDecode(kv[0]);
                    if (string.Equals(key, paramName, StringComparison.Ordinal))
                    {
                        return WebUtility.UrlDecode(kv[1]);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 将字符串转换为目标类型，失败则返回默认值（使用 InvariantCulture）
        /// </summary>
        private static object ConvertToTargetType(string raw, Type type, object defaultValue)
        {
            if (raw == null)
                return defaultValue;

            try
            {
                if (type == typeof(string)) return raw;

                if (type == typeof(int))
                    return int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var iv) ? iv : defaultValue;

                if (type == typeof(long))
                    return long.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var lv) ? lv : defaultValue;

                if (type == typeof(short))
                    return short.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var sv) ? sv : defaultValue;

                if (type == typeof(byte))
                    return byte.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var bv) ? bv : defaultValue;

                if (type == typeof(float))
                    return float.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var fv) ? fv : defaultValue;

                if (type == typeof(double))
                    return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dv) ? dv : defaultValue;

                if (type == typeof(decimal))
                    return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var decv) ? decv : defaultValue;

                if (type == typeof(bool))
                    return bool.TryParse(raw, out var bv2) ? bv2 : defaultValue;

                if (type == typeof(char))
                    return raw.Length > 0 ? raw[0] : defaultValue;

                // 其他类型尝试 ChangeType
                return Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取类型默认值
        /// </summary>
        private static object GetDefaultForType(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}