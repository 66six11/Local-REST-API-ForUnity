using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;


namespace LocalRestAPI
{
    // 示例控制器
    public class SampleController
    {
        [GetRoute("/api/sample/hello")]
        public HelloResponse Hello(string name = "World")
        {
            return new HelloResponse
            {
                message = $"Hello, {name}!",
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        [PostRoute("/api/sample/echo")]
        public EchoResponse Echo(string message)
        {
            return new EchoResponse
            {
                originalMessage = message,
                echoMessage = $"Echo: {message}",
                length = message?.Length ?? 0
            };
        }

        [GetRoute("/api/sample/random")]
        public RandomResponse GetRandom(int min = 0, int max = 100)
        {
            var random = new System.Random();
            return new RandomResponse
            {
                value = random.Next(min, max),
                min = min,
                max = max
            };
        }

        [GetRoute("/api/sample/status")]
        public StatusResponse GetStatus()
        {
            return new StatusResponse
            {
                uptime = (int)EditorApplication.timeSinceStartup,
                isPlaying = EditorApplication.isPlaying,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating
            };
        }
    }

    // Unity相关的控制器

    public class UnityController

    {
        [GetRoute("/api/unity/scene", true)]
        public SceneInfo GetActiveScene()


        {
            // 在编辑器模式下安全地获取场景信息

            UnityEngine.SceneManagement.Scene scene;

            if (UnityEditor.EditorApplication.isPlaying)

            {
                // 如果在播放模式下，可以安全使用SceneManager

                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            }

            else

            {
                // 在编辑器模式下，使用EditorSceneManager

                scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            }

            return new SceneInfo

            {
                name = scene.name,

                path = scene.path,

                isLoaded = scene.isLoaded,

                rootCount = scene.rootCount
            };
        }


        [GetRoute("/api/unity/objects", true)]
        public ObjectList GetObjectsInScene()

        {
            // 在编辑器模式下安全地获取对象信息

            var objects = new List<ObjectInfo>();


            if (EditorApplication.isPlaying)

            {
                // 如果在播放模式下，使用SceneManager

                var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();


                foreach (var obj in rootObjects)

                {
                    objects.Add(new ObjectInfo

                    {
                        name = obj.name,

                        type = obj.GetType().Name,

                        isActive = obj.activeSelf
                    });
                }
            }

            else

            {
                // 在编辑器模式下，使用EditorSceneManager

                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

                var rootObjects = scene.GetRootGameObjects();


                foreach (var obj in rootObjects)

                {
                    objects.Add(new ObjectInfo

                    {
                        name = obj.name,

                        type = obj.GetType().Name,

                        isActive = obj.activeSelf
                    });
                }
            }


            return new ObjectList

            {
                objects = objects.ToArray(),

                count = objects.Count
            };
        }


        [PostRoute("/api/unity/log", true)]
        public LogResponse LogMessage(string message, string type = "info")

        {
            switch (type.ToLower())

            {
                case "warning":

                    Debug.LogWarning(message);

                    break;

                case "error":

                    Debug.LogError(message);

                    break;

                default:

                    Debug.Log(message);

                    break;
            }


            return new LogResponse

            {
                message = $"Logged message: {message}",

                type = type
            };
        }
    }

    // 响应数据模型
    public class HelloResponse
    {
        public string message;
        public string timestamp;
    }

    public class EchoResponse
    {
        public string originalMessage;
        public string echoMessage;
        public int length;
    }

    public class RandomResponse
    {
        public int value;
        public int min;
        public int max;
    }

    public class StatusResponse
    {
        public int uptime;
        public bool isPlaying;
        public bool isCompiling;
        public bool isUpdating;
    }

    public class SceneInfo
    {
        public string name;
        public string path;
        public bool isLoaded;
        public int rootCount;
    }

    public class ObjectInfo
    {
        public string name;
        public string type;
        public bool isActive;
    }

    public class ObjectList
    {
        public ObjectInfo[] objects;
        public int count;
    }

    public class LogResponse
    {
        public string message;
        public string type;
    }
}

// 响应数据模型
public class HelloResponse
{
    public string message;
    public string timestamp;
}

public class EchoResponse
{
    public string originalMessage;
    public string echoMessage;
    public int length;
}

public class RandomResponse
{
    public int value;
    public int min;
    public int max;
}

public class StatusResponse
{
    public int uptime;
    public bool isPlaying;
    public bool isCompiling;
    public bool isUpdating;
}

public class SceneInfo
{
    public string name;
    public string path;
    public bool isLoaded;
    public int rootCount;
}

public class ObjectInfo
{
    public string name;
    public string type;
    public bool isActive;
}

public class ObjectList
{
    public ObjectInfo[] objects;
    public int count;
}

public class LogResponse
{
    public string message;
    public string type;
}