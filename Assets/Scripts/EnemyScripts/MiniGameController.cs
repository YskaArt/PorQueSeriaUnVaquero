using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGameController : MonoBehaviour
{
    [Header("Referencias principales (pueden reasignarse)")]
    [SerializeField] private TilemapScroller tilemapScroller;
    [SerializeField] private HorseSkillController horseSkill;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerShootController playerShooter;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Boss y posiciones")]
    [SerializeField] private Transform bossStartPosition;
    [SerializeField] private Transform bossTargetPosition;
    [SerializeField] private List<GameObject> bossPrefabs; // prefabs que contienen MiniBossController
    private GameObject activeBoss;
    private MiniBossController activeBossController;

    [Header("Minigame config")]
    [SerializeField] private float minigameDelay = 240f; // 4 minutos
    [SerializeField] private float preDisableTime = 10f; // 10s antes del minigame se apaga el spawner

    private Coroutine minigameTimerCoroutine;
    private bool isMiniGameActive = false;

    // --- Flags para evitar ejecuciones dobles ---
    private bool hasHandledBossDeath = false;

    private void Start()
    {
        // buscar referencias si faltan
        if (tilemapScroller == null) tilemapScroller = FindAnyObjectByType<TilemapScroller>();
        if (horseSkill == null) horseSkill = FindAnyObjectByType<HorseSkillController>();
        if (enemySpawner == null) enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (playerShooter == null) playerShooter = FindAnyObjectByType<PlayerShootController>();
        if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovement>();

        StartMiniGameCountdown();
    }

    // ---------------------------------------------------------------------
    // Temporizador / control
    // ---------------------------------------------------------------------
    public void StartMiniGameCountdown()
    {
        if (minigameTimerCoroutine != null) StopCoroutine(minigameTimerCoroutine);
        minigameTimerCoroutine = StartCoroutine(MinigameCountdown());
    }

    private IEnumerator MinigameCountdown()
    {
        float remaining = minigameDelay;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;

            // 10 segundos antes del minigame, desactivar spawner
            if (remaining <= preDisableTime && enemySpawner != null && enemySpawner.IsSpawning)
            {
                Debug.Log("[MiniGame] Desactivando spawner previo al minigame...");
                enemySpawner.StopSpawning();
            }

            yield return null;
        }

        StartMiniGame();
    }

    // ---------------------------------------------------------------------
    // INICIO DEL MINIGAME
    // ---------------------------------------------------------------------
    public void StartMiniGame()
    {
        if (isMiniGameActive) return;
        isMiniGameActive = true;
        hasHandledBossDeath = false;

        Debug.Log("[MiniGame] Iniciando minigame...");

        // Pausar scroll
        if (tilemapScroller != null)
        {
            tilemapScroller.SaveOriginalSpeed();
            tilemapScroller.SetScrollSpeed(0f);
        }
        else Debug.LogWarning("[MiniGame] No hay TilemapScroller activo.");

        // Forzar stop de horse skill
        horseSkill?.ForceStopHorseSkill();
        horseSkill?.SetMiniGameActive(true);

        // Detener spawner
        enemySpawner?.StopSpawning();

        // Centrar jugador
        playerMovement?.CenterToMiddleLane();

        // Instanciar boss (desde la lista) y asignar callback
        if (bossPrefabs != null && bossPrefabs.Count > 0 && bossStartPosition != null)
        {
            int bossIndex = Random.Range(0, bossPrefabs.Count);
            activeBoss = Instantiate(bossPrefabs[bossIndex], bossStartPosition.position, Quaternion.identity);

            activeBossController = activeBoss.GetComponent<MiniBossController>();
            if (activeBossController != null)
            {
                // Reemplaza cualquier callback anterior
                activeBossController.AssignDeathCallback(OnMiniBossDefeated);

                // Mover al boss hacia la posición objetivo; cuando llegue, comenzar la pelea
                activeBossController.MoveTo(bossTargetPosition.position, () =>
                {
                    playerShooter?.StartShooting();
                });
            }
            else
            {
                Debug.LogWarning("[MiniGame] El prefab del boss no contiene MiniBossController.");
            }
        }
        else
        {
            Debug.LogWarning("[MiniGame] No hay bossPrefabs o bossStartPosition asignada.");
        }
    }

    // ---------------------------------------------------------------------
    // HANDLER de muerte del miniboss (seguro para ejecuciones múltiples)
    // ---------------------------------------------------------------------
    public void OnMiniBossDefeated()
    {
        if (hasHandledBossDeath) return; // evita ejecuciones duplicadas
        hasHandledBossDeath = true;

        Debug.Log("[MiniGame] Boss derrotado. Iniciando cierre y cambio de nivel...");

        // Detener shooter por seguridad
        playerShooter?.StopShooting();

        // Lanzar la secuencia de fin de minigame (coroutine)
        StartCoroutine(HandleEndOfMinigame());
    }

    // ---------------------------------------------------------------------
    // Final del minigame: restauración y cambio de nivel
    // ---------------------------------------------------------------------
    private IEnumerator HandleEndOfMinigame()
    {
        // Dejar el game manager manejar fade, carga y cambio
        // Llamamos a GameManager para que haga FadeOut -> ApplyLevel -> ShowAds -> FadeIn (ya implementado ahí)
        if (GameManager.Instance != null)
        {
            // GameManager se encargará de detener/arrancar sistemas. Llamamos a su GotoLevel ó NextLevel
            GameManager.Instance.NextLevel();
        }
        else
        {
            Debug.LogWarning("[MiniGame] GameManager no encontrado. No se puede cambiar de nivel automáticamente.");
        }

        yield break;
    }

    // ---------------------------------------------------------------------
    // STOP MINIGAME (llamable desde GameManager si se necesita limpiar rápido)
    // ---------------------------------------------------------------------
    public void StopMiniGame()
    {
        if (!isMiniGameActive) return;
        isMiniGameActive = false;

        // Detener timer
        if (minigameTimerCoroutine != null) StopCoroutine(minigameTimerCoroutine);
        minigameTimerCoroutine = null;

        // Parar shooter
        playerShooter?.StopShooting();

        // Restaurar tilemap si estaba pausado
        if (tilemapScroller != null) tilemapScroller.RestoreOriginalSpeed();

        // Reactivar spawner
        enemySpawner?.RestartSpawning();

        // Forzar parar la habilidad del caballo
        horseSkill?.SetMiniGameActive(false);

        // Limpiar boss existente si lo hay
        if (activeBossController != null)
        {
            activeBossController.ClearDeathCallback();
        }
        if (activeBoss != null)
        {
            Destroy(activeBoss);
            activeBoss = null;
            activeBossController = null;
        }

        hasHandledBossDeath = false;
    }

    // Reassignment helper requested por GameManager
    public void ReassignReferences(TilemapScroller newScroller, EnemySpawner newSpawner)
    {
        tilemapScroller = newScroller;
        enemySpawner = newSpawner;
    }
}
