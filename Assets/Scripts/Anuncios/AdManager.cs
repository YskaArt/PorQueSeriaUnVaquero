using UnityEngine;
using System;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    // ola papu, las ID cambialas x las que te de Admob]
    //Nota de que ya cambie las ID papu

    private const string BANNER_ID = "ca-app-pub-8408315673471628/8656782151";
    private const string INTERSTITIAL_ID = "ca-app-pub-8408315673471628/5911199317";
    private const string REWARDED_ID = "ca-app-pub-8408315673471628/3285035971";

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private BannerView bannerView;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        MobileAds.Initialize(initStatus => {
            Debug.Log("Google Mobile Ads inicializado.");
            LoadInterstitial();
            LoadRewarded();
            ShowBanner();
        });
    }

    // -------- INTERSTITIAL --------
    private void LoadInterstitial()
    {
        AdRequest request = new AdRequest();
        InterstitialAd.Load(INTERSTITIAL_ID, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Falló cargar Interstitial: " + error);
                return;
            }
            interstitialAd = ad;
            Debug.Log("Interstitial cargado correctamente.");
        });
    }

    public void ShowInterstitial(Action onClosed)
    {
        Debug.Log("[AdManager] Simulando interstitial en Editor.");
        onClosed?.Invoke();
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.OnAdFullScreenContentClosed += () => {
                Debug.Log("Interstitial cerrado.");
                onClosed?.Invoke();
                LoadInterstitial(); // recargar
            };
            interstitialAd.Show();
        }
        else
        {
            Debug.Log("Interstitial no disponible.");
            onClosed?.Invoke();
            LoadInterstitial();
        }
    }

    // -------- REWARDED --------
    private void LoadRewarded()
    {
        AdRequest request = new AdRequest();
        RewardedAd.Load(REWARDED_ID, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Falló cargar Rewarded: " + error);
                return;
            }
            rewardedAd = ad;
            Debug.Log("Rewarded cargado correctamente.");
        });
    }

    public void ShowRewarded(Action<bool> onResult)
    {
        Debug.Log("[AdManager] Simulando rewarded en Editor.");
        onResult?.Invoke(true); // En editor sí conviene simular éxito
        return;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.OnAdFullScreenContentClosed += () => {
                Debug.Log("Rewarded cerrado.");
                LoadRewarded(); // recargar después de cerrarlo
            };

            rewardedAd.Show((Reward reward) => {
                Debug.Log("Usuario obtuvo recompensa.");
                onResult?.Invoke(true); //solo éxito si se dio la recompensa real
            });
        }
        else
        {
            Debug.Log("Rewarded no disponible.");
            onResult?.Invoke(false); //falló  no revive
            LoadRewarded();
        }
    }


    // -------- BANNER --------
    public void ShowBanner()
    {
        if (bannerView != null)
            return;
        Debug.Log("[AdManager] Simulando Banner en Editor.");
        if (bannerView != null)
            bannerView.Destroy();

        bannerView = new BannerView(BANNER_ID, AdSize.Banner, AdPosition.Bottom);
        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
    }

    public void HideBanner()
    {
        Debug.Log("[AdManager] Simulando ocultar Banner en Editor.");
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }
}
