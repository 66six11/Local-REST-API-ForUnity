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

        private static void CreateInstance()
        {
            if (_instance != null) return;

            var existing = FindObjectOfType<MainThreadDispatcher>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            var go = new GameObject("MainThreadDispatcher");
            _instance = go.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }

        private void Update()
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
                action();
                return true;
            }

            using (var resetEvent = new ManualResetEvent(false))
            {
                _syncActionQueue.Enqueue((action, resetEvent));
                return resetEvent.WaitOne(timeoutMilliseconds);
            }
        }

        /// <summary>
        /// 检查当前是否在主线程
        /// </summary>
        public static bool IsMainThread()
        {
            return Thread.CurrentThread == _instance?.mainThread;
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
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}