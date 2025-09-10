using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using GoogleMobileAds.Api;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float fadeOutDuration = 2f;

    [Header("Intro")]
    [SerializeField] private TextMeshProUGUI mapTitleText;
    [SerializeField] private float mapTitleDuration = 3f;

    [Header("Timer")]
    [SerializeField] private float timeBeforeMiniGame;
    [SerializeField] private float stopSpawnerDelay = 15f;

    [Header("Refs")]
    [SerializeField] private InfiniteTilemapLoop tilemapScroller;
    [SerializeField] private MiniGameController miniGame;

    private EnemySpawner spawner;
    [SerializeField] private float remainingTime;

    // Ads
    private const string INTERSTITIAL_ID = "ca-app-pub-8408315673471628/5911199317";
    private const string BANNER_ID = "ca-app-pub-8408315673471628/8656782151";
    private InterstitialAd interstitialAd;
    private BannerView bannerView;
    private bool isInterstitialLoaded = false;
    private bool isInterstitialClosed = false;
    private bool mobileAdsInitialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        StartCoroutine(InitAndShowInterstitialFlow());
    }

    private IEnumerator InitAndShowInterstitialFlow()
    {
        // Inicializar MobileAds solo una vez
        if (!mobileAdsInitialized)
        {
            bool done = false;
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[GameStartManager] MobileAds inicializado.");
                mobileAdsInitialized = true;
                done = true;
            });
            while (!done) yield return null;
        }

        // Cargar interstitial
        isInterstitialLoaded = false;
        isInterstitialClosed = false;
        LoadInterstitial();

        // Esperar a que el interstitial esté cargado o timeout
        float wait = 0f;
        const float maxWait = 8f;
        while (!isInterstitialLoaded && wait < maxWait)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        // Ocultar banner antes de mostrar interstitial
        HideBanner();

        if (isInterstitialLoaded && interstitialAd != null)
        {
            Debug.Log("[GameStartManager] Mostrando interstitial...");
            try
            {
                interstitialAd.Show();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameStartManager] Error al mostrar interstitial: " + ex);
                isInterstitialClosed = true;
            }
            // Esperar a que el usuario cierre el interstitial
            while (!isInterstitialClosed)
                yield return null;
            Debug.Log("[GameStartManager] Interstitial cerrado.");
        }
        else
        {
            Debug.LogWarning("[GameStartManager] Interstitial no listo tras timeout, mostrando banner.");
        }

        // Siempre mostrar banner después del interstitial o si no hay interstitial
        ShowBanner();

        // Cargar un tiempo guardado (si hay), o generar entre 180–300 seg (3–5 min)
        float savedTime = GameSaveManager.Instance != null ? GameSaveManager.Instance.GetSavedTimer() : 0f;
        remainingTime = savedTime > 0f ? savedTime : UnityEngine.Random.Range(180f, 300f);

        yield return StartCoroutine(PlayIntroSequence());
    }

    private void LoadInterstitial()
    {
        AdRequest request = new AdRequest();
        InterstitialAd.Load(INTERSTITIAL_ID, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[GameStartManager] Falló cargar Interstitial: " + error);
                interstitialAd = null;
                isInterstitialLoaded = false;
                return;
            }
            if (interstitialAd != null)
            {
                try { interstitialAd.Destroy(); } catch { }
            }
            interstitialAd = ad;
            isInterstitialLoaded = true;
            interstitialAd.OnAdFullScreenContentClosed += OnInterstitialClosed;
            interstitialAd.OnAdFullScreenContentFailed += (err) => { Debug.LogWarning("[GameStartManager] Interstitial failed: " + err); OnInterstitialClosed(); };
            Debug.Log("[GameStartManager] Interstitial cargado correctamente.");
        });
    }

    private void OnInterstitialClosed()
    {
        isInterstitialClosed = true;
        // Preload next interstitial for future use
        LoadInterstitial();
    }

    private IEnumerator PlayIntroSequence()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = Color.black;
        yield return new WaitForSeconds(0.5f);

        float t = 0f;
        while (t < fadeInDuration)
        {
            fadeImage.color = Color.Lerp(Color.black, Color.clear, t / fadeInDuration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = Color.clear;
        fadeImage.gameObject.SetActive(false);

        if (mapTitleText != null)
        {
            mapTitleText.gameObject.SetActive(true);
            yield return new WaitForSeconds(mapTitleDuration);
            mapTitleText.gameObject.SetActive(false);
        }

        float autosaveTicker = 0f;
        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            autosaveTicker += Time.deltaTime;

            if (autosaveTicker >= 2f && GameSaveManager.Instance != null)
            {
                autosaveTicker = 0f;
                GameSaveManager.Instance.SaveGame();
            }

            yield return null;
        }

        if (spawner != null) spawner.StopSpawning();
        while (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
            yield return null;

        yield return new WaitForSeconds(stopSpawnerDelay);

        if (miniGame != null) miniGame.StartMiniGame();
    }

    private void ShowBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
        Debug.Log("[GameStartManager] Mostrando Banner.");
        try
        {
            bannerView = new BannerView(BANNER_ID, AdSize.Banner, AdPosition.Bottom);
            AdRequest request = new AdRequest();
            bannerView.LoadAd(request);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameStartManager] Error al crear banner: " + ex);
        }
    }

    private void HideBanner()
    {
        if (bannerView != null)
        {
            Debug.Log("[GameStartManager] Ocultando Banner.");
            try { bannerView.Destroy(); } catch { }
            bannerView = null;
        }
    }

    public void EndSceneAndLoadNext(string nextSceneName)
    {
        StartCoroutine(EndSceneRoutine(nextSceneName));
    }

    private IEnumerator EndSceneRoutine(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeOutDuration)
        {
            fadeImage.color = Color.Lerp(Color.clear, Color.black, t / fadeOutDuration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = Color.black;

        // Guardar progreso antes de salir
        GameSaveManager.Instance?.SaveGame();

        SceneManager.LoadScene(sceneName);

        yield break;
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(remainingTime, 0f);
    }
}
