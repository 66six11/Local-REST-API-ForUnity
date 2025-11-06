using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiPerformanceWindow : EditorWindow
    {
        private static ApiPerformanceWindow window;
        private Vector2 scrollPosition;
        private float refreshInterval = 1.0f;
        private float lastRefreshTime;
        
        [MenuItem("Tools/Local REST API/性能监控")]
        public static void ShowWindow()
        {
            window = GetWindow<ApiPerformanceWindow>("API 性能监控");
            window.minSize = new Vector2(800, 600);
        }
        
        private void OnEnable()
        {
            lastRefreshTime = -refreshInterval; // 立即更新
            EditorApplication.update += UpdateWindow;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= UpdateWindow;
        }
        
        private void UpdateWindow()
        {
            if (Time.realtimeSinceStartup - lastRefreshTime >= refreshInterval)
            {
                Repaint();
                lastRefreshTime = Time.realtimeSinceStartup;
            }
        }
        
        private void OnGUI()
        {
            // 标题
            GUILayout.Label("API 性能监控", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space();
            
            // 总体性能指标
            DrawOverallMetrics();
            
            EditorGUILayout.Space();
            
            // 按路径的性能指标
            DrawPathMetrics();
            
            EditorGUILayout.Space();
            
            // 实时请求图表
            DrawRealtimeChart();
            
            // 刷新设置
            EditorGUILayout.Space();
            refreshInterval = EditorGUILayout.Slider("刷新间隔 (秒)", refreshInterval, 0.1f, 5.0f);
        }
        
        private void DrawOverallMetrics()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("总体指标", EditorStyles.boldLabel);
            
            var monitor = ApiPerformanceMonitor.Instance;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("请求速率");
            EditorGUILayout.LabelField($"{monitor.GetRequestsPerSecond():F2} req/s");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("总请求数");
            EditorGUILayout.LabelField(monitor.GetTotalRequests().ToString());
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("平均响应时间");
            EditorGUILayout.LabelField($"{monitor.GetAverageResponseTime():F2} ms");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("错误率");
            EditorGUILayout.LabelField($"{monitor.GetErrorRate():F2}%");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPathMetrics()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("各API端点指标", EditorStyles.boldLabel);
            
            var monitor = ApiPerformanceMonitor.Instance;
            var requestCounts = monitor.GetRequestCountByPath();
            var responseTimes = monitor.GetResponseTimeByPath();
            
            // 获取未注册路由的请求数
            var allCalls = monitor.GetAllApiCalls();
            var unregisteredCalls = allCalls.Where(c => c.IsUnregisteredRoute).ToList();
            int unregisteredCount = unregisteredCalls.Count;
            float unregisteredAvgTime = unregisteredCalls.Any() ? (float)unregisteredCalls.Average(c => c.DurationMs) : 0;
            
            // 按请求数排序
            var sortedPaths = requestCounts
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            if (sortedPaths.Count == 0 && unregisteredCount == 0)
            {
                EditorGUILayout.LabelField("暂无性能数据");
            }
            else
            {
                // 显示未注册路由统计
                if (unregisteredCount > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // 路径和方法
                    var unregisteredStyle = new GUIStyle(EditorStyles.label);
                    unregisteredStyle.normal.textColor = Color.yellow;
                    EditorGUILayout.LabelField("[未注册路由]", unregisteredStyle, GUILayout.Width(200));
                    
                    // 请求数
                    EditorGUILayout.LabelField($"请求: {unregisteredCount}", GUILayout.Width(100));
                    
                    // 平均响应时间
                    EditorGUILayout.LabelField($"平均: {unregisteredAvgTime:F1}ms", GUILayout.Width(100));
                    
                    // 绘制简单的响应时间条形图
                    var barWidth = Mathf.Clamp((float)(unregisteredAvgTime / 1000.0), 0, 1); // 限制在1秒内
                    EditorGUI.ProgressBar(
                        EditorGUILayout.GetControlRect(GUILayout.Width(100)), 
                        barWidth, 
                        $"{unregisteredAvgTime:F1}ms"
                    );
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                // 显示已注册路由统计
                foreach (var path in sortedPaths)
                {
                    var count = requestCounts.ContainsKey(path) ? requestCounts[path] : 0;
                    var avgTime = responseTimes.ContainsKey(path) ? responseTimes[path] : 0;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // 路径和方法
                    EditorGUILayout.LabelField(path, GUILayout.Width(200));
                    
                    // 请求数
                    EditorGUILayout.LabelField($"请求: {count}", GUILayout.Width(100));
                    
                    // 平均响应时间
                    EditorGUILayout.LabelField($"平均: {avgTime:F1}ms", GUILayout.Width(100));
                    
                    // 绘制简单的响应时间条形图
                    var barWidth = Mathf.Clamp((float)(avgTime / 1000.0), 0, 1); // 限制在1秒内
                    EditorGUI.ProgressBar(
                        EditorGUILayout.GetControlRect(GUILayout.Width(100)), 
                        barWidth, 
                        $"{avgTime:F1}ms"
                    );
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRealtimeChart()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("实时性能图表", EditorStyles.boldLabel);
            
            // 简单的文本图表显示最近的请求
            var monitor = ApiPerformanceMonitor.Instance;
            var recentCalls = monitor.GetRecentCalls(20); // 获取最近20个请求
            
            if (recentCalls.Count > 0)
            {
                // 计算最近的请求分布
                var now = DateTime.Now;
                var oneMinuteAgo = now.AddMinutes(-1);
                var callsPerSecond = new Dictionary<int, int>();
                
                foreach (var call in recentCalls)
                {
                    var second = (int)(call.Timestamp - oneMinuteAgo).TotalSeconds;
                    if (callsPerSecond.ContainsKey(second))
                        callsPerSecond[second]++;
                    else
                        callsPerSecond[second] = 1;
                }
                
                // 绘制简单的文本图表
                EditorGUILayout.BeginHorizontal();
                
                var maxCalls = callsPerSecond.Values.Any() ? callsPerSecond.Values.Max() : 1;
                var totalSeconds = (int)(now - oneMinuteAgo).TotalSeconds;
                
                for (int i = 0; i < totalSeconds; i++)
                {
                    var calls = callsPerSecond.ContainsKey(i) ? callsPerSecond[i] : 0;
                    var height = Mathf.Clamp((float)calls / maxCalls, 0, 1);
                    
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(5), GUILayout.Height(50));
                    
                    // 绘制简单的柱状图
                    EditorGUI.DrawRect(
                        new Rect(rect.x, rect.y + rect.height * (1 - height), rect.width, rect.height * height), 
                        Color.blue
                    );
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"最近60秒内的请求分布 (最高: {maxCalls} req/s)");
            }
            else
            {
                EditorGUILayout.LabelField("暂无性能数据");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void RefreshData()
        {
            // 数据会自动在UpdateWindow中刷新
        }
    }
}