using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LocalRestAPI
{
    public class ApiTestWindow : EditorWindow
    {
        private static ApiTestWindow window;
        
        // 测试参数
        private string serverUrl = "http://localhost:8080";
        private string accessToken = "";
        private string httpMethod = "GET";
        private string apiUrl = "/api/sample/hello";
        private string requestBody = "{}";
        private string requestHeaders = "";
        private string responseText = "";
        private int responseCode = 0;
        private double requestTime = 0;
        
        // UI滚动位置
        private Vector2 requestScrollPos;
        private Vector2 responseScrollPos;
        private Vector2 headerScrollPos;
        
        // 历史记录
        private List<ApiTestHistoryItem> testHistory = new List<ApiTestHistoryItem>();
        private int selectedHistoryIndex = -1;
        
        // 预定义的API列表
        private List<string> predefinedApis = new List<string>();
        private int selectedPredefinedApi = 0;
        
        [MenuItem("Tools/Local REST API/API测试工具")]
        public static void ShowWindow()
        {
            window = GetWindow<ApiTestWindow>("API 测试工具");
            window.minSize = new Vector2(800, 700);
        }
        
        private void OnEnable()
        {
            // 从配置加载默认值
            var config = ApiConfig.Load();
            serverUrl = config.serverUrl;
            accessToken = config.accessToken;
            
            // 加载预定义API
            RefreshPredefinedApis();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("API 测试工具", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space();
            
            // 服务器信息
            DrawServerInfo();
            
            EditorGUILayout.Space();
            
            // HTTP方法和API路径
            DrawRequestConfig();
            
            EditorGUILayout.Space();
            
            // 请求头
            DrawRequestHeaders();
            
            EditorGUILayout.Space();
            
            // 请求体
            DrawRequestBody();
            
            EditorGUILayout.Space();
            
            // 测试按钮
            DrawTestButton();
            
            EditorGUILayout.Space();
            
            // 响应结果
            DrawResponse();
            
            EditorGUILayout.Space();
            
            // 历史记录
            DrawHistory();
        }
        
        private void DrawServerInfo()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("服务器信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("服务器地址");
            serverUrl = EditorGUILayout.TextField(serverUrl);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("访问令牌");
            accessToken = EditorGUILayout.TextField(accessToken);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRequestConfig()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("请求配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("HTTP方法");
            httpMethod = EditorGUILayout.TextField(httpMethod);
            EditorGUILayout.EndHorizontal();
            
            // 预定义API选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("预定义API");
            RefreshPredefinedApis(); // 刷新API列表
            if (predefinedApis.Count > 0)
            {
                selectedPredefinedApi = EditorGUILayout.Popup(selectedPredefinedApi, predefinedApis.ToArray());
                if (GUILayout.Button("应用", GUILayout.Width(50)))
                {
                    if (selectedPredefinedApi >= 0 && selectedPredefinedApi < predefinedApis.Count)
                    {
                        var selectedApi = predefinedApis[selectedPredefinedApi];
                        if (!string.IsNullOrEmpty(selectedApi))
                        {
                            var parts = selectedApi.Split(' ');
                            if (parts.Length >= 2)
                            {
                                httpMethod = parts[0];
                                apiUrl = parts[1];
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("无可用API", GUILayout.Width(150));
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("API路径");
            apiUrl = EditorGUILayout.TextField(apiUrl);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRequestHeaders()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("请求头 (每行格式: Key: Value)", EditorStyles.boldLabel);
            
            headerScrollPos = EditorGUILayout.BeginScrollView(headerScrollPos, GUILayout.Height(60));
            requestHeaders = EditorGUILayout.TextArea(requestHeaders, GUILayout.Height(60));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRequestBody()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("请求体", EditorStyles.boldLabel);
            
            if (httpMethod == "GET" || httpMethod == "DELETE")
            {
                EditorGUILayout.HelpBox("GET和DELETE请求通常不包含请求体", MessageType.Info);
            }
            
            requestScrollPos = EditorGUILayout.BeginScrollView(requestScrollPos, GUILayout.Height(120));
            requestBody = EditorGUILayout.TextArea(requestBody, GUILayout.Height(120));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTestButton()
        {
            if (GUILayout.Button("发送请求", GUILayout.Height(40)))
            {
                SendApiRequest();
            }
        }
        
        private void DrawResponse()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("响应结果", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("状态码");
            EditorGUILayout.LabelField(responseCode.ToString(), EditorStyles.textField);
            
            EditorGUILayout.PrefixLabel("耗时");
            EditorGUILayout.LabelField($"{requestTime:F2} ms", EditorStyles.textField);
            EditorGUILayout.EndHorizontal();
            
            responseScrollPos = EditorGUILayout.BeginScrollView(responseScrollPos, GUILayout.Height(150));
            EditorGUILayout.SelectableLabel(responseText, GUILayout.Height(150));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawHistory()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("测试历史", EditorStyles.boldLabel);
            
            if (testHistory.Count > 0)
            {
                int itemsToShow = Mathf.Min(10, testHistory.Count); // 只显示最近10条
                for (int i = 0; i < itemsToShow; i++)
                {
                    var historyItem = testHistory[testHistory.Count - 1 - i]; // 从最新的开始
                    EditorGUILayout.BeginHorizontal();
                    
                    string historyText = $"{historyItem.Method} {historyItem.Url} - {historyItem.ResponseCode} ({historyItem.Timestamp})";
                    if (GUILayout.Button(historyText, EditorStyles.linkLabel))
                    {
                        // 加载历史记录到当前测试参数
                        httpMethod = historyItem.Method;
                        apiUrl = historyItem.Url;
                        requestBody = historyItem.RequestBody;
                        responseText = historyItem.Response;
                        responseCode = historyItem.ResponseCode;
                        requestTime = historyItem.RequestTime;
                        
                        // 更新到当前选择的历史记录
                        selectedHistoryIndex = testHistory.Count - 1 - i;
                    }
                    
                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        testHistory.RemoveAt(testHistory.Count - 1 - i);
                        Repaint();
                        return; // 退出当前绘制，避免索引问题
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("清空历史", GUILayout.Width(80)))
                {
                    testHistory.Clear();
                }
            }
            else
            {
                EditorGUILayout.LabelField("暂无测试历史");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void SendApiRequest()
        {
            // 验证URL
            string fullUrl = serverUrl.TrimEnd('/') + apiUrl;
            
            // 添加访问令牌
            var headers = ParseHeaders(requestHeaders);
            if (!string.IsNullOrEmpty(accessToken))
            {
                headers["Authorization"] = $"Bearer {accessToken}";
            }
            
            // 使用UnityWebRequest发送请求
            EditorCoroutine.Start(SendRequestCoroutine(fullUrl, headers));
        }
        
        private System.Collections.IEnumerator SendRequestCoroutine(string url, Dictionary<string, string> headers)
        {
            UnityWebRequest request;
            DateTime startTime = DateTime.Now;
            if (httpMethod.ToUpper() == "GET")
            {
                request = UnityWebRequest.Get(url);
            }
            else if (httpMethod.ToUpper() == "POST")
            {
                request = new UnityWebRequest(url, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
            }
            else if (httpMethod.ToUpper() == "PUT")
            {
                request = new UnityWebRequest(url, "PUT");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
            }
            else if (httpMethod.ToUpper() == "DELETE")
            {
                request = UnityWebRequest.Delete(url);
            }
            else
            {
                // 对于其他方法，使用通用方法
                request = new UnityWebRequest(url, httpMethod.ToUpper());
                if (!string.IsNullOrEmpty(requestBody))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }
                request.downloadHandler = new DownloadHandlerBuffer();
            }
            
            // 添加自定义请求头
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            // 设置Content-Type为JSON（除非用户已指定）
            if (!headers.ContainsKey("Content-Type") && (httpMethod.ToUpper() == "POST" || httpMethod.ToUpper() == "PUT"))
            {
                request.SetRequestHeader("Content-Type", "application/json");
            }
            
            // 发送请求
            yield return request.SendWebRequest();
            
            // 记录结束时间
            var endTime = DateTime.Now;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            // 处理响应
            if (request.result == UnityWebRequest.Result.Success)
            {
                responseText = request.downloadHandler.text;
                responseCode = (int)request.responseCode;
            }
            else
            {
                responseText = $"错误: {request.error}\n{request.downloadHandler.text}";
                responseCode = (int)request.responseCode;
            }
            
            requestTime = duration;
            
            // 保存到历史记录
            var historyItem = new ApiTestHistoryItem
            {
                Method = httpMethod,
                Url = apiUrl,
                RequestBody = requestBody,
                Response = responseText,
                ResponseCode = responseCode,
                RequestTime = duration,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            };
            testHistory.Add(historyItem);
            
            // 重绘窗口
            Repaint();
        }
        
        private Dictionary<string, string> ParseHeaders(string headerText)
        {
            var headers = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(headerText)) return headers;
            
            var lines = headerText.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;
                
                var separatorIndex = trimmedLine.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var key = trimmedLine.Substring(0, separatorIndex).Trim();
                    var value = trimmedLine.Substring(separatorIndex + 1).Trim();
                    headers[key] = value;
                }
            }
            
            return headers;
        }
        
        private void RefreshPredefinedApis()
        {
            predefinedApis.Clear();
            
            // 添加内置API
            predefinedApis.Add("GET /api/routes");
            predefinedApis.Add("GET /api/sample/hello");
            predefinedApis.Add("POST /api/sample/echo");
            predefinedApis.Add("GET /api/sample/random");
            predefinedApis.Add("GET /api/sample/status");
            predefinedApis.Add("GET /api/unity/scene");
            predefinedApis.Add("GET /api/unity/objects");
            predefinedApis.Add("POST /api/unity/log");
            
            // 从API服务器获取已注册的路由
            if (RestApiMainWindow.IsServiceRunning())
            {
                var apiServer = GetApiServerInstance();
                if (apiServer != null)
                {
                    var routes = apiServer.GetRoutes();
                    foreach (var route in routes)
                    {
                        var parts = route.Key.Split(' ');
                        if (parts.Length >= 2)
                        {
                            string method = parts[0];
                            string path = parts[1];
                            string routeInfo = $"{method} {path}";
                            
                            // 避免重复
                            if (!predefinedApis.Contains(routeInfo))
                            {
                                predefinedApis.Add(routeInfo);
                            }
                        }
                    }
                }
            }
        }
        
        private ApiServer GetApiServerInstance()
        {
            // 通过反射获取RestApiMainWindow中的apiServer实例
            var mainWindowType = typeof(RestApiMainWindow);
            var window = GetWindow<RestApiMainWindow>();
            
            if (window != null)
            {
                var serverField = mainWindowType.GetField("apiServer", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (serverField != null)
                {
                    return serverField.GetValue(window) as ApiServer;
                }
            }
            
            return null;
        }
    }
    
    [Serializable]
    public class ApiTestHistoryItem
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public string RequestBody { get; set; }
        public string Response { get; set; }
        public int ResponseCode { get; set; }
        public double RequestTime { get; set; }
        public string Timestamp { get; set; }
    }
}