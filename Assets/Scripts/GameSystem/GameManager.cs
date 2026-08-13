using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

///
/// GameManager
/// ------------
/// Controla la lógica general del juego:
/// - Maneja el fade-in / fade-out de la pantalla.
/// - Gestiona y activa los niveles configurados en la escena.
/// - Reasigna referencias dinámicas (TilemapScroller, EnemySpawner, MiniGameController).
/// - Controla la transición entre niveles (incluye intersticial si hay Ads).
/// - Muestra un mensaje de bienvenida al entrar a cada nivel.
/// - Mantiene una instancia persistente (Singleton).
///
/// LOADING GATE (nuevo):
/// - Al arrancar, el juego queda PAUSADO (Time.timeScale = 0) con la pantalla
///   en negro (fadeImage, que ya arrancaba así) hasta que todo lo necesario
///   para jugar esté realmente listo (nivel aplicado, misiones de zona
///   generadas, etc.). Recién ahí se despausa y se revela con el FadeIn()
///   existente. Esto reemplaza la idea de una escena de LoadingScreen aparte:
///   lo pesado vive en esta escena, así que el "loading" tiene que pasar acá.
///

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Loading Overlay (opcional, decorativo)")]
    [Tooltip("GameObject CON el título/arte y la barra de progreso, por ENCIMA del fadeImage negro. Se muestra mientras se espera y se oculta justo antes de empezar el FadeIn.")]
    [SerializeField] private GameObject loadingOverlayRoot;
    [Tooltip("Barra de progreso (Image Type = Filled). Opcional.")]
    [SerializeField] private Image loadingProgressFill;
    [Tooltip("Texto de progreso, ej. 'Cargando... 42%'. Opcional.")]
    [SerializeField] private TextMeshProUGUI loadingProgressText;
    [Tooltip("Lista de paneles a precalentar (abrir y cerrar) durante la carga, para que su primer Awake/Start no coincida con el primer toque real del jugador.")]
    [SerializeField] private UIWarmupList uiWarmupList;

    [Header("Loading Gate")]
    [Tooltip("Tiempo máximo de espera por las dependencias antes de arrancar igual, para no dejar al jugador colgado si algo falla.")]
    [SerializeField] private float maxLoadingWaitSeconds = 3f;
    [Tooltip("Tiempo MÍNIMO que se muestra la pantalla de carga, aunque todo esté listo antes. Evita que se sienta como un parpadeo cuando venís del menú (donde ya se precalentó casi todo).")]
    [SerializeField] private float minLoadingDisplaySeconds = 1.2f;

    [Header("Welcome Text")]
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private float welcomeDisplayTime = 2.5f;

    [Header("Levels (configuración de la escena)")]
    [SerializeField] private List<LevelData> levels = new List<LevelData>();
    [SerializeField] private int startLevelIndex = 0;

    private int currentLevelIndex = 0;
    private LevelData currentLevel;

    private void Awake()
    {
        Debug.Log($"[GameManager] Awake() en {gameObject.name} | Instance actual: {(Instance == null ? "null" : Instance.gameObject.name)} | Escena: {gameObject.scene.name}");

        if (Instance != null && Instance != this)
        {
            Debug.Log("[GameManager] Ya existe una Instance distinta -> me autodestruyo.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[GameManager] fadeImage asignado: {fadeImage != null} | loadingOverlayRoot asignado: {loadingOverlayRoot != null}");

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
        }

        if (welcomeText != null)
            welcomeText.gameObject.SetActive(false);

        if (loadingOverlayRoot != null)
            loadingOverlayRoot.SetActive(true);

        SetLoadingProgress(0f, "Cargando");

        // Pausar YA, en el primer Awake posible: nada debería moverse ni ser
        // clickeable mientras la pantalla sigue en negro.
        Time.timeScale = 0f;
        Debug.Log($"[GameManager] Awake() terminado. Time.timeScale = {Time.timeScale}");
    }

    private void Start()
    {
        Debug.Log($"[GameManager] Start() | Time.timeScale al entrar = {Time.timeScale}");

        // 🔥 Cargar nivel guardado
        if (GameSaveManager.Instance != null)
            currentLevelIndex = Mathf.Clamp(GameSaveManager.Instance.GetSavedLevelIndex(), 0, Mathf.Max(0, levels.Count - 1));
        else
            currentLevelIndex = Mathf.Clamp(startLevelIndex, 0, Mathf.Max(0, levels.Count - 1));

        Debug.Log($"[GameManager] Nivel inicial elegido: {currentLevelIndex}");

        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        Debug.Log("[GameManager] InitializeGame() arrancó.");

        // Precalentar los Resources.LoadAll (misiones + upgrades) ANTES de aplicar
        // el nivel, para que ese costo caiga bajo la pantalla de carga en vez de
        // en medio del gameplay. Es barato/idempotente si ya estaban cacheados.
        GameSaveManager.Instance?.WarmUp();
        DailyMissionManager.Instance?.WarmUp();
        ZoneMissionManager.Instance?.WarmUp();

        ApplyLevel(currentLevelIndex, isNewEntry: false);

        Debug.Log("[GameManager] ApplyLevel listo, precalentando paneles de UI...");

        if (uiWarmupList != null)
            yield return uiWarmupList.WarmUpAllRoutine();

        Debug.Log("[GameManager] Paneles precalentados, arrancando barra de carga...");

        yield return RunLoadingBar();

        Debug.Log($"[GameManager] Barra completa. Time.timeScale antes de destapar = {Time.timeScale}");

        if (loadingOverlayRoot != null)
            loadingOverlayRoot.SetActive(false);

        Time.timeScale = 1f;

        Debug.Log("[GameManager] Arrancando FadeIn().");
        yield return FadeIn();
        yield return ShowWelcomeMessage();
        Debug.Log("[GameManager] InitializeGame() completo.");
    }

    /// <summary>
    /// Barra de carga "creíble": SIEMPRE tarda al menos minLoadingDisplaySeconds
    /// en llegar al 100%, sin importar qué tan rápido estén listas las
    /// dependencias reales (típico si venís del menú, donde ya se precalentó
    /// JIT/assets). Si las dependencias reales tardan MÁS que ese mínimo, la
    /// barra se queda parada cerca del tope (no miente con un 100% prematuro)
    /// hasta que de verdad estén listas, con un tope de seguridad para no
    /// dejar al jugador colgado si algo falla.
    /// </summary>
    private IEnumerator RunLoadingBar()
    {
        const float fakeCap = 0.92f;      // tope visual mientras esperamos lo real
        const float finalRampSeconds = 0.2f; // remate rápido del 92% al 100%

        bool dependenciesReady = false;
        StartCoroutine(CheckDependenciesRoutine(() => dependenciesReady = true));

        float start = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - start;

            // Avance cosmético: sube parejo hacia fakeCap durante minLoadingDisplaySeconds.
            float cosmetic = minLoadingDisplaySeconds > 0f
                ? Mathf.Clamp01(elapsed / minLoadingDisplaySeconds) * fakeCap
                : fakeCap;
            SetLoadingProgress(cosmetic, "Cargando");

            bool minTimePassed = elapsed >= minLoadingDisplaySeconds;
            if (minTimePassed && dependenciesReady)
                break;

            if (elapsed >= maxLoadingWaitSeconds)
            {
                Debug.LogWarning("[GameManager] RunLoadingBar: se llegó al tope de espera, arrancando igual.");
                break;
            }

            yield return null; // sigue tickeando aunque timeScale sea 0
        }

        // Remate: del valor actual a 100%, rápido y prolijo (no instantáneo, para que no se sienta como un salto brusco).
        float fromValue = loadingProgressFill != null ? loadingProgressFill.fillAmount : fakeCap;
        float rampStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - rampStart < finalRampSeconds)
        {
            float t = (Time.realtimeSinceStartup - rampStart) / finalRampSeconds;
            SetLoadingProgress(Mathf.Lerp(fromValue, 1f, t), "Listo");
            yield return null;
        }
        SetLoadingProgress(1f, "Listo");
    }

    /// <summary>Chequea en segundo plano si las dependencias reales de arranque ya están listas.</summary>
    private IEnumerator CheckDependenciesRoutine(System.Action onReady)
    {
        while (true)
        {
            bool zoneReady = ZoneMissionManager.Instance == null || ZoneMissionManager.Instance.ActiveMissions.Count > 0;
            bool dailyReady = DailyMissionManager.Instance != null;
            bool saveReady = GameSaveManager.Instance != null;

            if (zoneReady && dailyReady && saveReady)
            {
                onReady?.Invoke();
                yield break;
            }

            yield return null;
        }
    }

    private void SetLoadingProgress(float value01, string label)
    {
        if (loadingProgressFill != null) loadingProgressFill.fillAmount = value01;
        if (loadingProgressText != null) loadingProgressText.text = $"{label}... {Mathf.RoundToInt(value01 * 100f)}%";
    }

    // ================== APLICAR NIVEL ==================
    public void ApplyLevel(int index, bool isNewEntry = true)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("[GameManager] No levels configurados.");
            return;
        }

        index = Mathf.Clamp(index, 0, levels.Count - 1);
        currentLevelIndex = index;

        foreach (var lvl in levels)
            lvl?.SetActive(false);

        currentLevel = levels[currentLevelIndex];
        currentLevel?.SetActive(true);

        var activeScroller = FindAnyObjectByType<TilemapScroller>();

        var spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning();
            spawner.SetEnemyPool(currentLevel != null ? currentLevel.enemyPrefabs : null);
        }

        ReassignDynamicReferences(activeScroller, spawner);

        // Misiones de zona: set nuevo si es un cambio real, o restaurar el guardado si es un resume.
        ZoneMissionManager.Instance?.OnZoneEntered(currentLevelIndex, isNewEntry);

        Debug.Log($"[GameManager] Nivel aplicado: {currentLevel?.levelName ?? currentLevelIndex.ToString()}");

        // Guardar inmediatamente el cambio de nivel aplicado
        if (GameSaveManager.Instance != null)
            GameSaveManager.Instance.SaveGame();
    }

    private void ReassignDynamicReferences(TilemapScroller newScroller, EnemySpawner newSpawner)
    {
        var horse = FindAnyObjectByType<HorseSkillController>();
        if (horse != null)
            horse.ReassignReferences(newScroller, newSpawner);

        var miniGame = FindAnyObjectByType<MiniGameController>();
        if (miniGame != null)
            miniGame.ReassignReferences(newScroller, newSpawner);
    }

    // ================== CAMBIO DE NIVEL ==================
    public void GotoLevel(int index)
    {
        StartCoroutine(ChangeLevelRoutine(Mathf.Clamp(index, 0, Mathf.Max(0, levels.Count - 1))));
    }

    private IEnumerator ChangeLevelRoutine(int nextIndex)
    {
        var currentScroller = FindAnyObjectByType<TilemapScroller>();
        var currentSpawner = FindAnyObjectByType<EnemySpawner>();
        var currentMini = FindAnyObjectByType<MiniGameController>();

        if (currentScroller != null) currentScroller.SetScrollSpeed(0f);
        if (currentSpawner != null) currentSpawner.StopSpawning();
        if (currentMini != null) currentMini.StopMiniGame();

        yield return FadeOut();

        ApplyLevel(nextIndex);
        yield return null;

        if (AdsManager.Instance != null)
            yield return AdsManager.Instance.ShowInterstitial();
        else
            Debug.LogWarning("[GameManager] AdsManager no presente - no se mostrará intersticial.");

        var newScroller = FindAnyObjectByType<TilemapScroller>();
        var newSpawner = FindAnyObjectByType<EnemySpawner>();
        var newMini = FindAnyObjectByType<MiniGameController>();

        if (newScroller != null) newScroller.RestoreOriginalSpeed();
        if (newSpawner != null) newSpawner.RestartSpawning();

        yield return FadeIn();
        yield return ShowWelcomeMessage();

        if (newMini != null)
            newMini.StartMiniGameCountdown();
    }

    // ================== SIGUIENTE NIVEL ==================
    public void NextLevel()
    {
        int nextIndex = GetRandomDifferentLevelIndex();
        GotoLevel(nextIndex);
    }

    // ================== FADE ==================
    public IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        float t = 0f;

        while (t < fadeDuration)
        {
            fadeImage.color = Color.Lerp(Color.clear, Color.black, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = Color.black;
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        float t = 0f;

        while (t < fadeDuration)
        {
            fadeImage.color = Color.Lerp(Color.black, Color.clear, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = Color.clear;
        fadeImage.gameObject.SetActive(false);
    }

    // ================== WELCOME MESSAGE ==================
    private IEnumerator ShowWelcomeMessage()
    {
        if (welcomeText == null || currentLevel == null)
            yield break;

        welcomeText.text = $"Welcome to {currentLevel.levelName}";
        welcomeText.gameObject.SetActive(true);
        welcomeText.alpha = 1f;

        yield return new WaitForSeconds(welcomeDisplayTime);

        float t = 0f;
        while (t < 1f)
        {
            welcomeText.alpha = Mathf.Lerp(1f, 0f, t);
            t += Time.deltaTime;
            yield return null;
        }

        welcomeText.alpha = 0f;
        welcomeText.gameObject.SetActive(false);
    }

    // ================== UTIL ==================
    public int GetCurrentLevelIndex() => currentLevelIndex;
    public int GetTotalLevels() => levels.Count;

    public int GetRandomDifferentLevelIndex()
    {
        if (levels.Count <= 1) return currentLevelIndex;
        int newIndex;
        do { newIndex = Random.Range(0, levels.Count); }
        while (newIndex == currentLevelIndex);
        return newIndex;
    }

    public void GotoRandomLevel()
    {
        int nextIndex = GetRandomDifferentLevelIndex();
        GotoLevel(nextIndex);
    }
}