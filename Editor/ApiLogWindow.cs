using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiLogWindow : EditorWindow
    {
        private static ApiLogWindow window;
        private Vector2 scrollPosition;
        private List<ApiLogEntry> filteredLogs;
        private string filterText = "";
        private string selectedType = "All";
        private bool autoScroll = true;
        
        [MenuItem("Tools/Local REST API/日志查看器")]
        public static void ShowWindow()
        {
            window = GetWindow<ApiLogWindow>("API 日志");
            window.minSize = new Vector2(800, 600);
        }
        
        private void OnEnable()
        {
            RefreshLogs();
            EditorApplication.update += UpdateWindow;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= UpdateWindow;
        }
        
        private void UpdateWindow()
        {
            // 检查是否有新的日志条目
            if (filteredLogs.Count != ApiLogger.Instance.GetLogEntries().Count)
            {
                RefreshLogs();
            }
        }
        
        private void OnGUI()
        {
            // 标题
            GUILayout.Label("API 日志查看器", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space();
            
            // 过滤器
            DrawFilters();
            
            // 日志列表
            DrawLogList();
            
            // 控制按钮
            DrawControlButtons();
        }
        
        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();
            
            filterText = EditorGUILayout.TextField("搜索", filterText, GUILayout.ExpandWidth(true));
            
            string[] types = { "All", "Request", "Response", "Error" };
            int selectedIndex = Array.IndexOf(types, selectedType);
            selectedIndex = EditorGUILayout.Popup(selectedIndex, types, GUILayout.Width(100));
            selectedType = types[selectedIndex];
            
            autoScroll = EditorGUILayout.Toggle("自动滚动", autoScroll, GUILayout.Width(100));
            
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshLogs();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawLogList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            
            if (filteredLogs != null)
            {
                foreach (var log in filteredLogs)
                {
                    DrawLogEntry(log);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawLogEntry(ApiLogEntry log)
        {
            var originalColor = GUI.contentColor;
            
            // 根据日志类型设置颜色
            if (log.Type == "Error")
            {
                GUI.contentColor = Color.red;
            }
            else if (log.Type == "Response" && log.StatusCode >= 400)
            {
                GUI.contentColor = Color.yellow;
            }
            else if (log.Type == "Response" && log.StatusCode >= 200 && log.StatusCode < 300)
            {
                GUI.contentColor = Color.green;
            }
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // 基本信息行
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"[{log.Timestamp:HH:mm:ss.fff}]", GUILayout.Width(100));
            GUILayout.Label(log.Type, GUILayout.Width(80));
            
            if (!string.IsNullOrEmpty(log.Method))
            {
                GUILayout.Label(log.Method, GUILayout.Width(60));
            }
            
            if (!string.IsNullOrEmpty(log.Url))
            {
                GUILayout.Label(log.Url, GUILayout.ExpandWidth(true));
            }
            
            if (log.StatusCode != 0)
            {
                GUILayout.Label(log.StatusCode.ToString(), GUILayout.Width(40));
            }
            
            if (log.DurationMs > 0)
            {
                GUILayout.Label($"{log.DurationMs:F1}ms", GUILayout.Width(70));
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 详细信息
            if (!string.IsNullOrEmpty(log.Message))
            {
                EditorGUILayout.LabelField("Message: " + log.Message);
            }
            
            if (!string.IsNullOrEmpty(log.Exception))
            {
                EditorGUILayout.LabelField("Exception: " + log.Exception);
            }
            
            if (!string.IsNullOrEmpty(log.Body))
            {
                EditorGUILayout.LabelField("Body: " + log.Body);
            }
            
            EditorGUILayout.EndVertical();
            
            GUI.contentColor = originalColor;
            
            // 自动滚动到底部
            if (autoScroll && Event.current.type == EventType.Repaint)
            {
                scrollPosition.y = float.MaxValue;
            }
        }
        
        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("清空日志"))
            {
                ApiLogger.Instance.ClearLogs();
                RefreshLogs();
            }
            
            if (GUILayout.Button("导出日志"))
            {
                string path = EditorUtility.SaveFilePanel("导出日志", "", "api-logs.csv", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    ApiLogger.Instance.ExportLogs(path);
                }
            }
            
            if (GUILayout.Button("复制选中"))
            {
                CopySelectedLogs();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void RefreshLogs()
        {
            var allLogs = ApiLogger.Instance.GetLogEntries();
            
            // 应用过滤器
            filteredLogs = allLogs
                .Where(log => 
                    (selectedType == "All" || log.Type == selectedType) &&
                    (string.IsNullOrEmpty(filterText) || 
                     log.Url?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true ||
                     log.Message?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true ||
                     log.Body?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true)
                )
                .ToList();
            
            Repaint();
        }
        
        private void CopySelectedLogs()
        {
            // 简单实现：复制所有显示的日志
            if (filteredLogs == null) return;
            
            var sb = new System.Text.StringBuilder();
            foreach (var log in filteredLogs)
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Type} - {log.Method} {log.Url} - {log.StatusCode}");
                if (!string.IsNullOrEmpty(log.Message))
                    sb.AppendLine($"Message: {log.Message}");
                if (!string.IsNullOrEmpty(log.Body))
                    sb.AppendLine($"Body: {log.Body}");
                sb.AppendLine("---");
            }
            
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            RestApiMainWindow.Log("日志已复制到剪贴板");
        }
    }
}