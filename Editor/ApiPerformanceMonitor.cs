using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiPerformanceMonitor
    {
        private static ApiPerformanceMonitor instance;
        private List<ApiCallMetric> recentCalls;
        private int maxMetrics = 1000;
        
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
            recentCalls = new List<ApiCallMetric>();
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
        
        public void RecordApiCall(string method, string path, int statusCode, double durationMs, string clientIp = "", bool isUnregisteredRoute = false)
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
                IsUnregisteredRoute = isUnregisteredRoute // 标识是否为未注册路由
            };
            
            recentCalls.Add(metric);
            
            // 限制指标数量
            if (recentCalls.Count > maxMetrics)
            {
                recentCalls.RemoveAt(0);
            }
        }
        
        public float GetRequestsPerSecond()
        {
            var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
            var callsInLastMinute = recentCalls.Count(c => c.Timestamp >= oneMinuteAgo);
            return callsInLastMinute / 60.0f;
        }
        
        public int GetTotalRequests()
        {
            return recentCalls.Count;
        }
        
        public float GetAverageResponseTime()
        {
            if (recentCalls.Count == 0) return 0;
            return (float)recentCalls.Average(c => c.DurationMs);
        }
        
        public float GetAverageResponseTimeForPath(string path)
        {
            var pathCalls = recentCalls.Where(c => c.Path == path);
            if (!pathCalls.Any()) return 0;
            return (float)pathCalls.Average(c => c.DurationMs);
        }
        
        public int GetTotalRequestsForPath(string path)
        {
            return recentCalls.Count(c => c.Path == path);
        }
        
        public Dictionary<string, float> GetResponseTimeByPath()
        {
            var result = new Dictionary<string, float>();
            // 过滤掉未注册路由
            var registeredCalls = recentCalls.Where(c => !c.IsUnregisteredRoute);
            var grouped = registeredCalls.GroupBy(c => c.Path);
            
            foreach (var group in grouped)
            {
                result[group.Key] = (float)group.Average(c => c.DurationMs);
            }
            
            return result;
        }
        
        public Dictionary<string, int> GetRequestCountByPath()
        {
            var result = new Dictionary<string, int>();
            // 过滤掉未注册路由
            var registeredCalls = recentCalls.Where(c => !c.IsUnregisteredRoute);
            var grouped = registeredCalls.GroupBy(c => c.Path);
            
            foreach (var group in grouped)
            {
                result[group.Key] = group.Count();
            }
            
            return result;
        }
        
        public List<ApiCallMetric> GetRecentCalls(int count = 50)
        {
            return recentCalls.OrderByDescending(c => c.Timestamp).Take(count).ToList();
        }
        
        public List<ApiCallMetric> GetRecentCallsByPath(string path, int count = 50)
        {
            return recentCalls
                .Where(c => c.Path == path)
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToList();
        }
        
        public List<ApiCallMetric> GetAllApiCalls()
        {
            return new List<ApiCallMetric>(recentCalls);
        }
        
        public float GetErrorRate()
        {
            if (recentCalls.Count == 0) return 0;
            var errorCalls = recentCalls.Count(c => c.StatusCode >= 400);
            return (float)errorCalls / recentCalls.Count * 100;
        }
        
        public void ClearMetrics()
        {
            recentCalls.Clear();
        }
    }
}