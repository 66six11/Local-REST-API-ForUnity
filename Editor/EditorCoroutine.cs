using System;
using System.Collections;
using UnityEditor;

namespace LocalRestAPI
{
    public class EditorCoroutine
    {
        private readonly IEnumerator coroutine;
        private bool isRunning = true;
        
        private EditorCoroutine(IEnumerator coroutine)
        {
            this.coroutine = coroutine;
            EditorApplication.update += Update;
        }
        
        public static EditorCoroutine Start(IEnumerator coroutine)
        {
            return new EditorCoroutine(coroutine);
        }
        
        private void Update()
        {
            if (!isRunning) return;
            
            try
            {
                if (!coroutine.MoveNext())
                {
                    Stop();
                }
            }
            catch (Exception ex)
            {
                Stop();
                throw ex;
            }
        }
        
        private void Stop()
        {
            isRunning = false;
            EditorApplication.update -= Update;
        }
    }
}