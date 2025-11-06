using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiTroubleshootingHelper
    {
        public static void ShowStartupTroubleshootingGuide()
        {
            RestApiMainWindow.Log("=== REST API 启动故障排除指南 ===");
            RestApiMainWindow.Log("1. 端口占用问题:");
            RestApiMainWindow.Log("   - 确保指定的端口没有被其他应用程序占用");
            RestApiMainWindow.Log("   - 尝试更改端口（例如从8080改为8081）");
            RestApiMainWindow.Log("2. 权限问题:");
            RestApiMainWindow.Log("   - 在某些系统上，可能需要管理员权限来绑定到特定端口");
            RestApiMainWindow.Log("   - 尝试以管理员身份运行Unity编辑器");
            RestApiMainWindow.Log("3. URL保留问题:");
            RestApiMainWindow.Log("   - Windows需要为HTTP服务器保留URL前缀");
            RestApiMainWindow.Log("   - 如果看到错误代码5或183，请检查是否已有其他程序保留了相同的URL");
            RestApiMainWindow.Log("4. 防火墙/安全软件:");
            RestApiMainWindow.Log("   - 检查防火墙设置是否阻止了应用程序");
            RestApiMainWindow.Log("   - 临时禁用防火墙测试（记得之后重新启用）");
            RestApiMainWindow.Log("5. URL格式:");
            RestApiMainWindow.Log("   - 确保URL格式正确，例如: http://localhost:8080/");
            RestApiMainWindow.Log("   - URL必须以/结尾");
            RestApiMainWindow.Log("6. 检查是否有其他实例:");
            RestApiMainWindow.Log("   - 确保没有其他LocalRestAPI实例正在运行");
            RestApiMainWindow.Log("   - 查看任务管理器中是否有其他Unity进程在运行API服务");
            RestApiMainWindow.Log("===============================");
        }
        
        public static void ShowRunningProcessesOnPort(string url)
        {
            try
            {
                var uri = new Uri(url);
                int port = uri.Port;
                
                RestApiMainWindow.Log($"正在查找使用端口 {port} 的进程...");
                
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
                
                var connectionsOnPort = tcpConnections.Where(c => c.LocalEndPoint.Port == port).ToList();
                
                if (connectionsOnPort.Count > 0)
                {
                    RestApiMainWindow.Log($"发现 {connectionsOnPort.Count} 个进程正在使用端口 {port}:");
                    foreach (var conn in connectionsOnPort)
                    {
                        RestApiMainWindow.Log($"  状态: {conn.State}, 本地: {conn.LocalEndPoint}, 远程: {conn.RemoteEndPoint}");
                    }
                }
                else
                {
                    RestApiMainWindow.Log($"没有发现其他进程使用端口 {port}");
                }
                
                // 获取监听指定端口的进程
                IPEndPoint[] listeners = ipGlobalProperties.GetActiveTcpListeners();
                var listenersOnPort = listeners.Where(l => l.Port == port).ToList();
                
                if (listenersOnPort.Count > 0)
                {
                    RestApiMainWindow.Log($"发现 {listenersOnPort.Count} 个监听器在端口 {port}:");
                    foreach (var listener in listenersOnPort)
                    {
                        RestApiMainWindow.Log($"  监听地址: {listener}");
                    }
                }
            }
            catch (Exception ex)
            {
                RestApiMainWindow.Log($"无法获取端口使用信息: {ex.Message}");
            }
        }
    }
}