using System;
using System.Net;
using System.Net.NetworkInformation;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class PortChecker
    {
        public static bool IsPortAvailable(int port)
        {
            try
            {
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] listeners = ipGlobalProperties.GetActiveTcpListeners();
                
                // 检查端口是否被监听
                foreach (IPEndPoint endPoint in listeners)
                {
                    if (endPoint.Port == port)
                    {
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"检查端口可用性时出错: {ex.Message}");
                // 如果无法检查，返回true以避免阻止服务器启动
                return true;
            }
        }
        
        public static int FindAvailablePort(int startingPort = 8080)
        {
            int port = startingPort;
            int maxAttempts = 100; // 最多尝试100个端口
            int attempts = 0;
            
            while (attempts < maxAttempts)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
                
                port++;
                attempts++;
            }
            
            // 如果没找到可用端口，返回原始端口
            return startingPort;
        }
        
        public static void TestPortAvailability(string url)
        {
            try
            {
                var uri = new Uri(url);
                int port = uri.Port;
                
                if (!IsPortAvailable(port))
                {
                    RestApiMainWindow.Log($"警告: 端口 {port} 似乎已被占用。这可能是API启动失败的原因。");
                }
                else
                {
                    RestApiMainWindow.Log($"信息: 端口 {port} 当前可用。");
                }
            }
            catch (Exception ex)
            {
                RestApiMainWindow.Log($"无法解析URL以检查端口: {ex.Message}");
            }
        }
    }
}