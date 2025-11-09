using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

namespace LocalRestAPI.Runtime
{
    using System;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;

    public static class PortChecker
    {
        /// <summary>
        /// 验证端口号是否合法且当前未被占用
        /// </summary>
        /// <param name="port">要检查的端口号</param>
        /// <exception cref="ArgumentOutOfRangeException">端口号不在有效范围内</exception>
        /// <exception cref="InvalidOperationException">端口已被占用</exception>
        /// <exception cref="System.Net.Sockets.SocketException">网络操作失败</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">系统API调用失败</exception>
        public static void ValidatePort(int port)
        {
            // 1. 检查端口号是否在合法范围内
            if (port <= 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"端口号 {port} 不在有效范围内 (1-65535)");
            }

            // 2. 检查端口是否已被占用
            CheckPortOccupancy(port);
        }

        /// <summary>
        /// 检查指定端口是否已被占用，如果被占用则抛出异常
        /// </summary>
        private static void CheckPortOccupancy(int port)
        {
            try
            {
                // 方法1: 查询系统网络信息（推荐）
                if (IsPortOccupiedBySystemInfo(port))
                {
                    throw new InvalidOperationException($"端口 {port} 已被其他进程占用");
                }
            }
            catch (PlatformNotSupportedException)
            {
                // 如果平台不支持系统信息查询，回退到绑定测试法
                if (IsPortOccupiedByBindingTest(port))
                {
                    throw new InvalidOperationException($"端口 {port} 已被其他进程占用");
                }
            }
        }

        /// <summary>
        /// 通过系统网络信息查询端口占用情况
        /// </summary>
        private static bool IsPortOccupiedBySystemInfo(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            // 检查TCP端口
            IPEndPoint[] tcpListeners = ipProperties.GetActiveTcpListeners();
            foreach (IPEndPoint endPoint in tcpListeners)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }

            // 检查UDP端口
            IPEndPoint[] udpListeners = ipProperties.GetActiveUdpListeners();
            foreach (IPEndPoint endPoint in udpListeners)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 通过尝试绑定端口来测试是否被占用
        /// </summary>
        private static bool IsPortOccupiedByBindingTest(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                return false; // 绑定成功，端口可用
            }
            catch (SocketException)
            {
                return true; // 绑定失败，端口被占用
            }
            finally
            {
                listener?.Stop();
            }
        }

        /// <summary>
        /// 查找可用的端口号（在指定范围内）
        /// </summary>
        /// <param name="startPort">起始端口号</param>
        /// <param name="endPort">结束端口号</param>
        /// <returns>可用的端口号</returns>
        /// <exception cref="InvalidOperationException">在指定范围内找不到可用端口</exception>
        public static int FindAvailablePort(int startPort = 8000, int endPort = 9000)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                try
                {
                    ValidatePort(port);
                    return port;
                }
                catch (InvalidOperationException)
                {
                    // 端口被占用，继续尝试下一个
                    continue;
                }
            }

            throw new InvalidOperationException($"在范围 {startPort}-{endPort} 内找不到可用端口");
        }
    }
}