using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiTestUtility
    {
        [MenuItem("Tools/Local REST API/测试API连接")]
        public static void TestApiConnection()
        {
            // 打开新的API测试窗口
            ApiTestWindow.ShowWindow();
        }
        
        [MenuItem("Tools/Local REST API/获取API路由")]
        public static void GetApiRoutes()
        {
            // 打开新的API测试窗口
            ApiTestWindow.ShowWindow();
        }
    }
}