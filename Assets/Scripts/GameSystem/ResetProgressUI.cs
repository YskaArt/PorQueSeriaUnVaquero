/*
    ResetProgressUI
    ---------------
    Agrega a la pestana de ajustes el boton "Borrar progreso". Al tocarlo se pide una
    confirmacion (Cancelar / Borrar) antes de hacer nada.

    Si se confirma:
      1. GameSaveManager.ResetGame(): oro, mejoras, maestria, misiones y boosts vuelven a 0
         y se borra el save. El volumen (musica y efectos) NO se borra.
      2. Fundido a negro (el mismo FadeOut del GameManager) antes de cambiar nada.
      3. Se destruyen los managers persistentes que nacieron en GameScene (GameManager, oro,
         anuncios, caballo, etc.). Si no se hiciera, al volver a entrar a GameScene quedarian
         los viejos apuntando a objetos destruidos y los nuevos se autodestruirian.
      4. Se carga el menu principal con un fundido de entrada. Al volver a jugar, el juego arranca de cero y, como se
         borro la marca del tutorial, el tutorial vuelve a aparecer.

    Igual que AudioSettingsUI y el boton "Como jugar", se arma por codigo al cargar la escena,
    asi que no hay que tocar la escena.
*/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ResetProgressUI
{
    // Textos (cuando se agregue el ingles, cambiar solo aca).
    private const string ButtonLabel = "Borrar progreso";
    private const string ConfirmTitle = "¿Borrar progreso?";
    private const string ConfirmBody =
        "Perderás tu oro, mejoras, maestría y misiones.\nEmpezarás desde cero y esta acción no se puede deshacer.";
    private const string CancelLabel = "Cancelar";
    private const string ConfirmLabel = "Borrar";

    private const string MainMenuScene = "MainMenu";

    private const string ButtonName = "ResetProgressButton";
    private const string DialogName = "ResetProgressDialog";

    // Posicion dentro del contenido de ajustes (origen en el centro, y hacia arriba).
    private const float ButtonY = -120f;

    private static readonly Color DangerColor = new Color(0.75f, 0.15f, 0.12f, 1f);
    private static readonly Color NeutralColor = new Color(0.4f, 0.32f, 0.25f, 1f);
    private static readonly Color PanelColor = new Color(0.16f, 0.1f, 0.06f, 0.98f);
    private static readonly Color GoldColor = new Color(1f, 0.82f, 0.25f, 1f);

    private static TMP_FontAsset font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // Por si el dominio no se recarga entre sesiones del editor: evita suscribir dos veces.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBuild();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryBuild();

    private static void TryBuild()
    {
        var menu = Object.FindFirstObjectByType<UpgradeMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.SettingsScroll == null || menu.SettingsScroll.content == null) return;

        var content = menu.SettingsScroll.content;
        if (content.Find(ButtonName) != null) return;

        font = null;
        var existing = content.GetComponentInChildren<TextMeshProUGUI>(true);
        if (existing != null) font = existing.font;
        if (font == null) font = TMP_Settings.defaultFontAsset;

        var button = TutorialManager.CreateButton(content, ButtonName, TutorialManager.Fit(ButtonLabel, font), font, 36,
            new Vector2(440f, 100f), out _);
        button.image.color = DangerColor;

        var rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, ButtonY);

        button.onClick.AddListener(ShowConfirmation);
    }

    // =====================================================================
    // Confirmacion
    // =====================================================================

    private static void ShowConfirmation()
    {
        if (GameObject.Find(DialogName) != null) return; // ya hay uno abierto

        var overlay = new GameObject(DialogName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 6000; // por encima del tutorial (5000)

        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var root = (RectTransform)overlay.transform;

        // Fondo oscuro que bloquea todo lo de atras.
        var dim = TutorialManager.CreateImage(root, "Dim", new Color(0f, 0f, 0f, 0.8f), true);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;

        // Cuadro
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelGo.transform.SetParent(root, false);

        var panel = (RectTransform)panelGo.transform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(900f, 0f);
        panel.anchoredPosition = Vector2.zero;

        panelGo.GetComponent<Image>().color = PanelColor;

        var outline = panelGo.GetComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(4f, -4f);

        var layout = panelGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 36, 36);
        layout.spacing = 28f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        panelGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = TutorialManager.CreateText(panel, "Title", TutorialManager.Fit(ConfirmTitle, font), font, 48,
            TextAlignmentOptions.Center);
        title.color = GoldColor;
        title.raycastTarget = false;

        var body = TutorialManager.CreateText(panel, "Body", TutorialManager.Fit(ConfirmBody, font), font, 36,
            TextAlignmentOptions.Center);
        body.color = Color.white;
        body.raycastTarget = false;

        // Fila de botones
        var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(panel, false);

        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 30f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        var rowElement = row.GetComponent<LayoutElement>();
        rowElement.minHeight = 110f;
        rowElement.preferredHeight = 110f;

        var cancel = TutorialManager.CreateButton(row.transform, "Cancel", TutorialManager.Fit(CancelLabel, font), font, 38,
            new Vector2(0f, 110f), out _);
        cancel.image.color = NeutralColor;
        cancel.onClick.AddListener(() => Object.Destroy(overlay));

        var confirm = TutorialManager.CreateButton(row.transform, "Confirm", TutorialManager.Fit(ConfirmLabel, font), font, 38,
            new Vector2(0f, 110f), out _);
        confirm.image.color = DangerColor;
        confirm.onClick.AddListener(() =>
        {
            Object.Destroy(overlay);
            PerformReset();
        });
    }

    // =====================================================================
    // Accion
    // =====================================================================

    // Managers persistentes (DontDestroyOnLoad) que se crean en GameScene. Los de MainMenu
    // (GameSaveManager, SoundtrackManager) y los de progresion (ProgressionSystems) se conservan.
    private static readonly System.Type[] GameSceneSingletons =
    {
        typeof(GameManager),
        typeof(GoldManager),
        typeof(HorseCooldownManager),
        typeof(SFXManager),
        typeof(AdsManager),
        typeof(RewardedAdPrompt),
    };

    private static void DestroyGameSceneSingletons()
    {
        foreach (var type in GameSceneSingletons)
        {
            foreach (var obj in Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = ((Component)obj).gameObject;

                // Seguro extra: nunca destruir objetos que tambien tengan los managers a conservar.
                if (go.GetComponent<GameSaveManager>() != null || go.GetComponent<SoundtrackManager>() != null ||
                    go.GetComponent<MasteryManager>() != null)
                    continue;

                Object.Destroy(go);
            }
        }
    }

    private static bool resetting;

    private static void PerformReset()
    {
        if (resetting) return;

        if (GameSaveManager.Instance == null)
        {
            Debug.LogWarning("[ResetProgressUI] No hay GameSaveManager; no se puede borrar el progreso.");
            return;
        }

        resetting = true;

        // Cerrar el menu de mejoras para que se vea el fundido.
        var menu = Object.FindFirstObjectByType<UpgradeMenuController>(FindObjectsInactive.Include);
        if (menu != null && menu.MenuPanelObject != null)
            menu.MenuPanelObject.SetActive(false);

        var gameManager = GameManager.Instance;
        if (gameManager != null)
            gameManager.StartCoroutine(ResetWithFade(gameManager));
        else
            ResetAndGoToMenu();
    }

    private static IEnumerator ResetWithFade(GameManager gameManager)
    {
        yield return gameManager.FadeOut();
        ResetAndGoToMenu();
    }

    private static void ResetAndGoToMenu()
    {
        // El fundido de entrada al menu: arranca en negro y sobrevive al cambio de escena.
        FadeOverlay.Create(MainMenuScene);

        GameSaveManager.Instance.ResetGame();
        DestroyGameSceneSingletons();

        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);

        resetting = false;
        Debug.Log("[ResetProgressUI] Progreso borrado por el jugador.");
    }

    /// <summary>
    /// Pantalla negra persistente que espera a que cargue la escena indicada y se desvanece.
    /// Evita el corte brusco entre el fundido de salida de GameScene y el menu.
    /// </summary>
    private class FadeOverlay : MonoBehaviour
    {
        private const float FadeSeconds = 0.8f;

        private string targetScene;
        private CanvasGroup group;
        private float elapsed;
        private bool fading;

        public static void Create(string sceneName)
        {
            var go = new GameObject("ResetFadeOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Object.DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 7000; // por encima de todo

            var black = TutorialManager.CreateImage(go.transform, "Black", Color.black, true);
            black.rectTransform.anchorMin = Vector2.zero;
            black.rectTransform.anchorMax = Vector2.one;
            black.rectTransform.offsetMin = black.rectTransform.offsetMax = Vector2.zero;

            var overlay = go.AddComponent<FadeOverlay>();
            overlay.targetScene = sceneName;
            overlay.group = go.GetComponent<CanvasGroup>();
            overlay.group.alpha = 1f;
        }

        private void Update()
        {
            // Espera a que el menu este cargado y le deja un instante para inicializarse.
            if (!fading)
            {
                if (SceneManager.GetActiveScene().name != targetScene) return;
                fading = true;
                elapsed = -0.2f;
            }

            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(elapsed / FadeSeconds);

            if (elapsed >= FadeSeconds)
                Destroy(gameObject);
        }
    }
}
