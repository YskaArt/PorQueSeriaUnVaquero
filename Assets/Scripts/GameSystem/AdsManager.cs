using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.UI;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Ad Unit IDs (poner tus IDs, usar test ids en desarrollo)")]
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
        // Inicializar MobileAds (solo una vez) y luego crear banner + preload interstitial
        bool done = false;
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdsManager] MobileAds initialized.");
            mobileAdsInitialized = true;
            done = true;
        });
        while (!done) yield return null;

        CreateAndShowBanner();
        PreloadInterstitial(); // carga en background
    }

 
    private void CreateAndShowBanner()
    {
        try
        {
            // Destroy previous if any
            bannerView?.Destroy();
            bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

            // Eventos (actual API usa OnBannerAdLoaded / OnBannerAdLoadFailed)
            bannerView.OnBannerAdLoaded += () => { Debug.Log("[AdsManager] Banner loaded."); };
            bannerView.OnBannerAdLoadFailed += (LoadAdError err) => { Debug.LogWarning("[AdsManager] Banner failed: " + err); };

            AdRequest request = new AdRequest();
            bannerView.LoadAd(request);
            // se muestra automáticamente al cargarse
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdsManager] CreateAndShowBanner exception: " + ex);
        }
    }

    public void ShowBanner()
    {
        try
        {
            bannerView?.Show();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdsManager] ShowBanner exception: " + ex);
        }
    }

    public void HideBanner()
    {
        try
        {
            bannerView?.Hide();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdsManager] HideBanner exception: " + ex);
        }
    }
   

    private void PreloadInterstitial()
    {
        try
        {
            AdRequest request = new AdRequest();
            InterstitialAd.Load(interstitialAdUnitId, request, (InterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null || ad == null)
                {
                    Debug.LogWarning("[AdsManager] Interstitial failed to load: " + loadError);
                    interstitialAd = null;
                    interstitialLoaded = false;
                    return;
                }

                // Destruir antigua referencia si existía
                if (interstitialAd != null)
                {
                    try { interstitialAd.Destroy(); } catch { }
                }

                interstitialAd = ad;
                interstitialLoaded = true;

                // Registrar handlers
                interstitialAd.OnAdFullScreenContentClosed += HandleOnInterstitialClosed;
                interstitialAd.OnAdFullScreenContentFailed += (AdError err) =>
                {
                    Debug.LogWarning("[AdsManager] Interstitial open failed: " + err);
                    // Consideramos como cerrado para no bloquear la ejecución
                    HandleOnInterstitialClosed();
                };

                Debug.Log("[AdsManager] Interstitial loaded.");
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
        // marca que se cerró y vuelve a pre-cargar el siguiente
        interstitialClosed = true;
        interstitialLoaded = false;

        // destruir el ad viejo
        try { interstitialAd?.Destroy(); }
        catch { }

        interstitialAd = null;
        // Preload next
        PreloadInterstitial();
    }

    /// <summary>
    /// Muestra el interstitial si está cargado. Oculta el banner mientras se muestra.
    /// Este método devuelve un IEnumerator para que quien lo llame pueda hacer "yield return".
    /// </summary>
    // Dentro de AdsManager.cs
    public IEnumerator ShowInterstitial()
    {
        // Si MobileAds no inicializó, no bloqueamos (continuamos)
        if (!mobileAdsInitialized)
        {
            Debug.LogWarning("[AdsManager] MobileAds not initialized -> skipping interstitial.");
            yield break;
        }

        // Ocultar banner mientras se intenta mostrar
        HideBanner();

        // Si interstitial ya está cargado, mostrarlo
        if (interstitialLoaded && interstitialAd != null)
        {
            interstitialClosed = false;
            try
            {
                interstitialAd.Show();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AdsManager] Error showing interstitial: " + ex);
                interstitialClosed = true;
            }

            // Esperar a que se cierre (o timeout)
            float elapsed = 0f;
            const float maxWait = 20f;
            while (!interstitialClosed && elapsed < maxWait)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!interstitialClosed)
                Debug.LogWarning("[AdsManager] Interstitial show timed out after " + maxWait + "s.");
        }
        else
        {
            // Si no está cargado: solicitar carga y esperar un poco
            Debug.Log("[AdsManager] Interstitial not ready -> requesting load and waiting up to 10s.");
            PreloadInterstitial(); // solicita carga inmediata

            float wait = 0f;
            const float maxWaitForLoad = 10f;
            while (!interstitialLoaded && wait < maxWaitForLoad)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            if (interstitialLoaded && interstitialAd != null)
            {
                interstitialClosed = false;
                try
                {
                    interstitialAd.Show();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[AdsManager] Error showing interstitial after load: " + ex);
                    interstitialClosed = true;
                }

                float elapsed = 0f;
                const float maxWaitShow = 20f;
                while (!interstitialClosed && elapsed < maxWaitShow)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!interstitialClosed)
                    Debug.LogWarning("[AdsManager] Interstitial show timed out after " + maxWaitShow + "s.");
            }
            else
            {
                Debug.LogWarning("[AdsManager] Interstitial not ready after waiting -> continuing without it.");
            }
        }

        // Mostrar banner otra vez al finalizar (o si falló)
        ShowBanner();
        yield break;
    }

   

    private void OnDestroy()
    {
        try { bannerView?.Destroy(); } catch { }
        try { interstitialAd?.Destroy(); } catch { }
    }
}
