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

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
        }

        if (welcomeText != null)
            welcomeText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // 🔥 Cargar nivel guardado
        if (GameSaveManager.Instance != null)
            currentLevelIndex = Mathf.Clamp(GameSaveManager.Instance.GetSavedLevelIndex(), 0, Mathf.Max(0, levels.Count - 1));
        else
            currentLevelIndex = Mathf.Clamp(startLevelIndex, 0, Mathf.Max(0, levels.Count - 1));

        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        ApplyLevel(currentLevelIndex);
        yield return null;
        yield return FadeIn();
        yield return ShowWelcomeMessage();
    }

    // ================== APLICAR NIVEL ==================
    public void ApplyLevel(int index)
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
