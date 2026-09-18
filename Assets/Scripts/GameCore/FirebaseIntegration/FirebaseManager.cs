using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using UnityEngine;

namespace WaterConveyorSort.FirebaseIntegration
{
    [DisallowMultipleComponent]
    public sealed class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }
        [SerializeField] private bool sendTestEventOnStart = true;
        public bool IsReady { get; private set; }
        private Task initialization;
        private FirebaseApp app;
        private bool destroyed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (Instance != this) return;
            try
            {
                await InitializeAsync();
                if (!destroyed && sendTestEventOnStart) LogTestEvent();
            }
            catch (Exception exception)
            {
                if (!destroyed) Debug.LogException(exception, this);
            }
        }

        public Task InitializeAsync()
        {
            if (destroyed || Instance != this)
                return Task.FromException(new InvalidOperationException("Only the active FirebaseManager can initialize Firebase."));
            if (initialization == null || initialization.IsFaulted || initialization.IsCanceled)
                initialization = InitializeCoreAsync();
            return initialization;
        }

        private async Task InitializeCoreAsync()
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (destroyed) throw new OperationCanceledException("FirebaseManager was destroyed during initialization.");
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException($"Firebase dependencies unavailable: {status}");
            app = FirebaseApp.DefaultInstance;
            IsReady = true;
            Debug.Log("Firebase initialized. Analytics is ready.", this);
        }

        public bool LogEvent(string eventName, params Parameter[] parameters)
        {
            if (!IsReady || destroyed)
            {
                Debug.LogWarning("Firebase is not ready. Await InitializeAsync before logging events.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("Analytics event name cannot be empty.", nameof(eventName));
            FirebaseAnalytics.LogEvent(eventName, parameters ?? Array.Empty<Parameter>());
            Debug.Log($"Analytics event submitted locally: {eventName}. Verify delivery in DebugView.", this);
            return true;
        }

        [ContextMenu("Send Analytics Test Event")]
        public void LogTestEvent()
        {
            LogEvent("prototype_test", new Parameter("source", "firebase_manager"));
        }

        private void OnDestroy()
        {
            destroyed = true;
            IsReady = false;
            if (Instance == this) Instance = null;
            // DefaultInstance is shared by Firebase products; this component does not dispose it.
            app = null;
        }
    }
}
