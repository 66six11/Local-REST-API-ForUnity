using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiTestWindow : EditorWindow
    {
        private static ApiTestWindow window;
        
        // API选择和测试相关
        private List<string> apiRoutes = new List<string>();
        private List<string> apiMethods = new List<string>();
        private int selectedApiIndex = 0;
        private string requestUrl = "";
        private string requestMethod = "GET";
        private string requestBody = "";
        private string responseText = "";
        private int responseCode = 0;
        private string accessToken = "";
        
        // 滚动视图
        private Vector2 scrollPosition;
        // 请求状态
        private bool isRequestInProgress = false;
        // 当前请求的访问令牌
        private string currentAccessToken = "";
        
        [MenuItem("Tools/Local REST API/API测试工具")]
        public static void ShowWindow()
        {
            window = GetWindow<ApiTestWindow>("API 测试工具");
            window.minSize = new Vector2(700, 500);
            window.LoadApiRoutes();
        }
        
        private void OnEnable()
        {
            // 尝试更新主窗口实例引用
            UpdateMainWindowInstance();
            
            accessToken = RestApiMainWindow.GetAccessToken();
            LoadApiRoutes();
            
            // 定期更新服务状态
            EditorApplication.update += UpdateServiceStatus;
        }
        
        private void UpdateMainWindowInstance()
        {
            // 尝试强制更新主窗口实例引用
            // 通过反射直接查找现有的窗口实例
            var mainWindowType = typeof(RestApiMainWindow);
            var windowField = mainWindowType.GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var currentWindow = windowField?.GetValue(null);
            
            if (currentWindow == null)
            {
                // 如果当前没有窗口实例，尝试查找现有的窗口
                var existingWindows = Resources.FindObjectsOfTypeAll(mainWindowType);
                if (existingWindows.Length > 0)
                {
                    windowField?.SetValue(null, existingWindows[0]);
                }
            }
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= UpdateServiceStatus;
        }
        
        private void UpdateServiceStatus()
        {
            // 每秒检查一次服务状态
            if (EditorApplication.timeSinceStartup % 1 < 0.1f)
            {
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            // 标题
            GUILayout.Label("API 测试工具", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space();
            
            // 服务状态显示
            DrawServiceStatus();
            
            EditorGUILayout.Space();
            
            // API选择部分
            DrawApiSelection();
            
            EditorGUILayout.Space();
            
            // 请求构建部分
            DrawRequestBuilder();
            
            EditorGUILayout.Space();
            
            // 发送请求按钮
            DrawSendRequestButton();
            
            EditorGUILayout.Space();
            
            
            
            EditorGUILayout.Space();
            
            // 响应显示部分
            DrawResponseDisplay();
        }
        
        private void DrawServiceStatus()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("服务状态", EditorStyles.boldLabel);
            
            bool isServiceRunning = CheckServiceRunningStatus();
            string token = RestApiMainWindow.GetAccessToken();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("服务状态");
            EditorGUILayout.LabelField(isServiceRunning ? "运行中" : "未运行", isServiceRunning ? "BoldLabel" : "Label");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("访问令牌");
            EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(token) ? "未设置" : token, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
            
            // 如果服务未运行，显示提示信息
            if (!isServiceRunning)
            {
                EditorGUILayout.HelpBox("请确保在主控制台中启动API服务", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private bool CheckServiceRunningStatus()
        {
            // 首先尝试使用现有的方法
            bool status = RestApiMainWindow.IsServiceRunning();
            
            // 如果返回false，尝试其他方法来确认
            if (!status)
            {
                // 检查是否存在主窗口实例
                if (RestApiMainWindow.HasWindowInstance())
                {
                    // 尝试通过反射获取实际的状态
                    var mainWindowType = typeof(RestApiMainWindow);
                    var windowField = mainWindowType.GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var windowInstance = windowField?.GetValue(null);
                    
                    if (windowInstance != null)
                    {
                        var isRunningField = mainWindowType.GetField("isServiceRunning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        status = (bool)(isRunningField?.GetValue(windowInstance) ?? false);
                    }
                }
            }
            
            return status;
        }
        
        private void DrawApiSelection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("选择API接口", EditorStyles.boldLabel);
            
            bool isServiceRunning = CheckServiceRunningStatus();
            
            if (apiRoutes.Count > 0)
            {
                // 创建显示选项（包含方法和路径）
                var displayOptions = new List<string>();
                for (int i = 0; i < apiRoutes.Count; i++)
                {
                    displayOptions.Add($"{apiMethods[i]} {apiRoutes[i]}");
                }
                
                selectedApiIndex = EditorGUILayout.Popup("API接口", selectedApiIndex, displayOptions.ToArray());
                
                // 更新URL和方法
                if (selectedApiIndex < apiRoutes.Count)
                {
                    requestUrl = apiRoutes[selectedApiIndex];
                    requestMethod = apiMethods[selectedApiIndex];
                }
            }
            else
            {
                if (isServiceRunning)
                {
                    EditorGUILayout.LabelField("未找到已注册的API接口");
                }
                else
                {
                    EditorGUILayout.LabelField("服务未运行，无法获取API接口");
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新API列表"))
            {
                LoadApiRoutes();
            }
            
            if (GUILayout.Button("刷新服务状态"))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRequestBuilder()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("请求配置", EditorStyles.boldLabel);
            
            // URL输入
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("请求URL");
            requestUrl = EditorGUILayout.TextField(requestUrl);
            EditorGUILayout.EndHorizontal();
            
            // 方法选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("请求方法");
            requestMethod = EditorGUILayout.TextField(requestMethod);
            EditorGUILayout.EndHorizontal();
            
            // 请求体（仅对POST/PUT请求显示）
            if (requestMethod == "POST" || requestMethod == "PUT")
            {
                GUILayout.Label("请求体 (JSON):", EditorStyles.boldLabel);
                requestBody = EditorGUILayout.TextArea(requestBody, GUILayout.Height(100));
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSendRequestButton()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("发送请求", GUILayout.Height(30)))
            {
                SendApiRequest();
            }
            
            if (GUILayout.Button("清空响应", GUILayout.Height(30)))
            {
                responseText = "";
                responseCode = 0;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawResponseDisplay()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("响应结果", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("状态码");
            EditorGUILayout.SelectableLabel(responseCode.ToString(), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Label("响应内容:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            EditorGUILayout.SelectableLabel(responseText, EditorStyles.textArea);
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void LoadApiRoutes()
        {
            apiRoutes.Clear();
            apiMethods.Clear();
            
            // 强制更新主窗口实例引用
            UpdateMainWindowInstance();
            
            bool isServiceRunning = CheckServiceRunningStatus();
            if (isServiceRunning)
            {
                var apiServer = GetApiServerInstance();
                if (apiServer != null)
                {
                    var routes = apiServer.GetDetailedRoutes();
                    foreach (var route in routes)
                    {
                        var parts = route.Key.Split(' ');
                        if (parts.Length >= 2)
                        {
                            apiMethods.Add(parts[0]);
                            apiRoutes.Add(parts[1]);
                        }
                    }
                }
                // 如果无法获取API服务器实例，不添加任何路由，而是让用户知道服务可能未就绪
            }
            else
            {
                // 不显示带警告的路由，而是让用户知道服务未运行
                // 可以选择不添加任何路由或显示空列表
            }
            
            if (apiRoutes.Count > 0 && selectedApiIndex >= apiRoutes.Count)
            {
                selectedApiIndex = 0;
            }
        }
        
        private ApiServer GetApiServerInstance()
        {
            // 直接通过RestApiMainWindow的公共方法获取API服务器实例
            return RestApiMainWindow.GetApiServerInstance();
        }
        
        private void SendApiRequest()
        {
            if (string.IsNullOrEmpty(requestUrl))
            {
                responseText = "错误: 请求URL不能为空";
                responseCode = 0;
                return;
            }
            
            // 确保获取最新的服务状态和访问令牌
            bool isServiceRunning = CheckServiceRunningStatus();
            accessToken = RestApiMainWindow.GetAccessToken();
            
            if (!isServiceRunning)
            {
                responseText = "错误: API服务未运行";
                responseCode = 0;
                return;
            }
            
            if (string.IsNullOrEmpty(accessToken))
            {
                responseText = "错误: 无法获取访问令牌";
                responseCode = 0;
                return;
            }
            
            // 获取当前服务器URL（从主窗口获取）
            string serverUrl = GetServerUrl();
            if (string.IsNullOrEmpty(serverUrl))
            {
                serverUrl = "http://localhost:8080/";
            }
            
            string fullUrl = serverUrl.TrimEnd('/') + requestUrl;
            
            // 显示加载状态
            responseText = "正在发送请求...";
            responseCode = 0;
            isRequestInProgress = true;
            Repaint();
            
            // 使用异步方式发送请求
            System.Threading.ThreadPool.QueueUserWorkItem(SendApiRequestAsync, fullUrl);
        }
        
        private void SendApiRequestAsync(object state)
        {
            string fullUrl = (string)state;
            // 在开始请求时保存当前的访问令牌
            string requestAccessToken = accessToken;
            
            try
            {
                var request = WebRequest.Create(fullUrl);
                request.Method = requestMethod;
                request.Headers.Add("Authorization", "Bearer " + requestAccessToken);
                request.Timeout = 10000; // 10秒超时
                
                // 如果是POST或PUT请求，添加请求体
                if ((requestMethod == "POST" || requestMethod == "PUT") && !string.IsNullOrEmpty(requestBody))
                {
                    request.ContentType = "application/json";
                    
                    using (var writer = new StreamWriter(request.GetRequestStream()))
                    {
                        writer.Write(requestBody);
                    }
                }
                
                var webResponse = request.GetResponse() as HttpWebResponse;
                responseCode = (int)webResponse.StatusCode;
                
                using (var stream = webResponse.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    responseText = reader.ReadToEnd();
                }
                
                webResponse.Close();
            }
            catch (WebException ex)
            {
                var webResponse = ex.Response as HttpWebResponse;
                responseCode = (int)(webResponse?.StatusCode ?? 0);
                
                if (webResponse != null)
                {
                    using (var stream = webResponse.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        responseText = reader.ReadToEnd();
                    }
                }
                else
                {
                    responseText = $"请求失败: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                responseText = $"发生错误: {ex.Message}";
                responseCode = 0;
            }
            finally
            {
                isRequestInProgress = false;
                // 在主线程中更新UI
                EditorApplication.delayCall += () => {
                    Repaint();
                };
            }
        }
        
        private string GetServerUrl()
        {
            // 通过反射获取主窗口中的serverUrl
            var mainWindowType = typeof(RestApiMainWindow);
            var windowField = mainWindowType.GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var windowInstance = windowField?.GetValue(null);
            
            if (windowInstance != null)
            {
                var serverUrlField = mainWindowType.GetField("serverUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return serverUrlField?.GetValue(windowInstance) as string;
            }
            
            return null;
        }
    }
}