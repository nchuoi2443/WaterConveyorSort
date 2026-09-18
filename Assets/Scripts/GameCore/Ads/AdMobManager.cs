using System;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace WaterConveyorSort.Ads
{

    public sealed class AdMobManager : MonoBehaviour
    {
        public static AdMobManager Instance { get; private set; }
        private static Task initialization;

        [SerializeField]
        private AdMobRewardedSample rewarded;
        [SerializeField]
        private AdMobInterstitialSample interstitial;

        [Header("Full Ads Timer")]
        [SerializeField] private bool autoShowInterstitials = true;
        [Tooltip("Seconds between full-screen ads. Zero disables automatic interstitials.")]
        [SerializeField, Min(0f)] private float fullAdsRestTime = 60f;
        public float FullAdsRestTime => fullAdsRestTime;
        [Tooltip("Runtime elapsed seconds. Reset when the manager starts or an ad is shown.")]
        [SerializeField] private float fullAdsElapsed;
        private float loadRetryRemaining;
        private bool applicationPaused;
        private bool skipTimerTick;
        private const float LoadRetryInterval = 10f;

        public bool IsShowing => rewarded != null && rewarded.IsShowing || interstitial != null && interstitial.IsShowing;
        public bool IsRewardedReady => !IsShowing && rewarded != null && rewarded.IsReady;
        public bool IsInterstitialReady => !IsShowing && interstitial != null && interstitial.IsReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Instance = null;
            initialization = null;
        }

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
            if (rewarded == null) rewarded = GetComponentInChildren<AdMobRewardedSample>(true);
            if (interstitial == null) interstitial = GetComponentInChildren<AdMobInterstitialSample>(true);
            ResetFullAdsTimer();
            if (interstitial == null)
                Debug.LogWarning("AdMobManager needs an Interstitial component assigned or on a child object to show full ads.", this);
        }

        private async void Start()
        {
            if (Instance != this) return;
            try { await InitializeAsync(); }
            catch (Exception exception)
            {
                if (this != null) Debug.LogException(exception, this);
            }
        }

        private void Update()
        {
            if (Instance != this) return;
            if (!autoShowInterstitials || interstitial == null) return;
            // Editor panel focus should not stop the timer while inspecting it.
            if (!Application.isEditor && (applicationPaused || !Application.isFocused)) return;
            if (skipTimerTick) { skipTimerTick = false; return; }
            if (IsShowing)
            {
                ResetFullAdsTimer();
                return;
            }
            if (FullAdsRestTime <= 0f) return;

            // Unscaled time keeps the interval independent of gameplay timeScale.
            fullAdsElapsed = Mathf.Min(FullAdsRestTime, fullAdsElapsed + Time.unscaledDeltaTime);
            if (fullAdsElapsed < FullAdsRestTime || interstitial == null) return;
            if (TryShowInterstitialAd()) return;

            // Retry loading at a bounded interval while waiting for an available ad.
            loadRetryRemaining -= Time.unscaledDeltaTime;
            if (loadRetryRemaining > 0f) return;
            loadRetryRemaining = LoadRetryInterval;
            LoadInterstitialAd();
        }

        public void ResetFullAdsTimer()
        {
            fullAdsElapsed = 0f;
            loadRetryRemaining = 0f;
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            skipTimerTick = true;
        }

        private void OnApplicationFocus(bool focused) => skipTimerTick = true;

        public Task InitializeAsync()
        {
            if (Instance != this)
                return Task.FromException(new InvalidOperationException("Only the active AdMobManager can initialize ads."));
            if (initialization != null) return initialization;

            var completion = new TaskCompletionSource<bool>();
            initialization = completion.Task;
            try
            {
                MobileAds.Initialize(status => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (status == null)
                    {
                        initialization = null;
                        completion.TrySetException(new InvalidOperationException("AdMob initialization failed."));
                        return;
                    }
                    Debug.Log("AdMob initialized (test sample).");
                    completion.TrySetResult(true);
                }));
            }
            catch (Exception exception)
            {
                initialization = null;
                completion.TrySetException(exception);
            }
            return completion.Task;
        }
        public void LoadRewardedAd() => rewarded.LoadAd();
        public void LoadInterstitialAd() => interstitial.LoadAd();
        public void ShowRewardedAd() => TryShowRewardedAd(null);
        public void ShowInterstitialAd() => TryShowInterstitialAd();

        public bool TryShowRewardedAd(Action onRewardEarned)
        {
            if (!IsRewardedReady) return false;
            return rewarded.TryShowAd(onRewardEarned);
        }

        public bool TryShowInterstitialAd()
        {
            if (!IsInterstitialReady) return false;
            if (!interstitial.TryShowAd()) return false;
            ResetFullAdsTimer();
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
