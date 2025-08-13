using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameStartManager : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float fadeOutDuration = 2f;

    [Header("Intro")]
    [SerializeField] private TextMeshProUGUI mapTitleText;
    [SerializeField] private float mapTitleDuration = 3f;

    [Header("Timer")]
    [SerializeField] private float timeBeforeMiniGame = 5f;
    [SerializeField] private float stopSpawnerDelay = 15f;

    [Header("Refs")]
    [SerializeField] private InfiniteTilemapLoop tilemapScroller;
    [SerializeField] private MiniGameController miniGame;

    private EnemySpawner spawner;

    private void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = Color.black;

        yield return new WaitForSeconds(0.5f);

        // Fade In
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
        mapTitleText.gameObject.SetActive(true);
        yield return new WaitForSeconds(mapTitleDuration);
        mapTitleText.gameObject.SetActive(false);

        // Esperar hasta el momento de detener el spawner
        yield return new WaitForSeconds(timeBeforeMiniGame - stopSpawnerDelay);

        spawner.StopSpawning();

        // Esperar que enemigos desaparezcan
        while (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
        {
            yield return null;
        }

        // Esperar el resto del tiempo
        yield return new WaitForSeconds(stopSpawnerDelay);

        miniGame.StartMiniGame();
    }

    public void EndSceneAndLoadNext(string nextSceneName)
    {
        StartCoroutine(EndSceneRoutine(nextSceneName));
    }

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
