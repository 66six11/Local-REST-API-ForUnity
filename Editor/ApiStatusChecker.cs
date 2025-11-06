using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiStatusChecker
    {
        [MenuItem("Tools/Local REST API/检查API状态", priority = 100)]
        public static void CheckApiStatus()
        {
            bool isRunning = RestApiMainWindow.IsServiceRunning();
            string token = RestApiMainWindow.GetAccessToken();
            
            string message = $"API服务状态检查:\n\n" +
                           $"服务运行: {isRunning}\n" +
                           $"访问令牌: {(string.IsNullOrEmpty(token) ? "未设置" : token)}\n" +
                           $"主窗口实例: {(RestApiMainWindow.HasWindowInstance() ? "存在" : "不存在")}\n" +
                           $"API服务器实例: {(RestApiMainWindow.HasApiServerInstance() ? "存在" : "不存在")}";
            
            EditorUtility.DisplayDialog("API状态", message, "确定");
        }
        
        [MenuItem("Tools/Local REST API/API测试工具")]
        public static void OpenApiTestWindow()
        {
            ApiTestWindow.ShowWindow();
        }
    }
}