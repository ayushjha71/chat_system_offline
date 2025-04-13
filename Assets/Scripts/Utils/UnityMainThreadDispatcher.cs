using System;
using UnityEngine;
using System.Collections.Generic;

namespace SparkVRTest.Utils
{
    /// <summary>
    /// A thread-safe dispatcher for executing actions on Unity's main thread.
    /// Useful when you need to update Unity objects from a background thread.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        // Singleton instance
        private static UnityMainThreadDispatcher _instance;

        // Thread-safe queue to store actions to execute on the main thread
        private readonly Queue<Action> _executionQueue = new Queue<Action>();

        // Lock object for synchronizing access to the queue
        private readonly object _lockObject = new object();

        /// <summary>
        /// Ensures that only one instance of the dispatcher exists and persists across scenes.
        /// </summary>
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject); // Persist between scenes
            }
            else
            {
                Destroy(gameObject); // Destroy duplicate instance
            }
        }

        /// <summary>
        /// Gets or creates the singleton instance of the dispatcher.
        /// </summary>
        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                // Create a GameObject with this component if not already present in the scene
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }

        /// <summary>
        /// Enqueues an action to run on the main Unity thread.
        /// Can be safely called from other threads.
        /// </summary>
        public static void RunOnMainThread(Action action)
        {
            if (_instance == null)
            {
                Debug.LogError("UnityMainThreadDispatcher not initialized. Ensure it exists in the scene before using it.");
                return;
            }
            _instance.Enqueue(action);
        }

        /// <summary>
        /// Adds an action to the internal queue.
        /// </summary>
        public void Enqueue(Action action)
        {
            lock (_lockObject)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Executes all queued actions on the main thread every frame.
        /// </summary>
        void Update()
        {
            lock (_lockObject)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }
    }
}
