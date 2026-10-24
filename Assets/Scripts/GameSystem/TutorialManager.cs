/*
    TutorialManager
    ---------------
    Tutorial jugable. Se ejecuta solo la primera vez que se juega y se puede repetir
    desde el boton "Como jugar" (se agrega solo en la pestana de ajustes del menu
    de mejoras).

    Primera vez (obligatorio, con acciones reales del jugador):
      1. Mover al vaquero entre carriles.
      2. Derrotar un enemigo.
      3. Juntar oro hasta alcanzar el costo de la primera mejora (sin regalar oro:
         se generan enemigos de a uno hasta llegar justo a lo que cuesta).
      4. Abrir el menu de mejoras, comprar la primera mejora y cerrarlo.
      5. Activar la habilidad del caballo.
      6. Bonus: el juego se pausa, se explica, se suelta un item y se repite hasta que
         el jugador lo agarra.
      7. Jefe de practica: se pelea contra un jefe de prueba (sin oro ni cambio de zona).

    Repeticion ("Como jugar"): igual pero sin gastar oro, sin forzar la compra, sin efecto
    del bonus y con boton para saltar.

    Como funciona por dentro:
    - Todo el overlay (oscurecido, agujero sobre el boton a resaltar, marco, cuadro de
      texto) se arma por codigo en un Canvas propio, asi no hay que tocar la escena.
    - Los botones a resaltar se buscan en runtime (hay getters publicos en los
      componentes del juego); no hay que asignar nada en el inspector.
    - Durante el tutorial el spawner queda controlado por este script y los bonus
      aleatorios se suprimen. Al terminar todo vuelve a la normalidad.
    - El progreso se guarda en PlayerPrefs ("TutorialSeen"); GameSaveManager lo borra
      al resetear la partida, asi que vuelve a aparecer tras un reset.

    Los campos page1 / page2 / tutorialPanel y los metodos ShowPage1/ShowPage2/
    CloseTutorial son del tutorial viejo de dos paginas. Se mantienen solo para no
    romper las referencias de la escena; el panel viejo queda siempre oculto y se
    puede borrar de la escena cuando se quiera.
*/

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string TutorialSeenKey = "TutorialSeen";

    [Header("Legacy (tutorial viejo de 2 paginas, ya no se usa)")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;
    [SerializeField] private GameObject tutorialPanel;

    [Header("Ajustes")]
    [Tooltip("Margen del cuadro de texto contra el borde de la pantalla (en unidades del canvas del tutorial, 1080x1920).")]
    [SerializeField] private float panelMargin = 220f;
    [Tooltip("Cantidad de cambios de carril que se piden en el primer paso.")]
    [SerializeField] private int laneChangesRequired = 2;
    [Tooltip("Cuantos segundos sigue el juego despues de activar el caballo antes de pasar a las tarjetas.")]
    [SerializeField] private float horseShowcaseSeconds = 4f;

    public bool IsRunning { get; private set; }

    private enum OverlayMode
    {
        None,       // sin overlay: el juego se usa normal
        Passive,    // transparente; bloquea la UI pero deja pasar el input del juego (swipes)
        Highlight,  // como Passive + marco sobre el objetivo
        Focus,      // oscurece todo menos el objetivo, que es lo unico clickeable
        Dim         // oscurece todo y bloquea
    }

    // ---- referencias del juego (se buscan al iniciar) ----
    private PlayerMovement player;
    private EnemySpawner spawner;
    private HorseSkillController horse;
    private UpgradeMenuToggle upgradeToggle;
    private UpgradeMenuController upgradeMenu;
    private TMP_FontAsset gameFont;

    // ---- overlay ----
    private GameObject overlayRoot;
    private RectTransform root;
    private Image[] dimPieces;
    private Image[] framePieces;
    private RectTransform panel;
    private TextMeshProUGUI panelText;
    private Button nextButton;
    private TextMeshProUGUI nextLabel;
    private Button skipButton;

    // ---- estado de paso ----
    private OverlayMode mode;
    private Func<RectTransform> targetFn;
    private Func<string> textFn;
    private string lastText;
    private bool nextClicked;
    private bool skipRequested;
    private bool replay;

    // ---- control de enemigos ----
    private Func<bool> feederWanted;
    private RunnerEnemy feederEnemy;
    private Coroutine feederRoutine;

    private readonly Vector3[] cornerBuffer = new Vector3[4];

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // El panel del tutorial viejo queda siempre oculto.
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        InjectHowToPlayButton();
        StartCoroutine(AutoStartRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Cleanup();
    }

    // =====================================================================
    // API publica
    // =====================================================================

    /// <summary>Lanza el tutorial. Lo llama el boton "Como jugar" (isReplay = true).</summary>
    public void StartTutorial(bool isReplay = true)
    {
        if (IsRunning) return;

        var movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null && movement.IsLockedForMiniGame()) return; // no durante el jefe

        StartCoroutine(RunTutorial(isReplay));
    }

    // Compatibilidad con el tutorial viejo (botones de la escena).
    public void ShowTutorial() => StartTutorial(true);
    public void ShowPage1() { }
    public void ShowPage2() { }
    public void CloseTutorial() { if (tutorialPanel != null) tutorialPanel.SetActive(false); }

    // =====================================================================
    // Arranque automatico (primera vez)
    // =====================================================================

    private IEnumerator AutoStartRoutine()
    {
        if (HasSeenTutorial()) yield break;

        // Esperar a que termine la carga inicial (pantalla de carga, fade y bienvenida).
        float waited = 0f;
        while ((GameManager.Instance == null || !GameManager.Instance.IsGameReady) && waited < 15f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.5f);

        if (HasSeenTutorial()) yield break;

        // Jugadores que ya tenian partida antes de que existiera este tutorial: no se les
        // impone. Lo pueden ver desde "Como jugar".
        if (HasPlayerProgress())
        {
            MarkSeen();
            yield break;
        }

        StartTutorial(false);
    }

    private static bool HasSeenTutorial() => PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

    private static void MarkSeen()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
    }

    private static bool HasPlayerProgress()
    {
        if (MasteryManager.Instance != null && MasteryManager.Instance.PrestigeCount > 0) return true;

        foreach (var ui in FindObjectsByType<UpgradeUIBaseCommon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = ui.GetUpgradeData();
            if (data != null && (data.currentLevel > 0 || data.bonusCount > 0)) return true;
        }
        return false;
    }

    // =====================================================================
    // Secuencia
    // =====================================================================

    private IEnumerator RunTutorial(bool isReplay)
    {
        IsRunning = true;
        replay = isReplay;
        skipRequested = false;

        FindReferences();
        BuildOverlay();
        PrepareWorld();

        bool completed = false;
        try
        {
            yield return RunSteps();
            completed = true;
        }
        finally
        {
            Cleanup();
        }

        // Tambien cuenta como visto si lo saltaron (solo se puede saltar en repeticiones).
        if (completed && !replay)
        {
            MarkSeen();

            // El caballo se uso en el tutorial: se reinicia para que el jugador arranque con la habilidad lista.
            HorseCooldownManager.Instance?.SetRemainingCooldown(0f);
        }
    }

    private IEnumerator RunSteps()
    {
        // ---- 1. Bienvenida ----
        yield return Card(replay ? TutorialTexts.WelcomeReplay : TutorialTexts.Welcome, null, OverlayMode.Dim,
            replay ? TutorialTexts.Next : TutorialTexts.Start);
        if (skipRequested) yield break;

        // ---- 2. Carriles ----
        if (player != null)
        {
            int laneChanges = 0;
            Action<int> onLane = _ => laneChanges++;
            player.OnLaneChanged += onLane;

            yield return WaitFor(() => laneChanges >= laneChangesRequired,
                () => TutorialTexts.Lanes(Mathf.Min(laneChanges, laneChangesRequired), laneChangesRequired),
                null, OverlayMode.Passive);

            player.OnLaneChanged -= onLane;
            if (skipRequested) yield break;
        }

        // ---- 3. Primer enemigo ----
        if (spawner != null)
        {
            bool killed = false;
            Action<RunnerEnemy> onKill = _ => killed = true;
            RunnerEnemy.OnAnyEnemyKilled += onKill;

            StartFeeder(() => !killed);
            yield return WaitFor(() => killed, () => TutorialTexts.Kill, null, OverlayMode.Passive);

            RunnerEnemy.OnAnyEnemyKilled -= onKill;
            StopFeeder();
            if (skipRequested) yield break;
        }

        // ---- 4. Oro + mejoras ----
        var upgradeUI = FindFirstUpgradeUI();
        var gold = GoldManager.Instance;

        if (!replay && upgradeUI != null && gold != null && spawner != null)
        {
            var data = upgradeUI.GetUpgradeData();
            double cost = data.GetCost();

            // Oro: solo enemigos, de a uno, hasta llegar justo al costo. No se regala nada.
            if (gold.CurrentGold < cost)
            {
                StartFeeder(() => gold.CurrentGold < cost);
                yield return WaitFor(() => gold.CurrentGold >= cost,
                    () => TutorialTexts.Gold(GoldManager.FormatNumber(cost), GoldManager.FormatNumber(gold.CurrentGold)),
                    () => gold.GoldText != null ? gold.GoldText.rectTransform : null,
                    OverlayMode.Highlight);
                StopFeeder();
                if (skipRequested) yield break;
            }

            // Abrir el menu.
            GameObject menuPanel = upgradeToggle != null ? upgradeToggle.MenuPanelObject : null;
            if (upgradeToggle != null && upgradeToggle.ToggleButton != null && menuPanel != null)
            {
                yield return WaitFor(() => menuPanel.activeInHierarchy,
                    () => TutorialTexts.OpenMenu,
                    () => upgradeToggle.ToggleButton.transform as RectTransform,
                    OverlayMode.Focus);
                if (skipRequested) yield break;
            }

            // Comprar.
            yield return WaitFor(() => data.currentLevel >= 1,
                () => TutorialTexts.Buy(data.upgradeName),
                () => upgradeUI.BuyButton != null ? upgradeUI.BuyButton.transform as RectTransform : null,
                OverlayMode.Focus);
            if (skipRequested) yield break;

            // Cerrar el menu.
            if (upgradeToggle != null && menuPanel != null)
            {
                yield return WaitFor(() => !menuPanel.activeInHierarchy,
                    () => TutorialTexts.CloseMenu,
                    () => upgradeMenu != null && upgradeMenu.CloseButton != null
                        ? upgradeMenu.CloseButton.transform as RectTransform
                        : upgradeToggle.ToggleButton.transform as RectTransform,
                    OverlayMode.Focus);
                if (skipRequested) yield break;
            }
        }
        else if (replay)
        {
            yield return Card(TutorialTexts.GoldInfo, null, OverlayMode.Dim);
            if (skipRequested) yield break;
            yield return Card(TutorialTexts.UpgradesInfo, null, OverlayMode.Dim);
            if (skipRequested) yield break;
        }

        // ---- 5. Caballo ----
        Button horseButton = horse != null ? horse.HorseButton : null;
        bool horseReady = HorseCooldownManager.Instance == null || HorseCooldownManager.Instance.IsReady();

        if (!replay && horse != null && horseButton != null && horseReady && spawner != null)
        {
            // El modo frenesi del spawner necesita que el spawn este encendido.
            spawner.StartSpawning();

            yield return WaitFor(() => horse.IsActive, () => TutorialTexts.Horse,
                () => horseButton.transform as RectTransform, OverlayMode.Focus);
            if (skipRequested) yield break;

            yield return Timed(() => TutorialTexts.HorseDone, null, OverlayMode.None, horseShowcaseSeconds);
            if (skipRequested) yield break;
        }
        else
        {
            yield return Card(TutorialTexts.HorseInfo, null, OverlayMode.Dim);
            if (skipRequested) yield break;
        }

        // ---- 6. Bonus ----
        yield return BonusPractice();
        if (skipRequested) yield break;

        // ---- 7. Jefe de practica ----
        yield return BossPractice();
        if (skipRequested) yield break;

        yield return Card(replay ? TutorialTexts.EndReplay : TutorialTexts.End, null, OverlayMode.Dim, TutorialTexts.Finish);
    }

    // =====================================================================
    // Pasos genericos
    // =====================================================================

    /// <summary>Tarjeta de texto con boton. Espera a que el jugador lo toque.</summary>
    private IEnumerator Card(string text, Func<RectTransform> target, OverlayMode overlay, string buttonLabel = null)
    {
        Show(() => text, target, overlay, true, buttonLabel ?? TutorialTexts.Next);
        nextClicked = false;
        while (!nextClicked && !skipRequested)
            yield return null;
    }

    /// <summary>Muestra un mensaje y espera hasta que se cumpla la condicion (accion del jugador).</summary>
    private IEnumerator WaitFor(Func<bool> condition, Func<string> text, Func<RectTransform> target, OverlayMode overlay)
    {
        Show(text, target, overlay, false, null);
        while (!condition() && !skipRequested)
            yield return null;
    }

    private IEnumerator Timed(Func<string> text, Func<RectTransform> target, OverlayMode overlay, float seconds)
    {
        Show(text, target, overlay, false, null);
        float t = 0f;
        while (t < seconds && !skipRequested)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Pausa el juego, explica el bonus, suelta el item y reanuda. Si el jugador no lo
    /// agarra, vuelve a pausar y a soltar otro hasta que lo consiga.
    /// </summary>
    private IEnumerator BonusPractice()
    {
        if (spawner == null)
            yield break;

        spawner.StopSpawning();
        ClearScreenObjects();

        bool picked = false;
        Action<BonusPickup> onPick = _ => picked = true;
        BonusPickup.OnAnyBonusPicked += onPick;

        bool first = true;
        while (!picked && !skipRequested)
        {
            SetGamePaused(true);

            if (first)
                yield return Card(TutorialTexts.BonusIntro, null, OverlayMode.Dim, TutorialTexts.Understood);
            else
                yield return Timed(() => TutorialTexts.BonusMissed, null, OverlayMode.Dim, 1.5f);
            first = false;
            if (skipRequested) break;

            var item = spawner.SpawnBonusForTutorial();
            if (item == null) break; // el spawner no tiene un bonus normal que soltar

            // Tipo fijo (nada de frenesi) y, en repeticiones, sin efecto real.
            item.SetTutorialOverride(BonusManager.BonusType.GPSDouble, !replay);

            SetGamePaused(false);
            yield return WaitFor(() => picked || !item.gameObject.activeInHierarchy,
                () => TutorialTexts.BonusCatch, null, OverlayMode.Passive);
        }

        BonusPickup.OnAnyBonusPicked -= onPick;
        SetGamePaused(false);

        if (picked && !skipRequested)
            yield return Timed(() => replay ? TutorialTexts.BonusDoneReplay : TutorialTexts.BonusDone, null,
                OverlayMode.None, 2.5f);
    }

    /// <summary>Jefe de prueba: el jugador lo pelea de verdad, pero sin recompensas ni cambio de zona.</summary>
    private IEnumerator BossPractice()
    {
        var miniGame = FindFirstObjectByType<MiniGameController>();
        if (miniGame == null || !miniGame.CanStartPractice)
        {
            yield return Card(TutorialTexts.BossInfo, null, OverlayMode.Dim);
            yield break;
        }

        yield return Card(TutorialTexts.BossIntro, null, OverlayMode.Dim, TutorialTexts.Understood);
        if (skipRequested) yield break;

        ClearScreenObjects();

        bool finished = false;
        miniGame.StartPracticeBoss(() => finished = true);

        yield return WaitFor(() => finished, () => TutorialTexts.BossFight, null, OverlayMode.Passive);

        if (finished && !skipRequested)
            yield return Timed(() => TutorialTexts.BossDone, null, OverlayMode.None, 3.5f);
    }

    private bool pausedByTutorial;
    private float timeScaleBeforePause = 1f;

    private void SetGamePaused(bool paused)
    {
        if (paused == pausedByTutorial) return;

        if (paused)
        {
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = timeScaleBeforePause;
        }
        pausedByTutorial = paused;
    }

    private static void ClearScreenObjects()
    {
        foreach (var e in FindObjectsByType<RunnerEnemy>(FindObjectsSortMode.None))
            e.gameObject.SetActive(false);
        foreach (var b in FindObjectsByType<BonusPickup>(FindObjectsSortMode.None))
            b.gameObject.SetActive(false);
    }

    private void Show(Func<string> text, Func<RectTransform> target, OverlayMode overlay, bool withNext, string nextText)
    {
        textFn = text;
        targetFn = target;
        mode = overlay;
        lastText = null;

        nextButton.gameObject.SetActive(withNext);
        if (withNext && nextLabel != null)
            nextLabel.text = Fit(nextText);
    }

    private UpgradeUIBaseCommon FindFirstUpgradeUI()
    {
        // La mejora mas barata de oro-por-segundo que viva dentro del menu de mejoras.
        GameObject menuPanel = upgradeToggle != null ? upgradeToggle.MenuPanelObject : null;
        UpgradeUIBaseCommon best = null;

        foreach (var ui in FindObjectsByType<UpgradeUIBaseCommon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = ui.GetUpgradeData();
            if (!(data is GPSUpgradeData) || ui.BuyButton == null) continue;
            if (menuPanel != null && !ui.transform.IsChildOf(menuPanel.transform)) continue;

            if (best == null || data.baseCost < best.GetUpgradeData().baseCost)
                best = ui;
        }
        return best;
    }

    // =====================================================================
    // Enemigos controlados por el tutorial
    // =====================================================================

    private void StartFeeder(Func<bool> wanted)
    {
        feederWanted = wanted;
        if (feederRoutine == null)
            feederRoutine = StartCoroutine(FeederRoutine());
    }

    private void StopFeeder()
    {
        feederWanted = null;
        if (feederRoutine != null)
        {
            StopCoroutine(feederRoutine);
            feederRoutine = null;
        }
    }

    /// <summary>
    /// Mantiene UN enemigo a la vez en pantalla mientras haga falta. Si el jugador lo deja
    /// pasar, genera otro. Asi el oro ganado es el justo y no hay enemigos de sobra.
    /// </summary>
    private IEnumerator FeederRoutine()
    {
        var wait = new WaitForSeconds(0.8f);

        while (true)
        {
            if (spawner != null && feederWanted != null && feederWanted() && !IsAlive(feederEnemy))
            {
                yield return wait;
                if (feederWanted != null && feederWanted() && !IsAlive(feederEnemy))
                    feederEnemy = spawner.SpawnRandomEnemyImmediate();
            }
            yield return null;
        }
    }

    private static bool IsAlive(RunnerEnemy enemy) => enemy != null && enemy.gameObject.activeInHierarchy;

    // =====================================================================
    // Preparacion / limpieza del mundo
    // =====================================================================

    private void FindReferences()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        spawner = FindFirstObjectByType<EnemySpawner>();
        horse = FindFirstObjectByType<HorseSkillController>();
        upgradeToggle = FindFirstObjectByType<UpgradeMenuToggle>(FindObjectsInactive.Include);
        upgradeMenu = FindFirstObjectByType<UpgradeMenuController>(FindObjectsInactive.Include);

        gameFont = null;
        if (GoldManager.Instance != null && GoldManager.Instance.GoldText != null)
            gameFont = GoldManager.Instance.GoldText.font;
        if (gameFont == null) gameFont = TMP_Settings.defaultFontAsset;
    }

    private void PrepareWorld()
    {
        // Menu de mejoras cerrado (si se lanzo desde "Como jugar" esta abierto).
        if (upgradeToggle != null && upgradeToggle.MenuPanelObject != null)
            upgradeToggle.MenuPanelObject.SetActive(false);

        if (spawner != null)
        {
            spawner.StopSpawning();
            spawner.SuppressBonuses = true;
        }

        // Limpiar lo que haya en pantalla para que el tutorial arranque limpio.
        ClearScreenObjects();

        // Que el jefe de verdad no aparezca en medio del tutorial.
        var miniGame = FindFirstObjectByType<MiniGameController>();
        if (miniGame != null) miniGame.StopCountdown();
    }

    private void Cleanup()
    {
        if (!IsRunning && overlayRoot == null) return;

        StopFeeder();
        SetGamePaused(false);

        if (overlayRoot != null)
        {
            Destroy(overlayRoot);
            overlayRoot = null;
        }

        if (spawner != null)
        {
            spawner.SuppressBonuses = false;
            spawner.StartSpawning(); // no hace nada si ya estaba activo
        }

        // Reiniciar la cuenta regresiva del jefe para que no caiga justo despues del tutorial.
        var miniGame = FindFirstObjectByType<MiniGameController>();
        if (miniGame != null)
        {
            miniGame.CancelPractice(); // por si se salto el tutorial en plena pelea
            miniGame.StartMiniGameCountdown();
        }

        IsRunning = false;
    }

    // =====================================================================
    // Boton "Como jugar" (en la pestana de ajustes del menu de mejoras)
    // =====================================================================

    private void InjectHowToPlayButton()
    {
        var menu = FindFirstObjectByType<UpgradeMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.SettingsScroll == null || menu.SettingsScroll.content == null) return;

        var content = menu.SettingsScroll.content;
        if (content.Find("HowToPlayButton") != null) return;

        TMP_FontAsset font = null;
        var existing = content.GetComponentInChildren<TextMeshProUGUI>(true);
        if (existing != null) font = existing.font;
        if (font == null) font = TMP_Settings.defaultFontAsset;

        var button = CreateButton(content, "HowToPlayButton", Fit(TutorialTexts.HowToPlayButton, font), font, 36,
            new Vector2(440f, 100f), out _);
        var rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 40f);

        button.onClick.AddListener(() => StartTutorial(true));
    }

    // =====================================================================
    // Overlay (UI armada por codigo)
    // =====================================================================

    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color ClearColor = new Color(0f, 0f, 0f, 0f);
    private static readonly Color PanelColor = new Color(0.16f, 0.1f, 0.06f, 0.96f);
    private static readonly Color GoldColor = new Color(1f, 0.82f, 0.25f, 1f);
    private static readonly Color ButtonColor = new Color(0.85f, 0.55f, 0.15f, 1f);

    private void BuildOverlay()
    {
        overlayRoot = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        var scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root = (RectTransform)overlayRoot.transform;

        dimPieces = new Image[4];
        for (int i = 0; i < dimPieces.Length; i++)
            dimPieces[i] = CreateImage(root, "Dim" + i, DimColor, true);

        framePieces = new Image[4];
        for (int i = 0; i < framePieces.Length; i++)
            framePieces[i] = CreateImage(root, "Frame" + i, GoldColor, false);

        // Cuadro de texto
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelGo.transform.SetParent(root, false);
        panel = (RectTransform)panelGo.transform;
        panel.sizeDelta = new Vector2(940f, 0f);

        var panelImage = panelGo.GetComponent<Image>();
        panelImage.color = PanelColor;

        var outline = panelGo.GetComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(4f, -4f);

        var layout = panelGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 30, 30);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        panelGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        panelText = CreateText(panel, "Text", string.Empty, gameFont, 40, TextAlignmentOptions.Center);
        panelText.color = Color.white;
        panelText.raycastTarget = false;

        nextButton = CreateButton(panel, "Next", string.Empty, gameFont, 38, new Vector2(0f, 96f), out nextLabel);
        nextButton.onClick.AddListener(() => nextClicked = true);
        var nextLayout = nextButton.gameObject.AddComponent<LayoutElement>();
        nextLayout.minHeight = 96f;
        nextLayout.preferredHeight = 96f;

        // Boton para saltar (solo en repeticiones)
        skipButton = CreateButton(root, "Skip", Fit(TutorialTexts.Skip), gameFont, 30, new Vector2(220f, 80f), out _);
        var skipRt = (RectTransform)skipButton.transform;
        skipRt.anchorMin = skipRt.anchorMax = new Vector2(1f, 1f);
        skipRt.pivot = new Vector2(1f, 1f);
        skipRt.anchoredPosition = new Vector2(-30f, -60f);
        skipButton.onClick.AddListener(() => skipRequested = true);
        skipButton.gameObject.SetActive(replay);

        mode = OverlayMode.Dim;
        textFn = null;
    }

    private void LateUpdate()
    {
        if (overlayRoot == null) return;

        // Texto
        if (textFn != null)
        {
            string raw = textFn();
            if (raw != lastText)
            {
                lastText = raw;
                panelText.text = Fit(raw);
            }
        }

        // Objetivo -> rect en espacio del canvas del tutorial
        Rect full = root.rect;
        Rect hole = default;
        bool hasHole = false;

        if (mode != OverlayMode.None && mode != OverlayMode.Dim && targetFn != null)
        {
            var target = targetFn();
            if (target != null && target.gameObject.activeInHierarchy)
            {
                hole = ToOverlayRect(target, 14f);
                hasHole = true;
            }
        }

        LayoutDim(full, hole, hasHole);
        LayoutFrame(hole, hasHole && (mode == OverlayMode.Highlight || mode == OverlayMode.Focus));
        LayoutPanel(hole, hasHole);
    }

    private void LayoutDim(Rect full, Rect hole, bool hasHole)
    {
        Color color;
        switch (mode)
        {
            case OverlayMode.Focus:
            case OverlayMode.Dim:
                color = DimColor;
                break;
            case OverlayMode.Passive:
            case OverlayMode.Highlight:
                color = ClearColor;
                break;
            default:
                foreach (var d in dimPieces) d.gameObject.SetActive(false);
                return;
        }

        foreach (var d in dimPieces) { d.gameObject.SetActive(true); d.color = color; }

        if (mode == OverlayMode.Focus && hasHole)
        {
            SetRect(dimPieces[0], Rect.MinMaxRect(full.xMin, hole.yMax, full.xMax, full.yMax));   // arriba
            SetRect(dimPieces[1], Rect.MinMaxRect(full.xMin, full.yMin, full.xMax, hole.yMin));   // abajo
            SetRect(dimPieces[2], Rect.MinMaxRect(full.xMin, hole.yMin, hole.xMin, hole.yMax));   // izquierda
            SetRect(dimPieces[3], Rect.MinMaxRect(hole.xMax, hole.yMin, full.xMax, hole.yMax));   // derecha
        }
        else
        {
            // Pantalla completa (una sola pieza); las otras quedan en cero.
            SetRect(dimPieces[0], full);
            for (int i = 1; i < dimPieces.Length; i++) SetRect(dimPieces[i], new Rect(0f, 0f, 0f, 0f));
        }
    }

    private void LayoutFrame(Rect hole, bool visible)
    {
        foreach (var f in framePieces) f.gameObject.SetActive(visible);
        if (!visible) return;

        const float t = 8f;
        float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
        var c = GoldColor; c.a = pulse;
        foreach (var f in framePieces) f.color = c;

        SetRect(framePieces[0], Rect.MinMaxRect(hole.xMin - t, hole.yMax, hole.xMax + t, hole.yMax + t));
        SetRect(framePieces[1], Rect.MinMaxRect(hole.xMin - t, hole.yMin - t, hole.xMax + t, hole.yMin));
        SetRect(framePieces[2], Rect.MinMaxRect(hole.xMin - t, hole.yMin, hole.xMin, hole.yMax));
        SetRect(framePieces[3], Rect.MinMaxRect(hole.xMax, hole.yMin, hole.xMax + t, hole.yMax));
    }

    private void LayoutPanel(Rect hole, bool hasHole)
    {
        bool visible = textFn != null;
        panel.gameObject.SetActive(visible);
        if (!visible) return;

        // El cuadro va del lado opuesto al objetivo para no taparlo.
        bool atBottom = hasHole && hole.center.y > 0f;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, atBottom ? 0f : 1f);
        panel.pivot = new Vector2(0.5f, atBottom ? 0f : 1f);
        panel.anchoredPosition = new Vector2(0f, atBottom ? panelMargin : -panelMargin);
    }

    private Rect ToOverlayRect(RectTransform target, float padding)
    {
        target.GetWorldCorners(cornerBuffer);

        var canvas = target.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera
            : null;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, cornerBuffer[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return Rect.MinMaxRect(min.x - padding, min.y - padding, max.x + padding, max.y + padding);
    }

    private static void SetRect(Image image, Rect r)
    {
        var rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = r.center;
        rt.sizeDelta = new Vector2(Mathf.Max(0f, r.width), Mathf.Max(0f, r.height));
    }

    // ---- fabricas de UI ----

    private static Image CreateImage(Transform parent, string name, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        image.canvasRenderer.cullTransparentMesh = false; // si no, las piezas transparentes no bloquean clicks
        return image;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, TMP_FontAsset font, float size,
        TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.text = text;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, float fontSize,
        Vector2 size, out TextMeshProUGUI labelText)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = ButtonColor;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.12f, 0.02f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        labelText = CreateText(go.transform, "Label", label, font, fontSize, TextAlignmentOptions.Center);
        labelText.color = Color.white;
        labelText.raycastTarget = false;
        var labelRt = labelText.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    // =====================================================================
    // Texto: la fuente del juego es ASCII; si falta algun caracter se limpian los acentos
    // =====================================================================

    private string Fit(string text) => Fit(text, gameFont);

    private static string Fit(string text, TMP_FontAsset font)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (font != null && font.HasCharacters(text)) return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (c == '¡' || c == '¿') continue; // inverted ! and ?
            sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
