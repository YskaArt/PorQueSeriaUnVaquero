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

        float gateStart = Time.realtimeSinceStartup;

        ApplyLevel(currentLevelIndex, isNewEntry: false);

        Debug.Log("[GameManager] ApplyLevel listo, esperando WaitUntilReadyToReveal...");

        // Esperar (en tiempo real, sin importar la pausa) a que todo lo
        // necesario esté realmente listo antes de destapar la pantalla.
        yield return WaitUntilReadyToReveal();

        // Aunque todo haya estado listo casi al instante (típico si venís del
        // menú, donde ya se precalentó JIT/assets), respetamos un mínimo de
        // tiempo visible para que la pantalla de carga no sea un parpadeo.
        float elapsed = Time.realtimeSinceStartup - gateStart;
        if (elapsed < minLoadingDisplaySeconds)
        {
            Debug.Log($"[GameManager] Todo listo en {elapsed:F2}s, esperando el mínimo de {minLoadingDisplaySeconds}s...");
            yield return new WaitForSecondsRealtime(minLoadingDisplaySeconds - elapsed);
        }

        Debug.Log($"[GameManager] WaitUntilReadyToReveal terminó. Time.timeScale antes de destapar = {Time.timeScale}");

        if (loadingOverlayRoot != null)
            loadingOverlayRoot.SetActive(false);

        Time.timeScale = 1f;

        Debug.Log("[GameManager] Arrancando FadeIn().");
        yield return FadeIn();
        yield return ShowWelcomeMessage();
        Debug.Log("[GameManager] InitializeGame() completo.");
    }

    /// <summary>
    /// Espera a que las dependencias de arranque estén listas (misiones de zona
    /// generadas, managers de progresión presentes, etc.), con un tope de tiempo
    /// para no dejar al jugador trabado si algo no llegó a inicializar.
    /// Usa tiempo REAL (WaitForSecondsRealtime / chequeos por frame) porque
    /// Time.timeScale está en 0 mientras tanto.
    /// </summary>
    private IEnumerator WaitUntilReadyToReveal()
    {
        // Un pequeño colchón fijo: deja que se asienten Awake/Start/OnEnable
        // del resto de los ~400 objetos de la escena antes de evaluar nada.
        SetLoadingProgress(0.15f, "Preparando");
        yield return new WaitForSecondsRealtime(0.15f);

        SetLoadingProgress(0.35f, "Cargando datos");

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < maxLoadingWaitSeconds)
        {
            bool zoneReady = ZoneMissionManager.Instance == null || ZoneMissionManager.Instance.ActiveMissions.Count > 0;
            bool dailyReady = DailyMissionManager.Instance != null;
            bool saveReady = GameSaveManager.Instance != null;

            if (zoneReady && dailyReady && saveReady)
            {
                SetLoadingProgress(1f, "¡Listo!");
                yield break;
            }

            // Progreso aproximado (no hay una fuente 0..1 real acá, a diferencia de
            // un AsyncOperation de carga de escena) para que la barra no quede quieta.
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - start) / maxLoadingWaitSeconds);
            SetLoadingProgress(Mathf.Lerp(0.35f, 0.9f, t), "Cargando misiones");
            yield return null; // sigue tickeando aunque timeScale sea 0
        }

        SetLoadingProgress(1f, "Listo");
        Debug.LogWarning("[GameManager] WaitUntilReadyToReveal: se llegó al tope de espera, arrancando igual.");
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