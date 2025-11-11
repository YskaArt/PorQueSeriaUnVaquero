using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

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
            fadeImage.color = Color.black; // inicia en negro
        }
    }

    private void Start()
    {
        currentLevelIndex = Mathf.Clamp(startLevelIndex, 0, Mathf.Max(0, levels.Count - 1));
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        ApplyLevel(currentLevelIndex);
        yield return null;
        yield return FadeIn();
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

        // Desactiva todos los roots de nivel
        foreach (var lvl in levels)
            lvl?.SetActive(false);

        // Activa solo el requerido
        currentLevel = levels[currentLevelIndex];
        currentLevel?.SetActive(true);

        // Buscar scroll activo del nuevo nivel (si existe)
        var activeScroller = FindAnyObjectByType<TilemapScroller>();

        // Actualizar Spawner: detener, reasignar pools (no arrancar aquí necesariamente)
        var spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning();
            spawner.SetEnemyPool(currentLevel != null ? currentLevel.enemyPrefabs : null);
        }

        // Reasignar referencias dinámicas (horse, minigame, etc)
        ReassignDynamicReferences(activeScroller, spawner);

        Debug.Log($"[GameManager] Nivel aplicado: {currentLevel?.levelName ?? currentLevelIndex.ToString()}");
    }

    private void ReassignDynamicReferences(TilemapScroller newScroller, EnemySpawner newSpawner)
    {
        // Horse skill
        var horse = FindAnyObjectByType<HorseSkillController>();
        if (horse != null)
            horse.ReassignReferences(newScroller, newSpawner);

        // MiniGame
        var miniGame = FindAnyObjectByType<MiniGameController>();
        if (miniGame != null)
            miniGame.ReassignReferences(newScroller, newSpawner);
    }

    // ================== CAMBIO DE NIVEL (con intersticial) ==================
    public void GotoLevel(int index)
    {
        StartCoroutine(ChangeLevelRoutine(Mathf.Clamp(index, 0, Mathf.Max(0, levels.Count - 1))));
    }

    private IEnumerator ChangeLevelRoutine(int nextIndex)
    {
        // 1) Detener sistemas actuales para evitar elementos activos durante fade
        var currentScroller = FindAnyObjectByType<TilemapScroller>();
        var currentSpawner = FindAnyObjectByType<EnemySpawner>();
        var currentMini = FindAnyObjectByType<MiniGameController>();

        if (currentScroller != null)
            currentScroller.SetScrollSpeed(0f);

        if (currentSpawner != null)
            currentSpawner.StopSpawning();

        if (currentMini != null)
            currentMini.StopMiniGame(); // asegúrate de implementar StopMiniGame() (ver nota abajo)

        // 2) Fade Out (deja la pantalla NEGRA; no la desactives)
        yield return FadeOut();

        // 3) APLICAR nivel mientras la pantalla está negra (carga de Data Level)
        ApplyLevel(nextIndex);
        // Esperar un frame para que todo tenga chance de inicializarse
        yield return null;

        // 4) Mostrar intersticial y esperar a que termine (si AdsManager existe)
        if (AdsManager.Instance != null)
        {
            // ShowInterstitial devuelve IEnumerator (espera dentro de AdsManager)
            yield return AdsManager.Instance.ShowInterstitial();
        }
        else
        {
            Debug.LogWarning("[GameManager] AdsManager no presente - no se mostrará intersticial.");
        }

        // 5) Restaurar / arrancar sistemas del nuevo nivel
        var newScroller = FindAnyObjectByType<TilemapScroller>();
        var newSpawner = FindAnyObjectByType<EnemySpawner>();
        var newMini = FindAnyObjectByType<MiniGameController>();

        if (newScroller != null)
            newScroller.RestoreOriginalSpeed();
        else
            Debug.LogWarning("[GameManager] No hay TilemapScroller activo al restaurar.");

        if (newSpawner != null)
            newSpawner.RestartSpawning();
        else
            Debug.LogWarning("[GameManager] No hay EnemySpawner activo al restaurar.");

        // 6) Fade In (la pantalla deja de estar negra)
        yield return FadeIn();

        // 7) Reiniciar cuenta atrás del minigame
        if (newMini != null)
            newMini.StartMiniGameCountdown();
    }

    // ================== SIGUIENTE NIVEL ==================
    public void NextLevel()
    {
        // Calcula el siguiente índice (si llega al final, vuelve al inicio)
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex >= levels.Count)
            nextIndex = 0;

        Debug.Log($"[GameManager] Avanzando al siguiente nivel: {nextIndex}");

        // Usa la misma rutina de cambio con fade + intersticial
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

    // ================== UTIL ==================
    public int GetCurrentLevelIndex() => currentLevelIndex;
    public int GetTotalLevels() => levels.Count;

    public int GetRandomDifferentLevelIndex()
    {
        if (levels.Count <= 1) return currentLevelIndex;
        int newIndex;
        do
        {
            newIndex = Random.Range(0, levels.Count);
        } while (newIndex == currentLevelIndex);
        return newIndex;
    }
}
