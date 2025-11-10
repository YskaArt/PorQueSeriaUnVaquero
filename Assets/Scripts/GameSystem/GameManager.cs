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

    /// <summary>
    /// Secuencia inicial: fade desde negro + aplicar primer nivel.
    /// </summary>
    private IEnumerator InitializeGame()
    {
        // Cargar primer nivel mientras pantalla negra
        ApplyLevel(currentLevelIndex);

        // Esperar un frame para asegurar que el Tilemap se haya activado
        yield return null;

        // Fade-in
        if (fadeImage != null)
        {
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

        Debug.Log($"[GameManager] Juego iniciado en nivel: {currentLevel?.levelName ?? currentLevelIndex.ToString()}");
    }

    /// <summary>
    /// Activa el level indicado por índice y desactiva los demás.
    /// También actualiza el EnemySpawner y resetea tilemap si corresponde.
    /// </summary>
    public void ApplyLevel(int index)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("[GameManager] No levels configurados.");
            return;
        }

        index = Mathf.Clamp(index, 0, levels.Count - 1);
        currentLevelIndex = index;

        // Desactivar todos los levelRoots y tilemaps
        foreach (var lvl in levels)
            lvl?.SetActive(false);

        // Activar solo el requerido
        currentLevel = levels[currentLevelIndex];
        currentLevel?.SetActive(true);

        // Configurar tilemap (resetear y ajustar velocidad)
        if (currentLevel != null && currentLevel.tilemapLoop != null)
        {
           // currentLevel.tilemapLoop.enabled = true;
           // currentLevel.tilemapLoop.ScrollSpeed = currentLevel.scrollSpeed;
            //currentLevel.tilemapLoop.ResetTilemap();

        }

        // Actualizar spawner (detener, asignar pool y reiniciar)
        var spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning();
            spawner.SetEnemyPool(currentLevel != null ? currentLevel.enemyPrefabs : null);
            spawner.RestartSpawning();
        }

        Debug.Log($"[GameManager] Nivel aplicado: {currentLevel?.levelName ?? currentLevelIndex.ToString()}");
    }

    /// <summary>
    /// Cambia al siguiente nivel (con fade). Cicla si llega al final.
    /// </summary>
    public void NextLevel()
    {
        StartCoroutine(ChangeLevelRoutine((currentLevelIndex + 1) % Mathf.Max(1, levels.Count)));
    }

    public void GotoLevel(int index)
    {
        StartCoroutine(ChangeLevelRoutine(Mathf.Clamp(index, 0, Mathf.Max(0, levels.Count - 1))));
    }

    private IEnumerator ChangeLevelRoutine(int nextIndex)
    {
        // Fade out
        if (fadeImage != null)
        {
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

        // Guardar progreso si corresponde
        GameSaveManager.Instance?.SaveGame();

        // Aplicar siguiente nivel
        ApplyLevel(nextIndex);

        // Esperar un frame para asegurar activación
        yield return null;

        // Fade in
        if (fadeImage != null)
        {
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

        yield break;
    }

    public LevelData GetCurrentLevelData() => currentLevel;
}
