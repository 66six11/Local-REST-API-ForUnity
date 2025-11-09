using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiPerformanceMonitor
    {
        private static ApiPerformanceMonitor instance;
        private ConcurrentBag<ApiCallMetric> recentCalls;
        private int maxMetrics = 1000;
        public static bool IsEnabled => instance != null;

        public static ApiPerformanceMonitor Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ApiPerformanceMonitor();
                }

                return instance;
            }
        }

        private ApiPerformanceMonitor()
        {
            recentCalls = new ConcurrentBag<ApiCallMetric>();
        }

        public class ApiCallMetric
        {
            public string Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string Method { get; set; }
            public string Path { get; set; }
            public int StatusCode { get; set; }
            public double DurationMs { get; set; }
            public string ClientIp { get; set; }
            public bool IsUnregisteredRoute { get; set; } // 标识是否为未注册路由
        }

        public static void RecordApiCall(string method, string path, int statusCode, double duration, string clientIp, bool isUnregisteredRoute)
        {
            Instance.Record(method, path, statusCode, duration, clientIp, isUnregisteredRoute);
        }

        public void Record(string method, string path, int statusCode, double durationMs, string clientIp = "", bool isUnregisteredRoute = false)
        {
            var metric = new ApiCallMetric
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Method = method,
                Path = path,
                StatusCode = statusCode,
                DurationMs = durationMs,
                ClientIp = clientIp,
                IsUnregisteredRoute = isUnregisteredRoute
            };

            // 线程安全添加
            recentCalls.Add(metric);

            // 限制指标数量 - 需要特殊处理，因为ConcurrentBag不支持直接删除
            MaintainSizeLimit();
        }

        // 维护列表大小限制
        private void MaintainSizeLimit()
        {
            // 如果超过限制，创建一个新的集合并保留最新的maxMetrics个元素
            if (recentCalls.Count > maxMetrics)
            {
                var newCalls = new ConcurrentBag<ApiCallMetric>();

                // 获取最新的maxMetrics个元素
                var sortedCalls = recentCalls
                                  .OrderByDescending(c => c.Timestamp)
                                  .Take(maxMetrics)
                                  .ToList();

                // 重新添加到新集合
                foreach (var call in sortedCalls)
                {
                    newCalls.Add(call);
                }

                recentCalls = newCalls;
            }
        }

        // 使用线程安全的快照进行查询
        private List<ApiCallMetric> GetSnapshot()
        {
            return recentCalls.ToList();
        }

        public float GetRequestsPerSecond()
        {
            var snapshot = GetSnapshot();
            var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
            var callsInLastMinute = snapshot.Count(c => c.Timestamp >= oneMinuteAgo);
            return callsInLastMinute / 60.0f;
        }

        public int GetTotalRequests()
        {
            return recentCalls.Count;
        }

        public float GetAverageResponseTime()
        {
            var snapshot = GetSnapshot();
            if (snapshot.Count == 0) return 0;
            return (float)snapshot.Average(c => c.DurationMs);
        }

        public float GetAverageResponseTimeForPath(string path)
        {
            var snapshot = GetSnapshot();
            var pathCalls = snapshot.Where(c => c.Path == path);
            if (!pathCalls.Any()) return 0;
            return (float)pathCalls.Average(c => c.DurationMs);
        }

        public int GetTotalRequestsForPath(string path)
        {
            var snapshot = GetSnapshot();
            return snapshot.Count(c => c.Path == path);
        }

        public Dictionary<string, float> GetResponseTimeByPath()
        {
            var snapshot = GetSnapshot();
            var result = new Dictionary<string, float>();
            // 过滤掉未注册路由
            var registeredCalls = snapshot.Where(c => !c.IsUnregisteredRoute);
            var grouped = registeredCalls.GroupBy(c => c.Path);

            foreach (var group in grouped)
            {
                result[group.Key] = (float)group.Average(c => c.DurationMs);
            }

            return result;
        }

        public Dictionary<string, int> GetRequestCountByPath()
        {
            var snapshot = GetSnapshot();
            var result = new Dictionary<string, int>();
            // 过滤掉未注册路由
            var registeredCalls = snapshot.Where(c => !c.IsUnregisteredRoute);
            var grouped = registeredCalls.GroupBy(c => c.Path);

            foreach (var group in grouped)
            {
                result[group.Key] = group.Count();
            }

            return result;
        }

        public List<ApiCallMetric> GetRecentCalls(int count = 50)
        {
            var snapshot = GetSnapshot();
            return snapshot.OrderByDescending(c => c.Timestamp).Take(count).ToList();
        }

        public List<ApiCallMetric> GetRecentCallsByPath(string path, int count = 50)
        {
            var snapshot = GetSnapshot();
            return snapshot
                   .Where(c => c.Path == path)
                   .OrderByDescending(c => c.Timestamp)
                   .Take(count)
                   .ToList();
        }

        public List<ApiCallMetric> GetAllApiCalls()
        {
            return GetSnapshot();
        }

        public float GetErrorRate()
        {
            var snapshot = GetSnapshot();
            if (snapshot.Count == 0) return 0;
            var errorCalls = snapshot.Count(c => c.StatusCode >= 400);
            return (float)errorCalls / snapshot.Count * 100;
        }

        public void ClearMetrics()
        {
            recentCalls = new ConcurrentBag<ApiCallMetric>();
        }

        // 新增：获取性能统计摘要
        public PerformanceSummary GetPerformanceSummary()
        {
            var snapshot = GetSnapshot();
            var registeredCalls = snapshot.Where(c => !c.IsUnregisteredRoute);

            return new PerformanceSummary
            {
                TotalRequests = snapshot.Count,
                RegisteredRequests = registeredCalls.Count(),
                UnregisteredRequests = snapshot.Count - registeredCalls.Count(),
                AverageResponseTime = snapshot.Count > 0 ? (float)snapshot.Average(c => c.DurationMs) : 0,
                ErrorRate = GetErrorRate(),
                RequestsPerSecond = GetRequestsPerSecond(),
                TopSlowEndpoints = GetTopSlowEndpoints(5),
                RecentErrorCodes = GetRecentErrorCodes(10)
            };
        }

        private Dictionary<string, float> GetTopSlowEndpoints(int topCount)
        {
            var snapshot = GetSnapshot();
            var registeredCalls = snapshot.Where(c => !c.IsUnregisteredRoute);

            return registeredCalls
                   .GroupBy(c => c.Path)
                   .Select(g => new { Path = g.Key, AvgTime = g.Average(c => c.DurationMs) })
                   .OrderByDescending(x => x.AvgTime)
                   .Take(topCount)
                   .ToDictionary(x => x.Path, x => (float)x.AvgTime);
        }

        private Dictionary<int, int> GetRecentErrorCodes(int recentCount)
        {
            var snapshot = GetSnapshot();
            return snapshot
                   .Where(c => c.StatusCode >= 400)
                   .OrderByDescending(c => c.Timestamp)
                   .Take(recentCount)
                   .GroupBy(c => c.StatusCode)
                   .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    // 性能统计摘要类
    public class PerformanceSummary
    {
        public int TotalRequests { get; set; }
        public int RegisteredRequests { get; set; }
        public int UnregisteredRequests { get; set; }
        public float AverageResponseTime { get; set; }
        public float ErrorRate { get; set; }
        public float RequestsPerSecond { get; set; }
        public Dictionary<string, float> TopSlowEndpoints { get; set; }
        public Dictionary<int, int> RecentErrorCodes { get; set; }
    }
}