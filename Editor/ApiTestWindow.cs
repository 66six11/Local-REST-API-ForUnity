using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LocalRestAPI.Runtime;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiTestWindow : EditorWindow
    {
        private static ApiTestWindow window;

        // HTTP客户端用于异步请求（全局复用，设置统一超时）
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // API服务器实例
        private ApiServer apiServer;

        // API路由选择相关
        private readonly List<RouteInfo> registeredRoutes = new List<RouteInfo>();
        private int selectedRouteIndex = 0;
        private string[] routeDisplayNames;

        // 请求参数相关
        private readonly Dictionary<string, string> parameterValues = new Dictionary<string, string>();
        private readonly List<ApiParameterInfo> currentRouteParameters = new List<ApiParameterInfo>();

        // 请求结果相关
        private string responseContent = "";
        private int responseStatusCode = 0;
        private bool showResponse = false;
        private Vector2 responseScrollPosition;

        // 请求设置相关（保留扩展位）
        private string customUrl = "";
        private string accessToken = "";
        private Vector2 parameterScrollPosition;

        // 参数信息类
        public class ApiParameterInfo
        {
            public string Name;
            public string Type; // 友好类型名（string/int/float/double/bool/long/short/byte/char/decimal...）
            public string DefaultValue;
            public bool HasDefaultValue;
        }

        // 从主控制台打开窗口并传递API服务器实例
        public static void ShowWindow(ApiServer server)
        {
            window = GetWindow<ApiTestWindow>("API测试工具");
            window.minSize = new Vector2(600, 500);
            if (server != null)
            {
                window.SetApiServer(server);
            }
        }

        // 设置API服务器实例
        public void SetApiServer(ApiServer server)
        {
            apiServer = server;
            RefreshRouteList();
        }

        private void OnEnable()
        {
            RefreshRouteList();
        }

        private void OnGUI()
        {
            // 标题
            GUILayout.Label("API测试工具", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space();

            // API选择面板
            DrawApiSelectionPanel();

            // 参数输入面板
            DrawParameterInputPanel();

            // 请求执行面板
            DrawRequestExecutionPanel();

            // 响应显示面板
            DrawResponsePanel();
        }

        private void DrawApiSelectionPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("API选择", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("选择API");

            // 创建路由显示名称数组
            if (routeDisplayNames != null && routeDisplayNames.Length > 0)
            {
                int newSelectedIndex = EditorGUILayout.Popup(selectedRouteIndex, routeDisplayNames);
                if (newSelectedIndex != selectedRouteIndex)
                {
                    selectedRouteIndex = newSelectedIndex;
                    // 当选择改变时，重置参数值
                    ResetParameterValues();
                    // 解析新选中路由的参数
                    ParseRouteParameters();
                }
            }
            else
            {
                EditorGUILayout.LabelField("暂无可用API");
            }

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshRouteList();
            }

            EditorGUILayout.EndHorizontal();

            // 显示选中的API详细信息
            if (registeredRoutes.Count > 0 && selectedRouteIndex >= 0 && selectedRouteIndex < registeredRoutes.Count)
            {
                var selectedRoute = registeredRoutes[selectedRouteIndex];
                EditorGUILayout.LabelField("方法:", selectedRoute.method);
                EditorGUILayout.LabelField("路径:", selectedRoute.path);
                EditorGUILayout.LabelField("处理器:", selectedRoute.handler);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawParameterInputPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("请求参数", EditorStyles.boldLabel);

            parameterScrollPosition = EditorGUILayout.BeginScrollView(parameterScrollPosition, GUILayout.Height(180));

            if (registeredRoutes.Count > 0 && selectedRouteIndex >= 0 && selectedRouteIndex < registeredRoutes.Count)
            {
                DrawParametersForRoute();
            }
            else
            {
                EditorGUILayout.LabelField("请选择一个API路由以查看参数");
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawParametersForRoute()
        {
            if (currentRouteParameters.Count == 0)
            {
                EditorGUILayout.HelpBox("该API无需参数", MessageType.Info);
                return;
            }

            // 使用表格布局显示参数
            foreach (var param in currentRouteParameters)
            {
                EditorGUILayout.BeginHorizontal();

                // 参数名称和类型（左对齐）
                EditorGUILayout.LabelField($"{param.Name} ({param.Type})", GUILayout.Width(180));

                string valueKey = param.Name;
                string currentValue = parameterValues.ContainsKey(valueKey) ? parameterValues[valueKey] : "";

                // 根据参数类型显示不同的输入控件
                string newValue = DrawParameterInputControl(param, currentValue);

                parameterValues[valueKey] = newValue;

                // 显示默认值提示
                if (param.HasDefaultValue)
                {
                    EditorGUILayout.LabelField($"默认: {param.DefaultValue}", EditorStyles.miniLabel, GUILayout.Width(120));
                }

                EditorGUILayout.EndHorizontal();

                // 验证参数值并在无效时显示警告
                if (!ValidateParameterValue(param, newValue) && !string.IsNullOrEmpty(newValue))
                {
                    EditorGUILayout.HelpBox($"参数 '{param.Name}' 的值无效", MessageType.Warning);
                }
            }
        }

        // 验证参数值是否有效
        private bool ValidateParameterValue(ApiParameterInfo param, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // 空值只有在没有默认值的情况下才需要验证
                return !param.HasDefaultValue;
            }

            // 根据参数类型进行验证（注意使用 InvariantCulture）
            switch (param.Type)
            {
                case "int":
                case "long":
                case "short":
                case "byte":
                    return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
                case "float":
                case "double":
                case "decimal":
                    return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
                case "bool":
                    return bool.TryParse(value, out _);
                default:
                    return true; // 字符串类型总是有效
            }
        }

        // 根据参数类型绘制适当的输入控件
        private string DrawParameterInputControl(ApiParameterInfo param, string currentValue)
        {
            string newValue = currentValue;

            if (param.Type == "bool")
            {
                bool boolValue = false;
                bool.TryParse(currentValue, out boolValue);
                boolValue = EditorGUILayout.Toggle(boolValue, GUILayout.Width(18));
                newValue = boolValue ? "true" : "false";
            }
            else if (param.Type == "int" || param.Type == "long" || param.Type == "short" || param.Type == "byte")
            {
                long longValue = 0;
                long.TryParse(currentValue, NumberStyles.Any, CultureInfo.InvariantCulture, out longValue);
                // 用 IntField 显示（如需 long 可自定义），这里简单转换
                int intShown = (int)Mathf.Clamp(longValue, int.MinValue, int.MaxValue);
                intShown = EditorGUILayout.IntField(intShown);
                newValue = intShown.ToString(CultureInfo.InvariantCulture);
            }
            else if (param.Type == "float" || param.Type == "double" || param.Type == "decimal")
            {
                double dblValue = 0.0;
                double.TryParse(currentValue, NumberStyles.Any, CultureInfo.InvariantCulture, out dblValue);
                float shown = (float)dblValue;
                shown = EditorGUILayout.FloatField(shown);
                newValue = ((double)shown).ToString(CultureInfo.InvariantCulture);
            }
            else if (param.Type == "char")
            {
                string s = EditorGUILayout.TextField(currentValue);
                newValue = string.IsNullOrEmpty(s) ? "" : s.Substring(0, 1);
            }
            else
            {
                newValue = EditorGUILayout.TextField(currentValue);
            }

            return newValue;
        }

        private void DrawRequestExecutionPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("请求设置", EditorStyles.boldLabel);

            // 显示当前服务器信息
            string currentServerUrl = "http://localhost:8080";
            string currentToken = "";

            // 从API服务器实例获取信息
            if (apiServer != null && apiServer.isRunning)
            {
                currentServerUrl = apiServer.serverUrl;
                currentToken = apiServer.accessToken;
            }

            EditorGUILayout.LabelField("当前服务器:", currentServerUrl);
            EditorGUILayout.LabelField("当前令牌:", string.IsNullOrEmpty(currentToken) ? "无" : "已设置");

            // 显示API服务器来源
            if (apiServer != null)
            {
                EditorGUILayout.LabelField("服务器来源:", "直接传递");
            }
            else
            {
                EditorGUILayout.LabelField("服务器来源:", "未设置（请通过主控制台打开）");
            }

            // 执行按钮
            if (GUILayout.Button("发送请求", GUILayout.Height(30)))
            {
                ExecuteRequest();
            }

            if (GUILayout.Button("测试连接"))
            {
                TestConnection();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawResponsePanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("响应结果", EditorStyles.boldLabel);

            if (showResponse)
            {
                EditorGUILayout.LabelField("状态码:", responseStatusCode.ToString());

                responseScrollPosition = EditorGUILayout.BeginScrollView(responseScrollPosition, GUILayout.Height(180));
                EditorGUILayout.TextArea(responseContent ?? "", GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("暂无响应数据");
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshRouteList()
        {
            try
            {
                registeredRoutes.Clear();

                // 只使用传入的API服务器实例
                if (apiServer != null && apiServer.isRunning)
                {
                    registeredRoutes.AddRange(apiServer.GetAllRoutes());
                }
                else
                {
                    // 如果没有API服务器实例，使用定义的路由
                    registeredRoutes.AddRange(GetDefinedRoutes());
                }

                // 创建显示名称数组
                routeDisplayNames = new string[registeredRoutes.Count];
                for (int i = 0; i < registeredRoutes.Count; i++)
                {
                    routeDisplayNames[i] = $"{registeredRoutes[i].method} {registeredRoutes[i].path}";
                }

                // 重置选择索引
                if (registeredRoutes.Count > 0)
                {
                    selectedRouteIndex = 0;
                    ResetParameterValues();
                    ParseRouteParameters();
                }
                else
                {
                    selectedRouteIndex = -1;
                }

                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新路由列表时出错: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"刷新路由列表时出错: {ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 获取所有定义的路由（无需启动服务器）
        /// </summary>
        private List<RouteInfo> GetDefinedRoutes()
        {
            var routeList = new List<RouteInfo>();

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
                                    routeList.Add(new RouteInfo
                                    {
                                        method = routeAttr.Method,
                                        path = routeAttr.Path,
                                        handler = type.Name + "." + method.Name
                                    });
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

            return routeList;
        }

        private void ResetParameterValues()
        {
            parameterValues.Clear();
        }

        private void ParseRouteParameters()
        {
            currentRouteParameters.Clear();

            if (registeredRoutes.Count == 0 || selectedRouteIndex < 0 || selectedRouteIndex >= registeredRoutes.Count)
            {
                return;
            }

            var selectedRoute = registeredRoutes[selectedRouteIndex];

            // 解析处理器名称，获取对应的控制器和方法
            var handlerParts = selectedRoute.handler.Split('.');
            if (handlerParts.Length >= 2)
            {
                string controllerName = handlerParts[0];
                string methodName = handlerParts[1];

                // 查找对应的控制器类型和方法
                var controllerType = FindControllerType(controllerName);
                if (controllerType != null)
                {
                    var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                    if (method != null)
                    {
                        // 获取方法参数
                        var parameters = method.GetParameters();
                        foreach (var param in parameters)
                        {
                            var paramInfo = new ApiParameterInfo
                            {
                                Name = param.Name,
                                Type = GetFriendlyTypeName(param.ParameterType),
                                HasDefaultValue = param.HasDefaultValue,
                                DefaultValue = FormatDefaultValue(param.DefaultValue, param.ParameterType)
                            };

                            currentRouteParameters.Add(paramInfo);

                            // 设置默认值
                            if (param.HasDefaultValue)
                            {
                                parameterValues[param.Name] = FormatDefaultValue(param.DefaultValue, param.ParameterType);
                            }
                            else
                            {
                                parameterValues[param.Name] = "";
                            }
                        }
                    }
                }
            }
        }

        // 根据控制器名称查找类型
        private Type FindControllerType(string controllerName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        // 检查类型名是否匹配，支持完整的命名空间匹配
                        if (type.Name == controllerName || (type.FullName != null && type.FullName.EndsWith("." + controllerName)))
                        {
                            // 检查是否在LocalRestAPI命名空间下
                            if (type.Namespace != null && type.Namespace.StartsWith("LocalRestAPI"))
                            {
                                return type;
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 忽略无法加载类型的程序集
                }
            }

            return null;
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(long)) return "long";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";
            if (type == typeof(decimal)) return "decimal";
            return type.Name;
        }

        private string FormatDefaultValue(object defaultValue, Type paramType)
        {
            if (defaultValue == null) return "";

            if (paramType == typeof(bool))
            {
                return defaultValue.ToString().ToLowerInvariant();
            }

            if (paramType == typeof(float) || paramType == typeof(double) || paramType == typeof(decimal))
            {
                return Convert.ToString(defaultValue, CultureInfo.InvariantCulture);
            }

            if (paramType == typeof(int) || paramType == typeof(long) || paramType == typeof(short) || paramType == typeof(byte))
            {
                return Convert.ToString(defaultValue, CultureInfo.InvariantCulture);
            }

            return defaultValue.ToString();
        }

        // 统一构建最终 URL（不在这里加 GET 查询参数）
        private string BuildBaseRequestUrl(RouteInfo selectedRoute)
        {
            string baseUrl = (apiServer != null && apiServer.isRunning)
                ? apiServer.serverUrl
                : "http://localhost:8080";

            baseUrl = baseUrl.TrimEnd('/');
            string path = selectedRoute.path.StartsWith("/") ? selectedRoute.path : "/" + selectedRoute.path;
            return baseUrl + path;
        }

        // 构建 GET 查询参数（仅在这里做），避免与其它位置重复
        private string BuildFinalUrlWithQuery(string baseUrl, string method)
        {
            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) || parameterValues.Count == 0)
                return baseUrl;

            var queryParts = new List<string>();
            foreach (var kv in parameterValues)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    queryParts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
                }
            }

            if (queryParts.Count == 0) return baseUrl;
            return baseUrl + "?" + string.Join("&", queryParts);
        }

        // 构建用于 JSON Body 的对象字符串，利用 currentRouteParameters
        private string BuildJsonPayloadForBody()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;

            foreach (var p in currentRouteParameters)
            {
                // 从 parameterValues 取值（可能不存在）
                parameterValues.TryGetValue(p.Name, out var raw);
                raw = raw ?? "";

                // 根据类型进行输出（统一 InvariantCulture）
                string jsonValue;
                switch (p.Type)
                {
                    case "int":
                    case "long":
                    case "short":
                    case "byte":
                        if (string.IsNullOrWhiteSpace(raw))
                            jsonValue = "null"; // 或者 "0"/跳过，依据后端约定
                        else if (long.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var l))
                            jsonValue = l.ToString(CultureInfo.InvariantCulture);
                        else
                            jsonValue = "null";
                        break;

                    case "float":
                    case "double":
                    case "decimal":
                        if (string.IsNullOrWhiteSpace(raw))
                            jsonValue = "null";
                        else if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                            jsonValue = d.ToString(CultureInfo.InvariantCulture);
                        else
                            jsonValue = "null";
                        break;

                    case "bool":
                        if (string.IsNullOrWhiteSpace(raw))
                            jsonValue = "null";
                        else if (bool.TryParse(raw, out var b))
                            jsonValue = b ? "true" : "false";
                        else
                            jsonValue = "null";
                        break;

                    case "char":
                        if (string.IsNullOrEmpty(raw))
                            jsonValue = "null";
                        else
                            jsonValue = "\"" + EscapeJsonString(raw.Substring(0, 1)) + "\"";
                        break;

                    case "string":
                    default:
                        // 字符串类型，允许空字符串；如需空→null，可按需调整
                        jsonValue = "\"" + EscapeJsonString(raw) + "\"";
                        break;
                }

                if (!first) sb.Append(',');
                sb.Append('"').Append(EscapeJsonKey(p.Name)).Append("\":").Append(jsonValue);
                first = false;
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string EscapeJsonString(string str)
        {
            if (str == null) return "";
            return str
                   .Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\b", "\\b")
                   .Replace("\f", "\\f")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
        }

        private string EscapeJsonKey(string key)
        {
            // 参数名通常安全，但做最低限度转义
            return (key ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // 发送HTTP请求(异步版本) —— 单点构建 GET 查询与 Body，并按请求设置 Authorization
        private async Task SendHttpRequestAsync(string baseUrl, RouteInfo selectedRoute)
        {
            try
            {
                var method = selectedRoute.method?.ToUpperInvariant() ?? "GET";
                HttpResponseMessage response;

                // 构建 HttpRequestMessage
                var requestMessage = new HttpRequestMessage(new HttpMethod(method), BuildFinalUrlWithQuery(baseUrl, method));

                // 设置授权头（避免静态 HttpClient DefaultRequestHeaders 并发污染）
                var token = !string.IsNullOrEmpty(accessToken) ? accessToken : apiServer?.accessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    requestMessage.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                // 如果是需要 body 的方法
                if (method == "POST" || method == "PUT" || method == "PATCH")
                {
                    string jsonBody = BuildJsonPayloadForBody();
                    if (string.IsNullOrEmpty(jsonBody))
                    {
                        jsonBody = "{}";
                    }

                    
                    //设置Content-Type头部
                    requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    Debug.Log("构建的body" + requestMessage.Content.ReadAsStringAsync().Result);
                }

                var url = requestMessage.RequestUri;
                Debug.Log("发送请求到: " + url);
                response = await httpClient.SendAsync(requestMessage);

                responseStatusCode = (int)response.StatusCode;
                responseContent = await response.Content.ReadAsStringAsync();
                
                showResponse = true;
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送HTTP请求时发生异常: {ex.Message}");

                responseStatusCode = 0;
                responseContent = $"发送请求时发生异常: {ex.Message}";
                showResponse = true;
                Repaint();
            }
        }

        // 修改 ExecuteRequest 使用新流程
        private async void ExecuteRequest()
        {
            if (!ValidateRequest()) return;

            var selectedRoute = registeredRoutes[selectedRouteIndex];
            var baseUrl = BuildBaseRequestUrl(selectedRoute);

            Debug.Log($"发送请求到: {baseUrl}, 方法: {selectedRoute.method}");

            await SendHttpRequestAsync(baseUrl, selectedRoute);
        }

        // 测试连接（异步）
        private async void TestConnection()
        {
            string serverUrl = "http://localhost:8080"; // 默认URL

            if (apiServer != null && apiServer.isRunning)
            {
                serverUrl = apiServer.serverUrl;
            }
            else
            {
                responseContent = "API服务器未运行或未设置。请通过主控制台的测试API按钮打开此窗口。";
                responseStatusCode = 0;
                showResponse = true;
                Repaint();
                return;
            }

            // 规范化 URL
            serverUrl = serverUrl.TrimEnd('/');
            string testUrl = serverUrl + "/api/routes";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, testUrl);

                // 设置授权头
                var token = !string.IsNullOrEmpty(accessToken) ? accessToken : apiServer?.accessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var resp = await httpClient.SendAsync(request);

                responseStatusCode = (int)resp.StatusCode;
                var text = await resp.Content.ReadAsStringAsync();
                responseContent = $"连接测试完成.\n状态码: {resp.StatusCode}\n响应:\n{text}";
                showResponse = true;
                Repaint();
            }
            catch (Exception ex)
            {
                responseStatusCode = 0;
                responseContent = $"连接测试异常!\n错误: {ex.Message}";
                showResponse = true;
                Repaint();
            }
        }

        // 验证请求参数
        private bool ValidateRequest()
        {
            // 检查是否选择了有效的路由
            if (registeredRoutes.Count == 0 || selectedRouteIndex < 0 || selectedRouteIndex >= registeredRoutes.Count)
            {
                responseContent = "请选择一个有效的API路由";
                responseStatusCode = 0;
                showResponse = true;
                Repaint();
                return false;
            }

            // 检查API服务器实例是否运行
            if (apiServer == null || !apiServer.isRunning)
            {
                responseContent = "API服务器未运行，请先启动服务器。请确保在主控制台中启动服务，然后通过主控制台的测试API按钮打开此窗口。";
                responseStatusCode = 0;
                showResponse = true;
                Repaint();
                return false;
            }

            return true;
        }
    }
}