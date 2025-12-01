using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.UI;

/// <summary>
/// AdsManager (robusto)
/// - Preload de RewardedAd
/// - ShowRewardedAdCoroutine maneja preloaded y carga bajo demanda con timeout
/// - Logs detallados para debug
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Ad Unit IDs (usar test ids en desarrollo)")]
    [SerializeField] private string bannerAdUnitId = "ca-app-pub-8408315673471628/8656782151";
    [SerializeField] private string interstitialAdUnitId = "ca-app-pub-8408315673471628/5911199317";

    // Rewarded ID que indicaste
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-8408315673471628/3285035971";

    private BannerView bannerView;
    private InterstitialAd interstitialAd;

    // Rewarded handling
    private RewardedAd rewardedAd = null;
    private bool rewardedLoaded = false;

    private bool mobileAdsInitialized = false;
    private bool interstitialLoaded = false;
    private bool interstitialClosed = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(InitMobileAdsCoroutine());
    }

    private IEnumerator InitMobileAdsCoroutine()
    {
        bool done = false;
        MobileAds.Initialize(initStatus =>
        {
            mobileAdsInitialized = true;
            done = true;
        });

        while (!done) yield return null;

        CreateAndShowBanner();
        PreloadInterstitial();
        PreloadRewarded(); // iniciar pre-load del rewarded
    }

    private void CreateAndShowBanner()
    {
        try
        {
            bannerView?.Destroy();
            bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

            bannerView.OnBannerAdLoaded += () => { Debug.Log("[AdsManager] Banner loaded"); };
            bannerView.OnBannerAdLoadFailed += (LoadAdError err) => { Debug.LogWarning("[AdsManager] Banner failed: " + err); };

            var request = new AdRequest();
            bannerView.LoadAd(request);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdsManager] CreateAndShowBanner exception: " + ex);
        }
    }

    public void ShowBanner()
    {
        try { bannerView?.Show(); }
        catch (Exception ex) { Debug.LogWarning("[AdsManager] ShowBanner exception: " + ex); }
    }

    public void HideBanner()
    {
        try { bannerView?.Hide(); }
        catch (Exception ex) { Debug.LogWarning("[AdsManager] HideBanner exception: " + ex); }
    }

    private void PreloadInterstitial()
    {
        try
        {
            var request = new AdRequest();
            InterstitialAd.Load(interstitialAdUnitId, request, (InterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null || ad == null)
                {
                    Debug.LogWarning("[AdsManager] Interstitial failed to load: " + loadError);
                    interstitialAd = null;
                    interstitialLoaded = false;
                    return;
                }

                interstitialAd?.Destroy();
                interstitialAd = ad;
                interstitialLoaded = true;

                interstitialAd.OnAdFullScreenContentClosed += HandleOnInterstitialClosed;
                interstitialAd.OnAdFullScreenContentFailed += (AdError err) =>
                {
                    Debug.LogWarning("[AdsManager] Interstitial open failed: " + err);
                    HandleOnInterstitialClosed();
                };

                Debug.Log("[AdsManager] Interstitial preloaded");
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdsManager] PreloadInterstitial exception: " + ex);
            interstitialLoaded = false;
        }
    }

    private void HandleOnInterstitialClosed()
    {
        interstitialClosed = true;
        interstitialLoaded = false;
        try { interstitialAd?.Destroy(); } catch { }
        interstitialAd = null;
        PreloadInterstitial();
    }

    public IEnumerator ShowInterstitial()
    {
        if (!mobileAdsInitialized)
            yield break;

        HideBanner();

        if (interstitialLoaded && interstitialAd != null)
        {
            yield return ShowLoadedInterstitial();
        }
        else
        {
            yield return TryLoadAndShowInterstitial();
        }

        ShowBanner();
    }

    private IEnumerator ShowLoadedInterstitial()
    {
        interstitialClosed = false;
        try { interstitialAd.Show(); }
        catch (Exception ex) { Debug.LogWarning("[AdsManager] Error showing interstitial: " + ex); interstitialClosed = true; }
        float elapsed = 0f; const float maxWait = 20f;
        while (!interstitialClosed && elapsed < maxWait) { elapsed += Time.deltaTime; yield return null; }
    }

    private IEnumerator TryLoadAndShowInterstitial()
    {
        PreloadInterstitial();
        float wait = 0f; const float maxWaitForLoad = 10f;
        while (!interstitialLoaded && wait < maxWaitForLoad) { wait += Time.deltaTime; yield return null; }
        if (interstitialLoaded && interstitialAd != null) { yield return ShowLoadedInterstitial(); }
        else Debug.LogWarning("[AdsManager] Interstitial not ready after waiting.");
    }

    // ---------------- Rewarded ----------------

    private void PreloadRewarded()
    {
        if (!mobileAdsInitialized)
        {
            Debug.Log("[AdsManager] PreloadRewarded skipped: SDK not initialized yet.");
            return;
        }

        try
        {
            Debug.Log("[AdsManager] Preloading rewarded ad...");
            RewardedAd.Load(rewardedAdUnitId, new AdRequest(), (RewardedAd ad, LoadAdError loadError) =>
            {
                if (loadError != null || ad == null)
                {
                    Debug.LogWarning("[AdsManager] Rewarded failed to preload: " + loadError);
                    rewardedAd = null;
                    rewardedLoaded = false;
                    return;
                }

                // Assign preloaded ad
                rewardedAd = ad;
                rewardedLoaded = true;

                // Provide some logging
                Debug.Log("[AdsManager] Rewarded preloaded successfully.");
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdsManager] Exception in PreloadRewarded: " + ex);
            rewardedAd = null;
            rewardedLoaded = false;
        }
    }

    /// <summary>
    /// Muestra un rewarded ad. Usa el preloaded si existe; si no, intentará cargar y mostrar.
    /// Llama onComplete(true) si el usuario obtuvo reward, false si falló/no reward.
    /// </summary>
    public IEnumerator ShowRewardedAdCoroutine(Action<bool> onComplete)
    {
        if (!mobileAdsInitialized)
        {
            Debug.LogWarning("[AdsManager] MobileAds not initialized -> skipping rewarded.");
            onComplete?.Invoke(false);
            yield break;
        }

        bool finished = false;
        bool granted = false;

        // If we have a preloaded rewarded, use it
        if (rewardedLoaded && rewardedAd != null)
        {
            Debug.Log("[AdsManager] Showing preloaded rewarded ad...");
            try
            {
                // Subscribe to full screen closed to mark finished
                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    Debug.Log("[AdsManager] Rewarded closed (preloaded).");
                    finished = true;
                };

                rewardedAd.OnAdFullScreenContentFailed += (AdError err) =>
                {
                    Debug.LogWarning("[AdsManager] Rewarded failed to show (preloaded): " + err);
                    finished = true;
                };

                // Show and pass reward callback
                rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log("[AdsManager] Rewarded: user earned reward.");
                    granted = true;
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AdsManager] Exception showing preloaded rewarded: " + ex);
                finished = true;
            }
        }
        else
        {
            Debug.Log("[AdsManager] No preloaded rewarded — loading and showing now...");
            // Attempt to load and immediately show (with timeouts)
            bool loadDone = false;
            RewardedAd loadedAd = null;
            try
            {
                RewardedAd.Load(rewardedAdUnitId, new AdRequest(), (RewardedAd ad, LoadAdError loadError) =>
                {
                    if (loadError != null || ad == null)
                    {
                        Debug.LogWarning("[AdsManager] Rewarded failed to load: " + loadError);
                        loadDone = true;
                        loadedAd = null;
                        return;
                    }

                    loadedAd = ad;
                    loadDone = true;
                    Debug.Log("[AdsManager] Rewarded loaded (on-demand).");

                    // subscribe events
                    loadedAd.OnAdFullScreenContentClosed += () =>
                    {
                        Debug.Log("[AdsManager] Rewarded closed (on-demand).");
                        finished = true;
                    };
                    loadedAd.OnAdFullScreenContentFailed += (AdError err) =>
                    {
                        Debug.LogWarning("[AdsManager] Rewarded failed to show (on-demand): " + err);
                        finished = true;
                    };

                    try
                    {
                        loadedAd.Show((Reward reward) =>
                        {
                            Debug.Log("[AdsManager] Rewarded: user earned reward (on-demand).");
                            granted = true;
                        });
                    }
                    catch (Exception ex2)
                    {
                        Debug.LogWarning("[AdsManager] Exception showing on-demand rewarded: " + ex2);
                        finished = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AdsManager] Exception while loading/showing rewarded: " + ex);
                finished = true;
            }

            // Wait for load to start showing or timeout
            float waitLoad = 0f; const float maxLoadWait = 10f;
            while (!loadDone && waitLoad < maxLoadWait) { waitLoad += Time.deltaTime; yield return null; }

            if (!loadDone)
            {
                Debug.LogWarning("[AdsManager] Rewarded on-demand load timed out.");
                finished = true;
            }
        }

        // Wait until ad closed or timeout
        float elapsed = 0f;
        const float maxWait = 30f;
        while (!finished && elapsed < maxWait) { elapsed += Time.deltaTime; yield return null; }

        if (!finished) Debug.LogWarning("[AdsManager] Rewarded ad overall timeout.");

        // If we used a preloaded ad, destroy reference and preload next
        if (rewardedAd != null)
        {
            try { /* no explicit Destroy API for RewardedAd instance in newer SDKs; clear ref */ }
            catch { }
            rewardedAd = null;
            rewardedLoaded = false;
            // Preload the next one in background
            PreloadRewarded();
        }
        else
        {
            // If loaded on-demand, also kick off a preload for next
            PreloadRewarded();
        }

        Debug.Log("[AdsManager] Rewarded finished. Granted=" + granted);
        onComplete?.Invoke(granted);
    }

    private void OnDestroy()
    {
        try { bannerView?.Destroy(); } catch { }
        try { interstitialAd?.Destroy(); } catch { }
        rewardedAd = null;
    }
}
