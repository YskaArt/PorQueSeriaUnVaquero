using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

public class MainMenuController : MonoBehaviour
{
    private const string BANNER_ID = "ca-app-pub-8408315673471628/8656782151";
    private BannerView bannerView;
    [SerializeField] private string defaultScene = "Desert";

    private void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[MainMenuController] MobileAds inicializado desde MainMenu.");
            ShowBanner();
        });
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            LoadGame();
        }
    }

    private void LoadGame()
    {
        string lastScene = null;

        if (GameSaveManager.Instance != null)
        {
            lastScene = GameSaveManager.Instance.GetLastScene();
        }

        string sceneToLoad = !string.IsNullOrEmpty(lastScene) ? lastScene : defaultScene;
        SceneManager.LoadScene(sceneToLoad);
    }

    public void StartGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        Debug.Log("[MainMenuController] Mostrando Banner.");
        try
        {
            bannerView = new BannerView(BANNER_ID, AdSize.Banner, AdPosition.Bottom);
            AdRequest request = new AdRequest();
            bannerView.LoadAd(request);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[MainMenuController] Error al crear banner: " + ex);
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            Debug.Log("[MainMenuController] Ocultando Banner.");
            try { bannerView.Destroy(); } catch { }
            bannerView = null;
        }
    }
}
