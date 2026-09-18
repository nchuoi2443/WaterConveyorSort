using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;
using UnityEngine.Events;

namespace WaterConveyorSort.Ads
{
    public sealed class AdMobRewardedSample : MonoBehaviour
    {
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool reloadAfterClose = true;
        [SerializeField] private UnityEvent adClosed = new UnityEvent();
        [SerializeField] private UnityEvent rewardEarned = new UnityEvent();
        private RewardedAd ad;
        private bool loading, showing, destroyed;
        private float loadedAt;
        public bool IsShowing => showing;
        public bool IsReady => !destroyed && !showing && ad != null &&
            Time.realtimeSinceStartup - loadedAt < 3600f && ad.CanShowAd();

#if UNITY_IOS
        private const string TestAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
        private const string TestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
#endif

        private void Start()
        {
            if (loadOnStart) LoadAd();
        }

        public async void LoadAd()
        {
            if (destroyed || loading || showing || IsReady) return;
            loading = true;
            try
            {
                AdMobManager manager = AdMobManager.Instance;
                if (manager == null)
                    throw new InvalidOperationException("Add an AdMobManager to the Ads parent object before loading ads.");
                await manager.InitializeAsync();
                if (destroyed) return;
                DestroyAd();
                RewardedAd.Load(TestAdUnitId, new AdRequest(), (loaded, error) =>
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        loading = false;
                        if (destroyed) { loaded?.Destroy(); return; }
                        if (error != null || loaded == null)
                        {
                            loaded?.Destroy();
                            Debug.LogWarning($"Rewarded load failed: {error}. Press Load to retry.", this);
                            return;
                        }
                        ad = loaded;
                        loadedAt = Time.realtimeSinceStartup;
                        loaded.OnAdFullScreenContentClosed += () =>
                            MobileAdsEventExecutor.ExecuteInUpdate(() => FinishShowing(loaded, null));
                        loaded.OnAdFullScreenContentFailed += failure =>
                            MobileAdsEventExecutor.ExecuteInUpdate(() => FinishShowing(loaded, failure));
                        Debug.Log("Rewarded test ad ready.", this);
                    }));
            }
            catch (Exception exception)
            {
                loading = false;
                if (!destroyed) Debug.LogException(exception, this);
            }
        }

        public void ShowAd() => TryShowAd();

        public bool TryShowAd(Action onRewardEarned = null)
        {
            if (!IsReady || (AdMobManager.Instance != null && AdMobManager.Instance.IsShowing))
            {
                Debug.LogWarning("Rewarded not ready, or another ad is showing. Load first and wait for the ready log.", this);
                return false;
            }
            showing = true;
            RewardedAd current = ad;
            bool granted = false;
            current.Show(reward => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (destroyed || granted) return;
                granted = true;
                Debug.Log($"Reward earned: {reward.Amount} {reward.Type}", this);
                onRewardEarned?.Invoke();
                rewardEarned.Invoke();
            }));
            return true;
        }

        private void FinishShowing(RewardedAd finished, AdError error)
        {
            if (destroyed || ad != finished) return;
            showing = false;
            DestroyAd();
            if (error != null) Debug.LogWarning($"Rewarded show failed: {error}", this);
            else adClosed.Invoke();
            if (!destroyed && reloadAfterClose) LoadAd();
        }

        private void DestroyAd()
        {
            ad?.Destroy();
            ad = null;
        }

        private void OnDestroy()
        {
            destroyed = true;
            DestroyAd();
        }
    }
}
