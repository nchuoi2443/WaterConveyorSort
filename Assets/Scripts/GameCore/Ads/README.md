# Persistent AdMob test manager

Create a dedicated root GameObject named Ads in the first scene and attach AdMobManager. Place AdMobRewardedSample and AdMobInterstitialSample on child objects and assign them to the manager in the Inspector (child lookup is also supported). AdMobManager owns SDK initialization; there is no separate initializer component. AdMobManager.Instance persists through scene changes using DontDestroyOnLoad. Duplicate manager GameObjects are destroyed, and destruction of a duplicate does not clear the original Instance. Do not attach the manager to a gameplay/UI root with unrelated children.

The format components load on Start by default. UI buttons can call manager LoadRewardedAd / ShowRewardedAd or LoadInterstitialAd / ShowInterstitialAd. Code can check IsRewardedReady / IsInterstitialReady and call TryShowRewardedAd(onRewardEarned) or TryShowInterstitialAd. TryShow returns false if the ad is unavailable or another full-screen ad is showing. Instance exists only after the configured object awakens; it does not create itself automatically.

Only the reward callback grants the reward. Closing an ad is not proof of earning a reward. Prefer a per-show callback to scene UI references on persistent components; ensure a scene object still exists before accessing it in an async callback. Load failures require an explicit retry; close/show failure triggers preload by default. SDK callbacks run on the Unity main thread. Ads are destroyed when the persistent object is destroyed, rather than when a gameplay scene unloads.

All ad unit IDs are Google test IDs for Android/iOS; configure matching sample App IDs under Assets > Google Mobile Ads > Settings. Editor shows mock ads; test native loading on a device. These are learning samples, not a release integration: consent, gameplay pause restoration, and production IDs need implementation before publishing.

## Automatic full-screen timer

AdMobManager / Full Ads Rest Time is the interval in seconds (default 60; zero disables automatic showing). Update uses unscaled time only while the application is foregrounded and no ad is showing. When the interval elapses, the manager shows a ready interstitial, or waits and retries loading at most every 10 seconds. Showing either ad format resets the interval, so an interstitial does not immediately follow a rewarded ad. Scene changes preserve the timer. ResetFullAdsTimer can restart the interval manually. Assigned Inspector references are preserved, with a child-component lookup fallback.

AdMobManager initializes on Start, and both ad format components await its shared InitializeAsync task before loading. Concurrent load requests do not initialize the SDK twice. Remove any old AdMobInitializer components from unsaved scenes.

The elapsed timer is visible in the Inspector and resets on manager Awake. Editor panel focus does not suspend it; background/pause checks apply on device. Missing interstitial references warn on Awake and prevent showing, but do not stop elapsed time. Full Ads Rest Time must be greater than zero; Unity Editor Pause still stops Update.
