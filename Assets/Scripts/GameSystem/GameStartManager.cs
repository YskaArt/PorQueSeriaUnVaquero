using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameStartManager : MonoBehaviour
{
    [Header("Fade")]
    // Imagen usada para el efecto de transición (fade in/out).
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 2f;  // Duración del fade-in al inicio.
    [SerializeField] private float fadeOutDuration = 2f; // Duración del fade-out al final.

    [Header("Intro")]
    // Texto para mostrar el título del mapa al inicio.
    [SerializeField] private TextMeshProUGUI mapTitleText;
    [SerializeField] private float mapTitleDuration = 3f; // Tiempo visible del título.

    [Header("Timer")]
    // Tiempos de control para el minijuego y el spawner.
    [SerializeField] private float timeBeforeMiniGame = 5f; // Tiempo antes de iniciar el minijuego.
    [SerializeField] private float stopSpawnerDelay = 15f;  // Tiempo antes de detener el spawner.

    [Header("Refs")]
    [SerializeField] private InfiniteTilemapLoop tilemapScroller; // Control del scroll del mapa.
    [SerializeField] private MiniGameController miniGame; // Referencia al controlador del minijuego.

    // Referencia interna al spawner de enemigos.
    private EnemySpawner spawner;

    // MÉTODO: Start()
    // Busca el spawner en la escena y arranca la secuencia inicial (intro y preparación del minijuego).
    private void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        StartCoroutine(PlayIntroSequence());
    }

    // MÉTODO: PlayIntroSequence()
    // Corrutina que gestiona la secuencia de inicio:
    // 1. Aparece pantalla negra y hace fade-in.
    // 2. Muestra el título del mapa por unos segundos.
    // 3. Espera el tiempo configurado antes de detener el spawner.
    // 4. Detiene la generación de enemigos y espera que desaparezcan.
    // 5. Activa el minijuego.
    private IEnumerator PlayIntroSequence()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = Color.black;

        yield return new WaitForSeconds(0.5f);

        // Fade-in desde negro a transparente.
        float t = 0f;
        while (t < fadeInDuration)
        {
            fadeImage.color = Color.Lerp(Color.black, Color.clear, t / fadeInDuration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = Color.clear;
        fadeImage.gameObject.SetActive(false);

        // Mostrar título del mapa.
        mapTitleText.gameObject.SetActive(true);
        yield return new WaitForSeconds(mapTitleDuration);
        mapTitleText.gameObject.SetActive(false);

        // Esperar hasta el momento indicado para detener el spawner.
        yield return new WaitForSeconds(timeBeforeMiniGame - stopSpawnerDelay);

        spawner.StopSpawning();

        // Esperar que todos los enemigos con tag "Enemy" desaparezcan.
        while (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
        {
            yield return null;
        }

        // Esperar el tiempo restante antes de iniciar el minijuego.
        yield return new WaitForSeconds(stopSpawnerDelay);

        // Inicia el minijuego.
        miniGame.StartMiniGame();
    }

    // MÉTODO: EndSceneAndLoadNext()
    // Llama a la corrutina para hacer el fade-out y cargar la siguiente escena.
    public void EndSceneAndLoadNext(string nextSceneName)
    {
        StartCoroutine(EndSceneRoutine(nextSceneName));
    }

    // MÉTODO: EndSceneRoutine()
    // Ejecuta el fade-out hacia negro y carga la escena indicada.
    private IEnumerator EndSceneRoutine(string sceneName)
    {
        float t = 0f;
        fadeImage.gameObject.SetActive(true);

        while (t < fadeOutDuration)
        {
            fadeImage.color = Color.Lerp(Color.clear, Color.black, t / fadeOutDuration);
            t += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = Color.black;
        SceneManager.LoadScene(sceneName);
    }
}
