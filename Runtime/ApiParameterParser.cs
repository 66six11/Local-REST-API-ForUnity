using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace LocalRestAPI
{
    /// <summary>
    /// 统一的 API 参数解析器：
    /// - 预先由代码生成器提供参数类型 / 名称 / 默认值数组（不再运行时反射）。
    /// - 优先使用 QueryString（仅当其有实际值），否则尝试 Body(JSON 或 form)。
    /// - 支持 application/json 和 application/x-www-form-urlencoded。
    /// - 对 ContentType 缺失但 Body 形如 { ... } 的情况进行 JSON 容错。
    /// - 支持基础类型、可空类型、枚举、字符串、char、DateTime、Guid、decimal 等。
    /// - 失败返回传入的 defaultValue（生成时决定），否则返回类型默认值。
    /// </summary>
    public static class ApiParameterParser
    {
        /// <summary>
        /// 解析请求参数。
        /// 调用方提供已写死的元数据数组：paramTypes / paramNames / paramDefaultValues
        /// </summary>
        public static object[] ParseParameters(HttpListenerRequest request,
                                               Type[] paramTypes,
                                               string[] paramNames,
                                               object[] paramDefaultValues)
        {
            
          
            
            if (paramTypes == null || paramNames == null)
                throw new ArgumentNullException("paramTypes / paramNames 不能为空");

            if (paramTypes.Length != paramNames.Length)
                throw new ArgumentException("paramTypes 与 paramNames 长度不一致");

            var result = new object[paramTypes.Length];

            // 允许解析 Body 的方法
            bool canHaveBody = request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase)
                               || request.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                               || request.HttpMethod.Equals("PATCH", StringComparison.OrdinalIgnoreCase);

            string body = null;
            JObject jsonObj = null;
            bool isJson = false;
            bool isForm = false;

            if (canHaveBody && request.ContentLength64 > 0)
            {
                body = ReadRequestBodyOnce(request);

                if (!string.IsNullOrWhiteSpace(body))
                {
                    string contentType = request.ContentType ?? string.Empty;
                    isJson = contentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0;
                    isForm = contentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) >= 0;

                    // 容错：缺失 ContentType 但看起来是 JSON 对象
                    bool looksLikeJson = body.TrimStart().StartsWith("{") && body.TrimEnd().EndsWith("}");
                    if (!isJson && looksLikeJson)
                        isJson = true;

                    if (isJson)
                    {
                        try
                        {
                            var token = JToken.Parse(body);
                            if (token is JObject jo)
                                jsonObj = jo;
                            else
                                isJson = false;
                        }
                        catch
                        {
                            isJson = false;
                        }
                    }
                }
            }

            for (int i = 0; i < paramTypes.Length; i++)
            {
                var targetType = paramTypes[i];
                string name = paramNames[i];
                object defaultValue = (paramDefaultValues != null && i < paramDefaultValues.Length)
                                      ? paramDefaultValues[i]
                                      : GetDefaultForType(targetType);

                string rawValue = null;

                // QueryString 优先：仅当有实际值（非 null 且长度 > 0）
                var qsValue = request.QueryString[name];
                if (!string.IsNullOrEmpty(qsValue))
                {
                    rawValue = qsValue;
                }
                else
                {
                    // 如果 QueryString 没提供有效值，则尝试 Body
                    if (canHaveBody && !string.IsNullOrEmpty(body))
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
                }

                // 转换
                result[i] = ConvertToTargetType(rawValue, targetType, defaultValue);
            }
            
            return result;
        }

        #region Body 读取与格式判定

        /// <summary>
        /// 读取请求体（HttpListenerRequest.InputStream 为一次性流，不可重复读取）
        /// </summary>
        private static string ReadRequestBodyOnce(HttpListenerRequest request)
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: true);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region JSON / Form 提取

        /// <summary>
        /// 从 JSON 对象读取顶层键的字符串表示（数字/布尔保持语义；null 返回 null；复杂对象返回紧凑 JSON）
        /// </summary>
        private static string ExtractValueFromJson(JObject jo, string name)
        {
            if (jo == null || string.IsNullOrEmpty(name)) return null;

            if (!jo.TryGetValue(name, StringComparison.Ordinal, out var token) &&
                !jo.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out token))
                return null;

            if (token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                return null;

            switch (token.Type)
            {
                case Newtonsoft.Json.Linq.JTokenType.String:
                    return token.Value<string>();
                case Newtonsoft.Json.Linq.JTokenType.Boolean:
                    return token.Value<bool>() ? "true" : "false";
                case Newtonsoft.Json.Linq.JTokenType.Integer:
                case Newtonsoft.Json.Linq.JTokenType.Float:
                    // 保留数值原貌
                    return token.ToString(Newtonsoft.Json.Formatting.None);
                default:
                    // 对象或数组等
                    return token.ToString(Newtonsoft.Json.Formatting.None);
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
                    string key = WebUtility.UrlDecode(kv[0]);
                    if (string.Equals(key, paramName, StringComparison.Ordinal))
                    {
                        return WebUtility.UrlDecode(kv[1]);
                    }
                }
            }
            return null;
        }

        #endregion

        #region 类型转换

        private static object ConvertToTargetType(string raw, Type type, object defaultValue)
        {
            if (raw == null)
                return defaultValue;

            // Nullable<T>
            if (IsNullable(type, out var underlying))
            {
                if (string.IsNullOrEmpty(raw))
                    return null; // 空字符串视作 null

                var converted = ConvertToTargetType(raw, underlying, GetDefaultForType(underlying));
                return converted; // 转回基础类型；上层会直接当 nullable 用
            }

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
                    return bool.TryParse(raw, out var b) ? b : defaultValue;

                if (type == typeof(char))
                    return raw.Length > 0 ? raw[0] : defaultValue;

                if (type == typeof(DateTime))
                {
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                        return dt;
                    // ISO8601 失败再尝试不带 Kind
                    if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        return dt;
                    return defaultValue;
                }

                if (type == typeof(Guid))
                {
                    if (Guid.TryParse(raw, out var g)) return g;
                    return defaultValue;
                }

                if (type.IsEnum)
                {
                    // 允许数字或名称（忽略大小写）
                    if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var enumInt))
                        return Enum.ToObject(type, enumInt);
                    try
                    {
                        return Enum.Parse(type, raw, ignoreCase: true);
                    }
                    catch
                    {
                        return defaultValue ?? Activator.CreateInstance(type);
                    }
                }

                // 其他引用或值类型尝试 ChangeType
                return Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue ?? GetDefaultForType(type);
            }
        }

        private static bool IsNullable(Type t, out Type underlying)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                underlying = Nullable.GetUnderlyingType(t);
                return true;
            }
            underlying = null;
            return false;
        }

        /// <summary>
        /// 获取类型默认值（值类型创建实例，引用类型 null）
        /// </summary>
        private static object GetDefaultForType(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        #endregion
    }
}