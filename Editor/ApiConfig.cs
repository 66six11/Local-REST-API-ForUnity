using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    [Serializable]
    public class ApiConfig
    {
        public string serverUrl = "http://localhost:8000";
        public string accessToken = "";
        
        private static string configPath => Path.Combine(Application.persistentDataPath, "LocalRestAPI", "config.json");
        
        public static ApiConfig Load()
        {
            // 尝试从项目特定位置加载配置
            string projectConfigPath = GetProjectConfigPath();
            
            if (File.Exists(projectConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(projectConfigPath);
                    var config = JsonUtility.FromJson<ApiConfig>(json);
                    
                    // 如果访问令牌为空，则生成一个新的
                    if (string.IsNullOrEmpty(config.accessToken))
                    {
                        config.accessToken = Guid.NewGuid().ToString("N");
                        config.Save();
                    }
                    
                    return config;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载API配置失败: {ex.Message}");
                    // 如果加载失败，返回默认配置
                    return new ApiConfig { accessToken = Guid.NewGuid().ToString("N") };
                }
            }
            else
            {
                // 如果配置文件不存在，创建默认配置
                var defaultConfig = new ApiConfig { accessToken = Guid.NewGuid().ToString("N") };
                defaultConfig.Save();
                return defaultConfig;
            }
        }
        
        public void Save()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(GetProjectConfigPath());
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(GetProjectConfigPath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存API配置失败: {ex.Message}");
            }
        }
        
        private static string GetProjectConfigPath()
        {
            // 使用Unity项目的Asset同级目录，这样配置会随着项目保存
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectPath, ".localrestapi", "config.json");
        }
        
        public static void ResetConfig()
        {
            string projectConfigPath = GetProjectConfigPath();
            if (File.Exists(projectConfigPath))
            {
                File.Delete(projectConfigPath);
            }
        }
    }
}