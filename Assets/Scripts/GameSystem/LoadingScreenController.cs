/*
 * LoadingScreenController
 * -------------------------
 * Escena intermedia: MainMenu -> LoadingScreen -> GameScene.
 *
 * QUÉ HACE (en orden):
 * 1. Espera a que los sistemas persistentes clave ya existan (GameSaveManager,
 *    ProgressionSystems). Estos ya se crean solos vía RuntimeInitializeOnLoadMethod
 *    y GameSaveManager.Awake(), así que normalmente esto es instantáneo, pero
 *    esperamos igual por robustez (por si algún día cambia el orden de carga).
 * 2. Precalienta los pools de misiones (Resources.LoadAll) de Daily y Zona,
 *    para que cuando GameScene los necesite de verdad ya estén en memoria.
 * 3. Carga GameScene en segundo plano (SceneManager.LoadSceneAsync) SIN activarla
 *    todavía, mostrando el progreso real.
 * 4. Cuando terminó de precalentar Y la escena está lista (progress >= 0.9) Y
 *    pasó un tiempo mínimo (para que la pantalla de carga no sea un parpadeo),
 *    activa la escena.
 *
 * QUÉ NO PRECALIENTA (y por qué):
 * - GameManager, EnemySpawner, pools de enemigos, Horse, etc.: viven DENTRO de
 *   GameScene. Con allowSceneActivation=false, Unity deja la carga pausada al
 *   90% pero NO instancia ni corre Awake() de los objetos de esa escena todavía
 *   (recién pasa cuando se activa). No hay forma de precalentar esto desde acá.
 * - ShopManager: no usa Resources.LoadAll (sus datos no vienen de un pool de
 *   assets), se restaura solo vía OnSceneLoaded. No necesita warm-up.
 *
 * ESCENA:
 * - Crear "LoadingScreen.unity", agregarla a Build Settings ENTRE MainMenu y
 *   GameScene (el orden en Build Settings no importa funcionalmente para esto,
 *   ya que cargamos por nombre, pero conviene mantenerlo prolijo).
 * - Un Canvas simple con: barra de progreso (Image Filled) y/o texto "%",
 *   opcionalmente un ícono/spinner animado y tips de texto.
 * - Este script va en un GameObject de esa escena, con la barra/texto asignados.
 * - MainMenu, al tocar "Jugar", debe hacer SceneManager.LoadScene("LoadingScreen")
 *   en vez de cargar GameScene directamente.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Escena destino")]
    [SerializeField] private string targetSceneName = "GameScene";

    [Header("UI")]
    [SerializeField] private Image progressFill;       // Image (Filled) 0..1
    [SerializeField] private TextMeshProUGUI progressText; // "Cargando... 42%"
    [SerializeField] private TextMeshProUGUI tipText;      // opcional: tips rotativos

    [Header("Tips (opcional)")]
    [TextArea]
    [SerializeField] private string[] tips;

    [Header("Timing")]
    [Tooltip("Tiempo mínimo que se muestra esta pantalla, aunque todo cargue más rápido. Evita el efecto 'parpadeo'.")]
    [SerializeField] private float minimumDisplaySeconds = 1.5f;

    private void Start()
    {
        if (tips != null && tips.Length > 0 && tipText != null)
            tipText.text = tips[Random.Range(0, tips.Length)];

        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        float startTime = Time.unscaledTime;

        // --- Paso 1: esperar a los sistemas persistentes clave ---
        // En el flujo normal esto ya está listo (se crean vía Awake / RuntimeInitializeOnLoadMethod
        // antes de que el jugador llegue siquiera al menú), pero esperamos por robustez.
        float waitStart = Time.unscaledTime;
        while (GameSaveManager.Instance == null && Time.unscaledTime - waitStart < 3f)
            yield return null;

        SetProgress(0.05f, "Cargando datos...");

        // --- Paso 2: precalentar pools de misiones ---
        DailyMissionManager.Instance?.WarmUp();
        ZoneMissionManager.Instance?.WarmUp();
        yield return null; // dar un frame para que el Resources.LoadAll no se sienta como un bloque único

        SetProgress(0.15f, "Preparando misiones...");

        // --- Paso 3: cargar la escena de juego en segundo plano ---
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            // op.progress va de 0 a 0.9 mientras carga; el 10% restante queda
            // reservado para la activación real de la escena.
            float sceneProgress01 = Mathf.Clamp01(op.progress / 0.9f);
            float combined = Mathf.Lerp(0.15f, 0.9f, sceneProgress01);
            SetProgress(combined, "Cargando el Oeste...");
            yield return null;
        }

        SetProgress(0.95f, "Casi listo...");

        // --- Paso 4: respetar el tiempo mínimo de pantalla ---
        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumDisplaySeconds)
            yield return new WaitForSecondsRealtime(minimumDisplaySeconds - elapsed);

        SetProgress(1f, "¡Listo!");

        op.allowSceneActivation = true;
        // GameScene toma el control desde acá (GameManager.ApplyLevel, etc.)
    }

    private void SetProgress(float value01, string label)
    {
        if (progressFill != null) progressFill.fillAmount = value01;
        if (progressText != null) progressText.text = $"{label} {Mathf.RoundToInt(value01 * 100f)}%";
    }
}
