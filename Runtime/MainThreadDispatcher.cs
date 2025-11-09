using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// Unity 主线程调度器
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();
        private static readonly ConcurrentQueue<(Action, ManualResetEvent)> _syncActionQueue = new ConcurrentQueue<(Action, ManualResetEvent)>();
        
        public static MainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateInstance();
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // 确保在场景加载后创建实例
            CreateInstance();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            // 编辑器模式下注册更新回调
            UnityEditor.EditorApplication.update += EditorUpdate;
            
            // 编辑器退出时清理
            UnityEditor.EditorApplication.quitting += OnEditorQuitting;
            
            // 确保在编辑器模式下也有实例
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (_instance == null && !Application.isPlaying)
                {
                    CreateEditorInstance();
                }
            };
        }

        private static void OnEditorQuitting()
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
            if (_instance != null && !Application.isPlaying)
            {
                DestroyImmediate(_instance.gameObject);
                _instance = null;
            }
        }

        private static void EditorUpdate()
        {
            // 编辑器模式下的更新处理
            if (_instance == null && !Application.isPlaying)
            {
                CreateEditorInstance();
            }
            
            if (_instance != null && !Application.isPlaying)
            {
                ProcessQueues();
            }
        }

        private static void CreateEditorInstance()
        {
            if (_instance != null) return;
            if (Application.isPlaying) return; // 播放模式下使用正常流程

            // 在编辑器模式下创建隐藏的GameObject
            var go = new GameObject("MainThreadDispatcher_Editor")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _instance = go.AddComponent<MainThreadDispatcher>();
            
            // 设置为主线程
            _instance.mainThread = Thread.CurrentThread;
        }
#endif

        private static void CreateInstance()
        {
            if (_instance != null) return;

            // 在播放模式下查找现有实例
            if (Application.isPlaying)
            {
                var existing = FindFirstObjectByType<MainThreadDispatcher>();
                if (existing != null)
                {
                    _instance = existing;
                    return;
                }

                var go = new GameObject("MainThreadDispatcher");
                _instance = go.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
#if UNITY_EDITOR
            else
            {
                // 编辑器非播放模式
                CreateEditorInstance();
            }
#endif
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                ProcessQueues();
            }
        }

        private static void ProcessQueues()
        {
            // 处理异步操作
            while (_actionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"主线程调度异常: {ex}");
                }
            }

            // 处理同步操作
            while (_syncActionQueue.TryDequeue(out var item))
            {
                var (action, resetEvent) = item;
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"主线程同步调度异常: {ex}");
                }
                finally
                {
                    resetEvent?.Set();
                }
            }
        }

        /// <summary>
        /// 异步调用到主线程（不等待完成）
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;

            if (IsMainThread())
            {
                action();
                return;
            }

            _actionQueue.Enqueue(action);
            
            // 确保在编辑器模式下有实例处理队列
            if (_instance == null)
            {
                CreateInstance();
            }
        }

        /// <summary>
        /// 同步调用到主线程（等待完成）
        /// </summary>
        public static void Invoke(Action action)
        {
            if (action == null) return;

            if (IsMainThread())
            {
                action();
                return;
            }

            using (var resetEvent = new ManualResetEvent(false))
            {
                _syncActionQueue.Enqueue((action, resetEvent));
                
                // 确保在编辑器模式下有实例处理队列
                if (_instance == null)
                {
                    CreateInstance();
                }
                
                resetEvent.WaitOne();
            }
        }

        /// <summary>
        /// 同步调用到主线程并返回结果
        /// </summary>
        public static T Invoke<T>(Func<T> func)
        {
            if (func == null) return default;

            if (IsMainThread())
            {
                return func();
            }

            T result = default;
            using (var resetEvent = new ManualResetEvent(false))
            {
                _syncActionQueue.Enqueue((() =>
                {
                    try
                    {
                        result = func();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"主线程函数执行异常: {ex}");
                    }
                }, resetEvent));
                
                // 确保在编辑器模式下有实例处理队列
                if (_instance == null)
                {
                    CreateInstance();
                }
                
                resetEvent.WaitOne();
            }
            return result;
        }

        /// <summary>
        /// 带超时的同步调用
        /// </summary>
        public static bool Invoke(Action action, int timeoutMilliseconds)
        {
            if (action == null) return false;

            if (IsMainThread())
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"主线程函数执行异常: {ex}");
                    return false;
                }
            }

            using (var resetEvent = new ManualResetEvent(false))
            {
                _syncActionQueue.Enqueue((action, resetEvent));
                
                // 确保在编辑器模式下有实例处理队列
                if (_instance == null)
                {
                    CreateInstance();
                }
                
                bool completed = resetEvent.WaitOne(timeoutMilliseconds);
                if (!completed)
                {
                    Debug.LogError($"主线程调用超时 ({timeoutMilliseconds}ms)");
                }
                return completed;
            }
        }

        /// <summary>
        /// 检查当前是否在主线程
        /// </summary>
        public static bool IsMainThread()
        {
            return _instance != null && Thread.CurrentThread == _instance.mainThread;
        }

        private Thread mainThread;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            mainThread = Thread.CurrentThread;
            
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

#if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            // 清理编辑器模式的实例
            if (!Application.isPlaying && _instance == this)
            {
                DestroyImmediate(gameObject);
                _instance = null;
            }
        }
#endif
    }
}