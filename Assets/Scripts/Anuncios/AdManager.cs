using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    private const string BANNER_ID = "ca-app-pub-8408315673471628/8656782151";
    private const string INTERSTITIAL_ID = "ca-app-pub-8408315673471628/5911199317";
    private const string REWARDED_ID = "ca-app-pub-8408315673471628/3285035971";

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private BannerView bannerView;

    private bool isShowingInterstitial = false;
    private Action interstitialCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdManager] Google Mobile Ads inicializado.");
            LoadInterstitial();
            LoadRewarded();
            ShowBanner();
        });
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cuando carga una escena, escondemos el banner y mostramos intersticial si está listo
        HideBanner();

        // Intentamos mostrar un interstitial; si no está listo, ShowInterstitial mostrará el banner
        ShowInterstitial(() => { /* callback vacío: banner será mostrado dentro de ShowInterstitial flow */ });
    }

    // ----------------------------
    // INTERSTITIAL
    // ----------------------------
    private void LoadInterstitial()
    {
        AdRequest request = new AdRequest();

        InterstitialAd.Load(INTERSTITIAL_ID, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdManager] Falló cargar Interstitial: " + error);
                interstitialAd = null;
                return;
            }

            if (interstitialAd != null)
            {
                try { interstitialAd.Destroy(); } catch { }
            }

            interstitialAd = ad;
            Debug.Log("[AdManager] Interstitial cargado correctamente.");
        });
    }

    public void ShowInterstitial(Action onClosed)
    {
        // If already showing, immediately invoke callback
        if (isShowingInterstitial)
        {
            Debug.Log("[AdManager] Ya hay un interstitial mostrándose.");
            onClosed?.Invoke();
            return;
        }

        // Editor simulation path
        if (Application.isEditor)
        {
            StartCoroutine(SimulateInterstitialCoroutine(onClosed));
            return;
        }

        // Production path: show real interstitial if available
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            isShowingInterstitial = true;
            interstitialCallback = onClosed;

            // Attach handlers
            interstitialAd.OnAdFullScreenContentClosed += HandleInterstitialClosed;
            interstitialAd.OnAdFullScreenContentFailed += HandleInterstitialFailed;

            try
            {
                interstitialAd.Show();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AdManager] Error al mostrar interstitial: " + ex);
                HandleInterstitialClosedFallback();
            }
        }
        else
        {
            Debug.Log("[AdManager] Interstitial no disponible — mostrando banner en su lugar.");
            // Fall back: show banner immediately and request a load for next time.
            ShowBanner();
            onClosed?.Invoke();
            LoadInterstitial();
        }
    }

    private void HandleInterstitialClosed()
    {
        CleanupInterstitialEvents();

        var cb = interstitialCallback;
        interstitialCallback = null;

        isShowingInterstitial = false;

        // Preload next interstitial
        LoadInterstitial();

        // Después de cerrar, mostramos el banner de nuevo
        ShowBanner();

        cb?.Invoke();
    }

    private void HandleInterstitialFailed(AdError error)
    {
        Debug.LogWarning("[AdManager] Interstitial fallo full-screen: " + error);
        HandleInterstitialClosedFallback();
    }

    private void HandleInterstitialClosedFallback()
    {
        CleanupInterstitialEvents();
        var cb = interstitialCallback;
        interstitialCallback = null;
        isShowingInterstitial = false;

        LoadInterstitial();

        // Mostramos banner después del fallback también
        ShowBanner();

        cb?.Invoke();
    }

    private void CleanupInterstitialEvents()
    {
        if (interstitialAd != null)
        {
            try
            {
                interstitialAd.OnAdFullScreenContentClosed -= HandleInterstitialClosed;
                interstitialAd.OnAdFullScreenContentFailed -= HandleInterstitialFailed;
            }
            catch { }
        }
    }

    private System.Collections.IEnumerator SimulateInterstitialCoroutine(Action onClosed)
    {
        Debug.Log("[AdManager] (Editor) Simulando interstitial...");
        isShowingInterstitial = true;
        float fakeDuration = 1.0f;
        float t = 0f;
        while (t < fakeDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        isShowingInterstitial = false;
        Debug.Log("[AdManager] (Editor) Fin simulación interstitial.");

        // Simulación: mostramos banner otra vez
        ShowBanner();

        onClosed?.Invoke();
        LoadInterstitial();
    }

    // ----------------------------
    // REWARDED
    // ----------------------------
    private void LoadRewarded()
    {
        AdRequest request = new AdRequest();

        RewardedAd.Load(REWARDED_ID, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdManager] Falló cargar Rewarded: " + error);
                rewardedAd = null;
                return;
            }

            rewardedAd = ad;
            Debug.Log("[AdManager] Rewarded cargado correctamente.");
        });
    }

    public void ShowRewarded(Action<bool> onResult)
    {
        if (Application.isEditor)
        {
            Debug.Log("[AdManager] Simulando rewarded en Editor.");
            onResult?.Invoke(true);
            return;
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                LoadRewarded();
            };

            rewardedAd.Show((Reward reward) =>
            {
                onResult?.Invoke(true);
            });
        }
        else
        {
            Debug.Log("[AdManager] Rewarded no disponible.");
            onResult?.Invoke(false);
            LoadRewarded();
        }
    }

    // ----------------------------
    // BANNER
    // ----------------------------
    public void ShowBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        Debug.Log("[AdManager] Mostrando Banner.");
        try
        {
            bannerView = new BannerView(BANNER_ID, AdSize.Banner, AdPosition.Bottom);
            AdRequest request = new AdRequest();
            bannerView.LoadAd(request);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdManager] Error al crear banner: " + ex);
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            Debug.Log("[AdManager] Ocultando Banner.");
            try
            {
                bannerView.Destroy();
            }
            catch { }
            finally
            {
                bannerView = null;
            }
        }
    }
}
