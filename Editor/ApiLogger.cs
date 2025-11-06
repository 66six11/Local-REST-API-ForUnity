using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiLogger
    {
        private static ApiLogger instance;
        private List<ApiLogEntry> logEntries;
        private int maxLogEntries = 1000;
        
        public static ApiLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ApiLogger();
                }
                return instance;
            }
        }
        
        private ApiLogger()
        {
            logEntries = new List<ApiLogEntry>();
        }
        
        public void LogRequest(string method, string url, string clientIp, Dictionary<string, string> headers, string body)
        {
            var logEntry = new ApiLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Type = "Request",
                Method = method,
                Url = url,
                ClientIp = clientIp,
                Headers = headers,
                Body = body
            };
            
            AddLogEntry(logEntry);
        }
        
        public void LogResponse(string requestId, int statusCode, Dictionary<string, string> headers, string body, double durationMs)
        {
            var logEntry = new ApiLogEntry
            {
                Id = requestId,
                Timestamp = DateTime.Now,
                Type = "Response",
                StatusCode = statusCode,
                Headers = headers,
                Body = body,
                DurationMs = durationMs
            };
            
            AddLogEntry(logEntry);
        }
        
        public void LogError(string message, Exception ex)
        {
            var logEntry = new ApiLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Type = "Error",
                Message = message,
                Exception = ex?.ToString()
            };
            
            AddLogEntry(logEntry);
        }
        
        private void AddLogEntry(ApiLogEntry entry)
        {
            logEntries.Add(entry);
            
            // 限制日志条目数量
            if (logEntries.Count > maxLogEntries)
            {
                logEntries.RemoveAt(0);
            }
            
            // 同时记录到Unity控制台
            if (entry.Type == "Error")
            {
                Debug.LogError($"[API] {entry.Message}\n{entry.Exception}");
            }
            else
            {
                RestApiMainWindow.Log($"[API] {entry.Type}: {entry.Method} {entry.Url}");
            }
        }
        
        public List<ApiLogEntry> GetLogEntries()
        {
            return new List<ApiLogEntry>(logEntries);
        }
        
        public void ClearLogs()
        {
            logEntries.Clear();
        }
        
        public void ExportLogs(string filePath)
        {
            try
            {
                var lines = new List<string>();
                lines.Add("Id,Timestamp,Type,Method,Url,StatusCode,ClientIp,DurationMs,Message");
                
                foreach (var entry in logEntries)
                {
                    lines.Add($"{entry.Id},{entry.Timestamp},{entry.Type},{entry.Method},{entry.Url},{entry.StatusCode},{entry.ClientIp},{entry.DurationMs},{entry.Message}");
                }
                
                File.WriteAllLines(filePath, lines);
                RestApiMainWindow.Log($"日志已导出到: {filePath}");
            }
            catch (Exception ex)
            {
                RestApiMainWindow.Log($"导出日志失败: {ex.Message}");
            }
        }
    }
    
    public class ApiLogEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } // Request, Response, Error
        public string Method { get; set; }
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public string ClientIp { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
        public double DurationMs { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}