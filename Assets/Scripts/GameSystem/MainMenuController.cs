using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

/// <summary>
/// Controlador del menú principal. Inicializa los anuncios, muestra un banner,
/// y carga la última escena jugada (o una escena por defecto) al detectar input.
/// También permite iniciar escenas manualmente u ocultar/cerrar el juego.
///
/// FIX: se agregó un guard (hasStartedGame) para que StartGame()/LoadScene solo
/// se dispare UNA vez. Antes, cada toque detectado en Update() volvía a llamar
/// SceneManager.LoadScene("GameScene") -- si el jugador tocaba más de una vez
/// seguida (algo común, sobre todo si algún botón tarda en responder), se
/// interrumpía la carga de la escena a mitad de camino con una carga nueva,
/// dejando a GameManager en un estado inconsistente (por ejemplo, con
/// Time.timeScale ya en 1 sin haber llegado a mostrar la pantalla de carga).
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const string BANNER_ID = "ca-app-pub-8408315673471628/8656782151";
    private BannerView bannerView;

    [SerializeField] private string defaultScene = "GameScene";

    private bool hasStartedGame = false;

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
        if (hasStartedGame) return; // evita disparar la carga más de una vez si tocan varias veces

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            StartGame();
        }
    }


    public void StartGame()
    {
        if (hasStartedGame) return;
        hasStartedGame = true;

        SceneManager.LoadScene(defaultScene);
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