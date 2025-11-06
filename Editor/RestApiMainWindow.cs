using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class RestApiMainWindow : EditorWindow
    {
        private static RestApiMainWindow window;

        // API服务器实例
        private ApiServer apiServer;

        // 服务状态
        private bool isServiceRunning = false;
        private string serviceStatus = "未启动";
        private string serverUrl = "http://localhost:8080/";

        // 日志相关
        private Vector2 logScrollPosition;
        private List<string> logs = new List<string>();
        private int maxLogEntries = 100;

        // 性能监控相关
        private float requestsPerSecond = 0f;
        private int totalRequests = 0;
        private float averageResponseTime = 0f;

        // API路由列表
        private Vector2 routeScrollPosition;
        private List<string> registeredRoutes = new List<string>();

        // 访问令牌
        private string accessToken = "";

        [MenuItem("Tools/Local REST API/主控制台")]
        public static void ShowWindow()
        {
            window = GetWindow<RestApiMainWindow>("Local REST API");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            // 只在访问令牌为空时初始化
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = Guid.NewGuid().ToString("N");
            }

            // 注册日志回调
            Application.logMessageReceived += HandleLog;

            // 定期更新性能指标
            EditorApplication.update += UpdatePerformanceMetrics;
        }

        private void OnDisable()
        {
            // 取消注册日志回调
            Application.logMessageReceived -= HandleLog;

            // 停止服务
            StopService();

            // 取消注册性能指标更新
            EditorApplication.update -= UpdatePerformanceMetrics;
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            AddLog($"[{type}] {condition}");
        }

        private void UpdatePerformanceMetrics()
        {
            // 每秒更新一次性能指标
            if (EditorApplication.timeSinceStartup % 1 < 0.1f)
            {
                var monitor = ApiPerformanceMonitor.Instance;
                requestsPerSecond = monitor.GetRequestsPerSecond();
                totalRequests = monitor.GetTotalRequests();
                averageResponseTime = monitor.GetAverageResponseTime();

                // 重绘窗口以更新显示
                Repaint();
            }
        }

        private void OnGUI()
        {
            // 标题
            GUILayout.Label("Local REST API 服务控制台", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space();

            // 服务控制面板
            DrawServiceControlPanel();

            // 访问令牌设置
            DrawAccessTokenSettings();

            // 性能监控面板
            DrawPerformancePanel();

            // API路由列表
            DrawRouteList();

            // 日志面板
            DrawLogPanel();
        }

        private void DrawServiceControlPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("服务控制", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("服务状态");
            EditorGUILayout.LabelField(serviceStatus, isServiceRunning ? "BoldLabel" : "Label");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("服务地址");
            if (!isServiceRunning)
            {
                serverUrl = EditorGUILayout.TextField(serverUrl);
            }
            else
            {
                EditorGUILayout.SelectableLabel(serverUrl, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isServiceRunning ? "停止服务" : "启动服务", GUILayout.Height(30)))
            {
                if (isServiceRunning)
                {
                    StopService();
                }
                else
                {
                    StartService();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawAccessTokenSettings()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("访问令牌", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("访问令牌");
            accessToken = EditorGUILayout.TextField(accessToken);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新生成令牌"))
            {
                accessToken = Guid.NewGuid().ToString("N");
                AddLog("访问令牌已重新生成");
            }

            if (GUILayout.Button("复制令牌"))
            {
                EditorGUIUtility.systemCopyBuffer = accessToken;
                AddLog("访问令牌已复制到剪贴板");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPerformancePanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("性能监控", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("请求速率");
            EditorGUILayout.LabelField($"{requestsPerSecond:F2} req/s");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("总请求数");
            EditorGUILayout.LabelField(totalRequests.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("平均响应时间");
            EditorGUILayout.LabelField($"{averageResponseTime:F2} ms");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawRouteList()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("已注册API路由", EditorStyles.boldLabel);

            routeScrollPosition = EditorGUILayout.BeginScrollView(routeScrollPosition, GUILayout.Height(150));

            if (registeredRoutes.Count == 0)
            {
                EditorGUILayout.LabelField("暂无注册路由");
            }
            else
            {
                for (int i = 0; i < registeredRoutes.Count; i++)
                {
                    var route = registeredRoutes[i];
                    var routeContent = new GUIContent(route);

                    // 检查是否鼠标悬停在该路由上
                    var rect = EditorGUILayout.GetControlRect();
                    if (GUI.Button(rect, routeContent, GUIStyle.none))
                    {
                        // 解析路由信息并尝试打开对应的代码文件
                        OpenRouteCode(registeredRoutes[i]);
                    }

                    // 如果鼠标悬停，显示手型光标
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新路由列表"))
            {
                RefreshRouteList();
            }

            if (GUILayout.Button("跳转到API服务器"))
            {
                // 跳转到API服务器代码
                var serverScript = AssetDatabase.FindAssets("ApiServer t:Script");
                if (serverScript.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(serverScript[0]);
                    var obj = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawLogPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("服务日志", EditorStyles.boldLabel);

            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));

            foreach (var log in logs)
            {
                // 使用可选择的文本字段显示日志，使用户可以复制
                EditorGUILayout.SelectableLabel(log, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空日志"))
            {
                logs.Clear();
            }

            if (GUILayout.Button("导出日志"))
            {
                ExportLogs();
            }

            if (GUILayout.Button("复制日志"))
            {
                CopyLogsToClipboard();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void StartService()
        {
            try
            {
                // 确保URL以斜杠结尾
                if (!serverUrl.EndsWith("/"))
                {
                    serverUrl += "/";
                }

                // 确保URL格式正确
                if (!serverUrl.StartsWith("http://") && !serverUrl.StartsWith("https://"))
                {
                    serverUrl = "http://" + serverUrl;
                }

                if (apiServer == null)
                {
                    apiServer = new ApiServer(serverUrl, accessToken);
                }

                apiServer.Start();
                isServiceRunning = apiServer.IsRunning();
                serviceStatus = isServiceRunning ? "运行中" : "启动失败";

                if (isServiceRunning)
                {
                    AddLog("REST API服务已启动");
                    AddLog($"监听地址: {serverUrl}");
                    AddLog($"访问令牌: {accessToken}");

                    // 刷新路由列表
                    RefreshRouteList();
                }
                else
                {
                    AddLog("REST API服务启动失败");
                }
            }
            catch (Exception ex)
            {
                AddLog($"启动服务失败: {ex.Message}");
                isServiceRunning = false;
                serviceStatus = "启动失败";
            }
        }

        private void StopService()
        {
            try
            {
                if (apiServer != null)
                {
                    apiServer.Stop();
                    apiServer = null;
                }

                isServiceRunning = false;
                serviceStatus = "已停止";
                AddLog("REST API服务已停止");
            }
            catch (Exception ex)
            {
                AddLog($"停止服务失败: {ex.Message}");
            }
        }

        private void RefreshRouteList()
        {
            registeredRoutes.Clear();

            if (apiServer != null)
            {
                var routes = apiServer.GetDetailedRoutes();
                foreach (var route in routes)
                {
                    var parts = route.Key.Split(' ');
                    registeredRoutes.Add($"{parts[0]} {parts[1]} - {route.Value.methodInfo.DeclaringType.Name}.{route.Value.methodInfo.Name}");
                }

                AddLog($"路由列表已刷新，共 {routes.Count} 个路由");
            }
            else
            {
                // 如果服务未启动，显示示例路由
                registeredRoutes.Add("GET /api/sample/hello - SampleController.Hello");
                registeredRoutes.Add("POST /api/sample/echo - SampleController.Echo");
                registeredRoutes.Add("GET /api/sample/random - SampleController.GetRandom");
                registeredRoutes.Add("GET /api/sample/status - SampleController.GetStatus");
                registeredRoutes.Add("GET /api/unity/scene - UnityController.GetActiveScene");
                registeredRoutes.Add("GET /api/unity/objects - UnityController.GetObjectsInScene");
                registeredRoutes.Add("POST /api/unity/log - UnityController.LogMessage");
                registeredRoutes.Add("GET /api/routes - 内置路由");

                AddLog("显示示例路由列表");
            }
        }

        private void OpenRouteCode(string routeInfo)
        {
            try
            {
                // 解析路由信息 "GET /api/sample/hello - SampleController.Hello"
                var parts = routeInfo.Split(new string[] { " - " }, StringSplitOptions.None);
                if (parts.Length < 2) return;

                var handler = parts[1]; // "SampleController.Hello"
                var handlerParts = handler.Split('.');
                if (handlerParts.Length < 2) return;

                var className = handlerParts[0];  // "SampleController"
                var methodName = handlerParts[1]; // "Hello"

                // 查找对应的脚本文件
                var scriptGUIDs = AssetDatabase.FindAssets($"{className} t:Script");
                if (scriptGUIDs.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(scriptGUIDs[0]);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                    if (script != null)
                    {
                        // 打开脚本并跳转到方法
                        EditorGUIUtility.PingObject(script);
                        Selection.activeObject = script;
                        AssetDatabase.OpenAsset(script);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("无法找到脚本", $"无法找到类 {className} 对应的脚本文件", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"打开路由代码失败: {ex.Message}");
            }
        }

        private void ExportLogs()
        {
            try
            {
                string path = EditorUtility.SaveFilePanel("导出日志", "", "api-logs.txt", "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (var log in logs)
                    {
                        sb.AppendLine(log);
                    }

                    System.IO.File.WriteAllText(path, sb.ToString());
                    AddLog($"日志已导出到: {path}");
                }
            }
            catch (System.Exception ex)
            {
                AddLog($"导出日志失败: {ex.Message}");
            }
        }

        private void CopyLogsToClipboard()
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (var log in logs)
                {
                    sb.AppendLine(log);
                }

                EditorGUIUtility.systemCopyBuffer = sb.ToString();
                AddLog("日志已复制到剪贴板");
            }
            catch (System.Exception ex)
            {
                AddLog($"复制日志失败: {ex.Message}");
            }
        }

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";

            logs.Add(logEntry);

            // 限制日志数量
            if (logs.Count > maxLogEntries)
            {
                logs.RemoveAt(0);
            }

            // 自动滚动到最新日志
            logScrollPosition.y = float.MaxValue;

            // 重绘窗口
            Repaint();
        }

        // 公共方法供其他组件调用
        public static void Log(string message)
        {
            if (window != null)
            {
                window.AddLog(message);
            }
        }

        public static string GetAccessToken()
        {
            return window?.accessToken ?? "";
        }

        public static bool IsServiceRunning()
        {
            return window?.isServiceRunning ?? false;
        }

        public static bool HasWindowInstance()
        {
            return window != null;
        }

        public static bool HasApiServerInstance()

        {
            return window?.apiServer != null;
        }


        public static ApiServer GetApiServerInstance()

        {
            return window?.apiServer;
        }
    }
}