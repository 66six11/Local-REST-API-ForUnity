using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI.EditorTools
{
    public class ApiParameterParserTesterWindow : EditorWindow
    {
        [MenuItem("LocalRestAPI/参数解析测试器")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<ApiParameterParserTesterWindow>("参数解析测试器");
            wnd.minSize = new Vector2(820, 600);
            wnd.wantsMouseMove = true;
        }

        private enum HttpMethodKind { GET, POST, PUT, PATCH }

        // Server settings
        private string host = "http://127.0.0.1";
        private int port = 47123;
        private string routePath = "/test"; // server listens here
        private bool serverRunning;
        private HttpListener listener;
        private Thread listenerThread;

        // Client request settings
        private HttpMethodKind method = HttpMethodKind.POST;
        private string requestPath = "/test";
        private string contentType = "application/json";
        private string requestBody = "{\"message\":\"123\",\"type\":\"info\"}";
        private List<KV> queryParams = new List<KV> { new KV("message", ""), new KV("type", "info") };

        // Parameter metadata to feed ApiParameterParser
        private List<TestParam> testParams = new List<TestParam>
        {
            new TestParam("message", ParamType.String, ""), // default null/empty
            new TestParam("type", ParamType.String, "info"),
        };

        // Results
        private Vector2 scroll;
        private string lastRequestInfo = "";
        private string lastParsedJson = "";
        private string lastError = "";

        // Thread-safety
        private readonly object sync = new object();

        private static readonly HttpClient client = new HttpClient();

        private void OnEnable()
        {
            EditorApplication.quitting += OnEditorQuitting;
        }

        private void OnDisable()
        {
            StopServer();
            EditorApplication.quitting -= OnEditorQuitting;
        }

        private void OnEditorQuitting()
        {
            StopServer();
        }

        private void OnGUI()
        {
            DrawServerControls();
            EditorGUILayout.Space(6);
            DrawParamMetadata();
            EditorGUILayout.Space(6);
            DrawClientRequestBuilder();
            EditorGUILayout.Space(6);
            DrawResults();
        }

        private void DrawServerControls()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("本地测试服务", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            host = EditorGUILayout.TextField("Host", host);
            port = EditorGUILayout.IntField("Port", port);
            routePath = EditorGUILayout.TextField("服务端路由路径", routePath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (!serverRunning)
            {
                if (GUILayout.Button("启动服务", GUILayout.Height(26)))
                {
                    StartServer();
                }
            }
            else
            {
                if (GUILayout.Button("停止服务", GUILayout.Height(26)))
                {
                    StopServer();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            string url = $"{host}:{port}{routePath}";
            EditorGUILayout.LabelField("监听地址:", url);

            EditorGUILayout.EndVertical();
        }

        private void DrawParamMetadata()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("参数元数据（传入 ApiParameterParser 的 ParamTypes/ParamNames/Defaults）", EditorStyles.boldLabel);

            for (int i = 0; i < testParams.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                testParams[i].Name = EditorGUILayout.TextField(testParams[i].Name, GUILayout.Width(180));
                testParams[i].TypeName = (ParamType)EditorGUILayout.EnumPopup(testParams[i].TypeName, GUILayout.Width(120));
                testParams[i].DefaultValue = EditorGUILayout.TextField(testParams[i].DefaultValue);
                if (GUILayout.Button("×", GUILayout.Width(28)))
                {
                    testParams.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加参数", GUILayout.Width(100)))
            {
                testParams.Add(new TestParam("param" + (testParams.Count + 1), ParamType.String, ""));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawClientRequestBuilder()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("客户端请求构建", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            method = (HttpMethodKind)EditorGUILayout.EnumPopup("Method", method, GUILayout.Width(350));
            requestPath = EditorGUILayout.TextField("请求路径", requestPath);
            EditorGUILayout.EndHorizontal();

            // Query params
            GUILayout.Label("查询参数 (key=value)", EditorStyles.miniBoldLabel);
            for (int i = 0; i < queryParams.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                queryParams[i].Key = EditorGUILayout.TextField(queryParams[i].Key, GUILayout.Width(180));
                queryParams[i].Value = EditorGUILayout.TextField(queryParams[i].Value);
                if (GUILayout.Button("×", GUILayout.Width(28)))
                {
                    queryParams.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("添加Query参数", GUILayout.Width(150)))
            {
                queryParams.Add(new KV("", ""));
            }

            // Headers and body
            EditorGUILayout.Space(6);
            contentType = EditorGUILayout.TextField("Content-Type", contentType);

            if (method != HttpMethodKind.GET)
            {
                GUILayout.Label("请求Body（原始）", EditorStyles.miniBoldLabel);
                requestBody = EditorGUILayout.TextArea(requestBody, GUILayout.MinHeight(80));
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("发送请求", GUILayout.Height(28)))
            {
                _ = SendClientRequest();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private Vector2 jsonScrollPosition = Vector2.zero;
        private bool showRawJson = false; // 切换显示格式

        private void DrawResults()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("执行结果", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(240));

            lock (sync)
            {
                if (!string.IsNullOrEmpty(lastError))
                {
                    EditorGUILayout.HelpBox(lastError, MessageType.Error);
                }

                GUILayout.Label("请求信息:", EditorStyles.miniBoldLabel);
                EditorGUILayout.TextArea(lastRequestInfo ?? "", GUILayout.MinHeight(60));

                GUILayout.Space(6);
        
                // JSON结果显示区域
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("解析结果（服务端返回）:", EditorStyles.miniBoldLabel);
        
                // 添加格式切换按钮
                if (!string.IsNullOrEmpty(lastParsedJson))
                {
                    showRawJson = EditorGUILayout.ToggleLeft("原始格式", showRawJson, GUILayout.Width(80));
                }
                EditorGUILayout.EndHorizontal();

                // JSON内容显示区域
                jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition, GUILayout.Height(120));
        
                string displayJson = lastParsedJson ?? "";
                if (!showRawJson && !string.IsNullOrEmpty(displayJson))
                {
                    try
                    {
                        // 尝试格式化JSON
                        var parsedJson = Newtonsoft.Json.Linq.JToken.Parse(displayJson);
                        displayJson = parsedJson.ToString(Newtonsoft.Json.Formatting.Indented);
                    }
                    catch
                    {
                        // 如果不是有效JSON，保持原样
                    }
                }
        
                // 使用TextField而不是TextArea，因为我们在ScrollView中
                displayJson = EditorGUILayout.TextArea(displayJson, 
                    GUILayout.ExpandHeight(true), 
                    GUILayout.ExpandWidth(true));
            
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        private void StartServer()
        {
            try
            {
                if (serverRunning) return;

                listener = new HttpListener();
                var prefix = $"{host}:{port}/";
                if (!prefix.EndsWith("/")) prefix += "/";
                if (!routePath.StartsWith("/")) routePath = "/" + routePath;
                // Listen the root so client can call /test or other path we set
                listener.Prefixes.Add(prefix);

                listener.Start();
                serverRunning = true;
                listenerThread = new Thread(ListenerLoop);
                listenerThread.IsBackground = true;
                listenerThread.Start();

                lastError = "";
            }
            catch (Exception ex)
            {
                lastError = "启动服务失败: " + ex.Message;
                serverRunning = false;
                try { listener?.Close(); } catch { }
                listener = null;
            }
            Repaint();
        }

        private void StopServer()
        {
            try
            {
                if (!serverRunning) return;
                serverRunning = false;
                try { listener?.Stop(); } catch { }
                try { listener?.Close(); } catch { }
                listener = null;
                if (listenerThread != null && listenerThread.IsAlive)
                {
                    listenerThread.Abort();
                    listenerThread = null;
                }
            }
            catch (Exception ex)
            {
                lastError = "停止服务异常: " + ex.Message;
            }
            Repaint();
        }

        private void ListenerLoop()
        {
            while (serverRunning && listener != null && listener.IsListening)
            {
                try
                {
                    var ctx = listener.GetContext(); // blocking
                    ThreadPool.QueueUserWorkItem(_ => HandleContext(ctx));
                }
                catch (HttpListenerException)
                {
                    // Listener closed
                    break;
                }
                catch (Exception ex)
                {
                    lock (sync) { lastError = "监听循环异常: " + ex.Message; }
                    EditorApplication.delayCall += Repaint;
                }
            }
        }

        private void HandleContext(HttpListenerContext ctx)
        {
            try
            {
                var req = ctx.Request;

                // Build Param metadata arrays from UI
                var (paramTypes, paramNames, paramDefaults) = BuildParamArrays();

                // Call your ApiParameterParser
                object[] parsed = ApiParameterParser.ParseParameters(req, paramTypes, paramNames, paramDefaults);

                // Prepare response: include raw info + parsed
                var info = new
                {
                    method = req.HttpMethod,
                    url = req.Url?.ToString(),
                    contentType = req.ContentType,
                    hasEntityBody = req.HasEntityBody,
                    contentLength64 = req.ContentLength64,
                    query = req.QueryString.AllKeys.ToDictionary(k => k, k => req.QueryString[k]),
                    parsed = BuildParsedDict(paramNames, parsed),
                    parsedTypes = parsed.Select(v => v?.GetType().FullName ?? "null").ToArray()
                };

                string json = JsonConvert.SerializeObject(info, Formatting.Indented);

                // Update UI fields
                lock (sync)
                {
                    lastRequestInfo = $"[{info.method}] {info.url}\nContent-Type: {info.contentType}\nHasEntityBody: {info.hasEntityBody}, ContentLength64: {info.contentLength64}";
                    lastParsedJson = json;
                    lastError = "";
                }
                EditorApplication.delayCall += Repaint;

                // Write response
                var buffer = Encoding.UTF8.GetBytes(json);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentEncoding = Encoding.UTF8;
                ctx.Response.ContentLength64 = buffer.Length;
                using (var os = ctx.Response.OutputStream)
                {
                    os.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                lock (sync) { lastError = "处理请求异常: " + ex.Message; }
                EditorApplication.delayCall += Repaint;
                try
                {
                    ctx.Response.StatusCode = 500;
                    byte[] err = Encoding.UTF8.GetBytes(lastError);
                    ctx.Response.ContentType = "text/plain; charset=utf-8";
                    ctx.Response.ContentLength64 = err.Length;
                    using (var os = ctx.Response.OutputStream) os.Write(err, 0, err.Length);
                }
                catch { }
            }
        }

        private static Dictionary<string, object> BuildParsedDict(string[] names, object[] values)
        {
            var d = new Dictionary<string, object>();
            for (int i = 0; i < names.Length; i++)
            {
                d[names[i]] = values != null && i < values.Length ? values[i] : null;
            }
            return d;
        }

        private (Type[] types, string[] names, object[] defaults) BuildParamArrays()
        {
            var types = new Type[testParams.Count];
            var names = new string[testParams.Count];
            var defs = new object[testParams.Count];

            for (int i = 0; i < testParams.Count; i++)
            {
                var p = testParams[i];
                names[i] = p.Name;
                types[i] = ToSystemType(p.TypeName);
                defs[i] = ParseDefaultValue(p.DefaultValue, types[i]);
            }
            return (types, names, defs);
        }

        private object ParseDefaultValue(string val, Type t)
        {
            if (string.IsNullOrEmpty(val))
            {
                return t.IsValueType ? Activator.CreateInstance(t) : null;
            }

            try
            {
                if (t == typeof(string)) return val;
                if (t == typeof(int)) return int.Parse(val, CultureInfo.InvariantCulture);
                if (t == typeof(long)) return long.Parse(val, CultureInfo.InvariantCulture);
                if (t == typeof(short)) return short.Parse(val, CultureInfo.InvariantCulture);
                if (t == typeof(byte)) return byte.Parse(val, CultureInfo.InvariantCulture);
                if (t == typeof(float)) return float.Parse(val, CultureInfo.InvariantCulture);
                if (t == typeof(double)) return double.Parse(val, CultureInfo.InvariantCulture);
                if (t == typeof(decimal)) return decimal.Parse(val, CultureInfo.InvariantCulture);
                if (t == typeof(bool)) return bool.Parse(val);
                if (t == typeof(char)) return val[0];
                if (t == typeof(DateTime)) return DateTime.Parse(val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                if (t == typeof(Guid)) return Guid.Parse(val);
                return Convert.ChangeType(val, t, CultureInfo.InvariantCulture);
            }
            catch
            {
                return t.IsValueType ? Activator.CreateInstance(t) : null;
            }
        }

        private Type ToSystemType(ParamType pt)
        {
            switch (pt)
            {
                case ParamType.String: return typeof(string);
                case ParamType.Int: return typeof(int);
                case ParamType.Long: return typeof(long);
                case ParamType.Float: return typeof(float);
                case ParamType.Double: return typeof(double);
                case ParamType.Decimal: return typeof(decimal);
                case ParamType.Bool: return typeof(bool);
                case ParamType.Char: return typeof(char);
                case ParamType.DateTime: return typeof(DateTime);
                case ParamType.Guid: return typeof(Guid);
                default: return typeof(string);
            }
        }

        private async System.Threading.Tasks.Task SendClientRequest()
        {
            try
            {
                var baseUrl = $"{host}:{port}";
                var path = requestPath.StartsWith("/") ? requestPath : "/" + requestPath;

                // Build query string
                var qp = queryParams.Where(kv => !string.IsNullOrEmpty(kv.Key))
                                    .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? "")}");
                string url = baseUrl + path + (qp.Any() ? "?" + string.Join("&", qp) : "");

                using var req = new HttpRequestMessage(new System.Net.Http.HttpMethod(method.ToString()), url);

                if (method != HttpMethodKind.GET)
                {
                    // Attach body
                    if (!string.IsNullOrEmpty(contentType) &&
                        contentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        req.Content = new StringContent(requestBody ?? "", Encoding.UTF8, "application/json");
                    }
                    else if (!string.IsNullOrEmpty(contentType) &&
                             contentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Send raw form content (assume body is "a=1&b=2")
                        req.Content = new StringContent(requestBody ?? "", Encoding.UTF8, "application/x-www-form-urlencoded");
                    }
                    else
                    {
                        // default raw text
                        req.Content = new StringContent(requestBody ?? "", Encoding.UTF8, contentType ?? "text/plain");
                    }
                }

                var resp = await client.SendAsync(req);
                var text = await resp.Content.ReadAsStringAsync();

                lock (sync)
                {
                    lastRequestInfo = $"[{method}] {url}\nContent-Type: {contentType}\nStatus: {(int)resp.StatusCode}";
                    lastParsedJson = text;
                    lastError = "";
                }
                Repaint();
            }
            catch (Exception ex)
            {
                lock (sync)
                {
                    lastError = "发送请求失败: " + ex.Message;
                }
                Repaint();
            }
        }

        [Serializable]
        private class KV
        {
            public string Key;
            public string Value;
            public KV() { }
            public KV(string k, string v) { Key = k; Value = v; }
        }

        [Serializable]
        private class TestParam
        {
            public string Name;
            public ParamType TypeName;
            public string DefaultValue;
            public TestParam() { }
            public TestParam(string name, ParamType type, string defVal)
            {
                Name = name; TypeName = type; DefaultValue = defVal;
            }
        }

        private enum ParamType
        {
            String, Int, Long, Float, Double, Decimal, Bool, Char, DateTime, Guid
        }
    }
}