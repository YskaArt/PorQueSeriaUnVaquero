/*******************************************************
 * AdsManager
 * -----------------------------------------------------
 * Gestiona toda la integración con Google Mobile Ads:
 *  - Inicializa el SDK al iniciar el juego.
 *  - Muestra y oculta el banner.
 *  - Carga, mantiene y muestra interstitials de forma
 *    segura y con fallback si fallan o tardan demasiado.
 *  - Garantiza que siempre haya un interstitial precargado.
 *  - Maneja eventos de cierre para volver a cargar uno nuevo.
 *
 * Es un singleton persistente entre escenas.
 *******************************************************/

using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.UI;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Ad Unit IDs (usar test ids en desarrollo)")]
    [SerializeField] private string bannerAdUnitId = "ca-app-pub-8408315673471628/8656782151";
    [SerializeField] private string interstitialAdUnitId = "ca-app-pub-8408315673471628/5911199317";

    private BannerView bannerView;
    private InterstitialAd interstitialAd;

    private bool mobileAdsInitialized = false;
    private bool interstitialLoaded = false;
    private bool interstitialClosed = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

        while (!done)
            yield return null;

        CreateAndShowBanner();
        PreloadInterstitial();
    }

    private void CreateAndShowBanner()
    {
        try
        {
            bannerView?.Destroy();
            bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

            bannerView.OnBannerAdLoaded += () => { };
            bannerView.OnBannerAdLoadFailed += (LoadAdError err) =>
            {
                Debug.LogWarning("[AdsManager] Banner failed: " + err);
            };

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

    /// <summary>
    /// Muestra un interstitial si está disponible.  
    /// Oculta el banner mientras dura la operación.
    /// </summary>
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
        catch (Exception ex)
        {
            Debug.LogWarning("[AdsManager] Error showing interstitial: " + ex);
            interstitialClosed = true;
        }

        float elapsed = 0f;
        const float maxWait = 20f;

        while (!interstitialClosed && elapsed < maxWait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator TryLoadAndShowInterstitial()
    {
        PreloadInterstitial();

        float wait = 0f;
        const float maxWaitForLoad = 10f;

        while (!interstitialLoaded && wait < maxWaitForLoad)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        if (interstitialLoaded && interstitialAd != null)
        {
            yield return ShowLoadedInterstitial();
        }
        else
        {
            Debug.LogWarning("[AdsManager] Interstitial not ready after waiting.");
        }
    }

    private void OnDestroy()
    {
        try { bannerView?.Destroy(); } catch { }
        try { interstitialAd?.Destroy(); } catch { }
    }
}
