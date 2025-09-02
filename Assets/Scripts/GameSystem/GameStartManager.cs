using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class GameStartManager : MonoBehaviour
{
    // Singleton liviano (útil para que GameSaveManager pregunte el tiempo restante)
    public static GameStartManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float fadeOutDuration = 2f;

    [Header("Intro")]
    [SerializeField] private TextMeshProUGUI mapTitleText;
    [SerializeField] private float mapTitleDuration = 3f;

    [Header("Timer")]
    // El tiempo total antes de disparar el minijuego se toma de guardado o se genera (3–5 min)
    [SerializeField] private float timeBeforeMiniGame;
    [SerializeField] private float stopSpawnerDelay = 15f;

    [Header("Refs")]
    [SerializeField] private InfiniteTilemapLoop tilemapScroller;
    [SerializeField] private MiniGameController miniGame;

    private EnemySpawner spawner;
    private float remainingTime; // contador persistente

    // ==========================
    // Awake(): configura Instance
    // ==========================
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    // ==========================
    // Start(): inicialización y arranque de la intro
    // ==========================
    private void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();

        // Cargar un tiempo guardado (si hay), o generar entre 180–300 seg (3–5 min)
        float savedTime = GameSaveManager.Instance != null ? GameSaveManager.Instance.GetSavedTimer() : 0f;
        remainingTime = savedTime > 0f ? savedTime : UnityEngine.Random.Range(180f, 300f);

        StartCoroutine(PlayIntroSequence());
    }

    // ==========================
    // Corrutina de Intro + cuenta regresiva persistente
    // ==========================
    private IEnumerator PlayIntroSequence()
    {
        // Pantalla negra al inicio
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = Color.black;
        yield return new WaitForSeconds(0.5f);

        // Fade-in
        float t = 0f;
        while (t < fadeInDuration)
        {
            fadeImage.color = Color.Lerp(Color.black, Color.clear, t / fadeInDuration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = Color.clear;
        fadeImage.gameObject.SetActive(false);

        // Título del mapa
        if (mapTitleText != null)
        {
            mapTitleText.gameObject.SetActive(true);
            yield return new WaitForSeconds(mapTitleDuration);
            mapTitleText.gameObject.SetActive(false);
        }

        // Conteo vivo del tiempo restante (se guarda cada ~2 seg para persistencia)
        float autosaveTicker = 0f;
        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            autosaveTicker += Time.deltaTime;

            if (autosaveTicker >= 2f && GameSaveManager.Instance != null)
            {
                autosaveTicker = 0f;
                GameSaveManager.Instance.SaveGame(); // guarda remainingTime y timestamp
            }

            yield return null;
        }

        // Frenar el spawner y esperar a que no queden enemigos en escena
        if (spawner != null) spawner.StopSpawning();
        while (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
            yield return null;

        // Pequeño delay de “respiro” antes del minijuego
        yield return new WaitForSeconds(stopSpawnerDelay);

        // Arrancar minijuego
        if (miniGame != null) miniGame.StartMiniGame();
    }

    // ==========================
    // EndSceneAndLoadNext(): inicia fade-out y, tras el fade, muestra intersticial y cambia de escena
    // ==========================
    public void EndSceneAndLoadNext(string nextSceneName)
    {
        StartCoroutine(EndSceneRoutine(nextSceneName));
    }

    // ==========================
    // EndSceneRoutine(): hace fade a negro, muestra intersticial, luego carga escena
    // ==========================
    private IEnumerator EndSceneRoutine(string sceneName)
    {
        // Fade-out a negro
        fadeImage.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeOutDuration)
        {
            fadeImage.color = Color.Lerp(Color.clear, Color.black, t / fadeOutDuration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = Color.black;

        // Si hay banner, ocultarlo antes del intersticial
        if (AdManager.Instance != null)
            AdManager.Instance.HideBanner();

        // Mostramos intersticial sobre pantalla negra y, al cerrarse, cargamos la escena
        bool proceeded = false;
        Action proceedOnce = () =>
        {
            if (proceeded) return;
            proceeded = true;

            // Guarda por si querés persistir "última escena" y timer
            GameSaveManager.Instance?.SaveGame();

            SceneManager.LoadScene(sceneName);
        };

        if (AdManager.Instance != null)
        {
            // Llamamos y esperamos a que el callback llegue; ponemos un “backup” por si el provider no dispara evento.
            AdManager.Instance.ShowInterstitial(proceedOnce);

            // Espera pasiva hasta que el callback ocurra o pase un tiempo de seguridad
            float safety = 5f; // segundos de espera de seguridad
            while (!proceeded && safety > 0f)
            {
                safety -= Time.unscaledDeltaTime;
                yield return null;
            }
            proceedOnce(); // si ya ocurrió, no hará nada (idempotente)
        }
        else
        {
            // Si no hay AdManager, cargamos de inmediato
            proceedOnce();
        }
    }

    // ==========================
    // GetRemainingTime(): usado por GameSaveManager para persistir el timer
    // ==========================
    public float GetRemainingTime()
    {
        return Mathf.Max(remainingTime, 0f);
    }
}
