using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using LocalRestAPI.Runtime;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiTestWindow : EditorWindow
    {
        private static ApiTestWindow window;

        // API服务器实例
        private ApiServer apiServer;

        // API路由选择相关
        private List<RouteInfo> registeredRoutes = new List<RouteInfo>();
        private int selectedRouteIndex = 0;
        private string[] routeDisplayNames;

        // 请求参数相关
        private Dictionary<string, string> parameterValues = new Dictionary<string, string>();
        private List<ApiParameterInfo> currentRouteParameters = new List<ApiParameterInfo>();

        // 请求结果相关
        private string responseContent = "";
        private int responseStatusCode = 0;
        private bool showResponse = false;
        private Vector2 responseScrollPosition;

        // 请求设置相关
        private string customUrl = "";
        private string accessToken = "";
        private Vector2 parameterScrollPosition;

        // 参数信息类
        public class ApiParameterInfo
        {
            public string Name;
            public string Type;
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

            parameterScrollPosition = EditorGUILayout.BeginScrollView(parameterScrollPosition, GUILayout.Height(150));

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
                EditorGUILayout.LabelField($"{param.Name} ({param.Type})", GUILayout.Width(150));

                string valueKey = param.Name;
                string currentValue = parameterValues.ContainsKey(valueKey) ? parameterValues[valueKey] : "";

                // 根据参数类型显示不同的输入控件
                string newValue = DrawParameterInputControl(param, currentValue);

                parameterValues[valueKey] = newValue;

                // 显示默认值提示
                if (param.HasDefaultValue)
                {
                    EditorGUILayout.LabelField($"默认: {param.DefaultValue}", EditorStyles.miniLabel, GUILayout.Width(80));
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

            // 根据参数类型进行验证
            switch (param.Type)
            {
                case "int":
                    return int.TryParse(value, out _);
                case "float":
                case "double":
                    return float.TryParse(value, out _);
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
                boolValue = EditorGUILayout.Toggle(boolValue, GUILayout.Width(15));
                newValue = boolValue.ToString().ToLower();
            }
            else if (param.Type == "int")
            {
                int intValue = 0;
                int.TryParse(currentValue, out intValue);
                intValue = EditorGUILayout.IntField(intValue);
                newValue = intValue.ToString();
            }
            else if (param.Type == "float" || param.Type == "double")
            {
                float floatValue = 0f;
                float.TryParse(currentValue, out floatValue);
                floatValue = EditorGUILayout.FloatField(floatValue);
                newValue = floatValue.ToString();
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

            // 自定义URL
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("自定义URL");
            customUrl = EditorGUILayout.TextField(customUrl);
            EditorGUILayout.EndHorizontal();

            // 访问令牌
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("访问令牌");
            accessToken = EditorGUILayout.TextField(accessToken);
            EditorGUILayout.EndHorizontal();

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

                responseScrollPosition = EditorGUILayout.BeginScrollView(responseScrollPosition, GUILayout.Height(150));
                EditorGUILayout.TextArea(responseContent, GUILayout.ExpandHeight(true));
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
                    registeredRoutes = apiServer.GetAllRoutes();
                }
                else
                {
                    // 如果没有API服务器实例，使用定义的路由
                    registeredRoutes = GetDefinedRoutes();
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
        /// <returns>路由信息列表</returns>
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
                        if (type.Name == controllerName || type.FullName.EndsWith("." + controllerName))
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

        // 格式化默认值以适应UI显示


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

            // 对于某些类型，进行特殊格式化
            if (paramType == typeof(bool))
            {
                return defaultValue.ToString().ToLower();
            }

            return defaultValue.ToString();
        }

        private void TestConnection()
        {
            string serverUrl = "http://localhost:8000";

            // 从API服务器实例获取URL
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

            // 确保URL格式正确
            if (!serverUrl.EndsWith("/"))
            {
                serverUrl += "/";
            }

            string testUrl = serverUrl + "api/routes"; // 测试API路由端点

            try
            {
                Debug.Log($"测试连接到: {testUrl}");

                var request = (HttpWebRequest)WebRequest.Create(testUrl);
                request.Method = "GET";
                request.Timeout = 5000; // 5秒超时

                // 添加访问令牌
                string token = !string.IsNullOrEmpty(accessToken) ? accessToken : apiServer.accessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers["Authorization"] = "Bearer " + token;
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    responseStatusCode = (int)response.StatusCode;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseContent = $"连接测试成功!\n状态码: {response.StatusCode}\n响应: {reader.ReadToEnd()}";
                    }
                }

                showResponse = true;
                Repaint();
            }
            catch (WebException ex)
            {
                responseStatusCode = 0;
                if (ex.Response is HttpWebResponse response)
                {
                    responseStatusCode = (int)response.StatusCode;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseContent = $"连接测试失败!\n状态码: {response.StatusCode}\n错误: {ex.Message}\n响应: {reader.ReadToEnd()}";
                    }
                }
                else
                {
                    responseContent = $"连接测试失败!\n错误: {ex.Message}\n状态: {ex.Status}";
                }

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

        private void ExecuteRequest()
        {
            if (!ValidateRequest())
            {
                return;
            }

            var selectedRoute = registeredRoutes[selectedRouteIndex];
            string requestUrl = BuildRequestUrl(selectedRoute);

            try
            {
                Debug.Log($"发送请求到: {requestUrl}"); // 添加调试日志

                var request = CreateHttpRequest(requestUrl, selectedRoute);
                SendHttpRequest(request, selectedRoute);
            }
            catch (WebException ex)
            {
                HandleWebException(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"执行请求时发生异常: {ex.Message}");
                responseStatusCode = 0;
                responseContent = $"请求失败: {ex.Message}";
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

        // 构建请求URL
        private string BuildRequestUrl(RouteInfo selectedRoute)
        {
            string baseUrl = "http://localhost:8080/"; // 默认URL

            // 只使用API服务器实例的信息
            if (apiServer != null && apiServer.isRunning)
            {
                // 获取主窗口的服务器URL
                baseUrl = apiServer.serverUrl;
            }
            else
            {
                // 如果没有API服务器实例，使用默认URL
                Debug.LogWarning("API服务器实例为空或未运行，使用默认URL。请通过主控制台打开API测试窗口。");
            }

            // 确保baseUrl以/结尾
            if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }

            string requestUrl = customUrl;
            if (string.IsNullOrEmpty(requestUrl))
            {
                // 确保路径格式正确
                string path = selectedRoute.path;
                if (!path.StartsWith("/"))
                {
                    path = "/" + path;
                }

                // 确保路径不以多个斜杠开始
                // path = "/" + path.TrimStart('/');
                requestUrl = baseUrl.TrimEnd('/') + path;
            }

            // 添加查询参数
            if (selectedRoute.method == "GET" && parameterValues.Count > 0)
            {
                bool hasQuery = requestUrl.Contains("?");
                foreach (var param in parameterValues)
                {
                    if (!string.IsNullOrEmpty(param.Value))
                    {
                        requestUrl += (hasQuery ? "&" : "?") + param.Key + "=" + Uri.EscapeDataString(param.Value);
                        hasQuery = true;
                    }
                }
            }

            Debug.Log($"构建的请求URL: {requestUrl}"); // 添加调试日志
            return requestUrl;
        }

        // 创建HTTP请求
        private HttpWebRequest CreateHttpRequest(string requestUrl, RouteInfo selectedRoute)
        {
            var request = (HttpWebRequest)WebRequest.Create(requestUrl);
            request.Method = selectedRoute.method;
            request.ContentType = "application/json";
            request.Timeout = 15000;          // 设置15秒超时
            request.ReadWriteTimeout = 15000; // 设置读写超时

            // 添加访问令牌
            string token = !string.IsNullOrEmpty(accessToken) ? accessToken : apiServer.accessToken;
            Debug.Log($"使用访问令牌: {(!string.IsNullOrEmpty(token) ? "是" : "否")}"); // 添加调试日志

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers["Authorization"] = "Bearer " + token;
                Debug.Log($"设置Authorization头: Bearer {token}"); // 添加调试日志
            }

            return request;
        }

        // 发送HTTP请求
        private void SendHttpRequest(HttpWebRequest request, RouteInfo selectedRoute)
        {
            try
            {
                // 如果是POST请求且有参数，添加请求体
                if (selectedRoute.method == "POST" && parameterValues.Count > 0)
                {
                    AddPostData(request);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    responseStatusCode = (int)response.StatusCode;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseContent = reader.ReadToEnd();
                    }
                }

                showResponse = true;
                Repaint();
            }
            catch (WebException webEx)
            {
                HandleWebException(webEx);
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

        // 添加POST数据
        private void AddPostData(HttpWebRequest request)
        {
            var postData = new Dictionary<string, string>();
            foreach (var param in parameterValues)
            {
                if (!string.IsNullOrEmpty(param.Value))
                {
                    postData[param.Key] = param.Value;
                }
            }

            if (postData.Count > 0)
            {
                string json = BuildJsonFromParameters(postData);
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
        }

        // 从参数构建JSON
        private string BuildJsonFromParameters(Dictionary<string, string> postData)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var param in postData)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{param.Key}\":\"{param.Value}\"");
                first = false;
            }

            sb.Append("}");
            return sb.ToString();
        }

        // 处理Web异常
        private void HandleWebException(WebException ex)
        {
            Debug.LogError($"Web异常: {ex.Message}, 状态: {ex.Status}");

            if (ex.Response is HttpWebResponse response)
            {
                responseStatusCode = (int)response.StatusCode;
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    responseContent = reader.ReadToEnd();
                }
            }
            else
            {
                responseStatusCode = 0;
                responseContent = $"请求失败: {ex.Message}\n异常状态: {ex.Status}";

                // 根据不同的异常状态提供更具体的错误信息
                switch (ex.Status)
                {
                    case WebExceptionStatus.Timeout:
                        responseContent += "\n错误: 请求超时，请检查服务器是否正在运行";
                        break;
                    case WebExceptionStatus.ConnectFailure:
                        responseContent += "\n错误: 连接失败，请检查服务器地址是否正确";
                        break;
                    case WebExceptionStatus.NameResolutionFailure:
                        responseContent += "\n错误: DNS解析失败，请检查主机名";
                        break;
                    default:
                        responseContent += $"\n错误状态: {ex.Status}";
                        break;
                }
            }

            showResponse = true;
            Repaint();
        }
    }
}