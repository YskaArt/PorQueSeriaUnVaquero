/*
    MiniGameController
    ------------------
    Controla toda la lógica del Minigame que ocurre cada cierto tiempo durante la partida.
    Sus responsabilidades incluyen:

    • Temporizador del minigame:
        - Espera un tiempo configurado.
        - 10 segundos antes, desactiva el spawner.
        - Al finalizar la cuenta, inicia el minijuego.

    • Inicio del Minigame:
        - Pausa el scroll del Tilemap.
        - Detiene la habilidad del caballo.
        - Bloquea el movimiento del jugador y lo centra en el carril del medio.
        - Detiene el spawner y el disparo del jugador.
        - Instancia un MiniBoss, lo mueve a su posición de pelea y luego habilita el disparo.

    • Manejo de la muerte del MiniBoss:
        - Evita doble ejecución usando un flag.
        - Limpia callbacks, elimina el boss y detiene el disparo.
        - Dispara la rutina que finaliza el minigame y pide al GameManager avanzar de nivel.

    • Fin del Minigame:
        - Libera bloqueo del jugador.
        - Restaura velocidades y sistemas del Tilemap.
        - Reinicia el spawner.
        - Señaliza al HorseSkill que el minigame terminó.

    • Soporta reasignación de referencias desde el GameManager.
*/

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
        if (tilemapScroller == null) tilemapScroller = FindAnyObjectByType<TilemapScroller>();
        if (horseSkill == null) horseSkill = FindAnyObjectByType<HorseSkillController>();
        if (enemySpawner == null) enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (playerShooter == null) playerShooter = FindAnyObjectByType<PlayerShootController>();
        if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovement>();

        StartMiniGameCountdown();
    }

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

            if (remaining <= preDisableTime && enemySpawner != null && enemySpawner.IsSpawning)
            {
                Debug.Log("[MiniGame] Desactivando spawner previo al minigame...");
                enemySpawner.StopSpawning();
            }

            yield return null;
        }

        while (BonusManager.Instance != null && BonusManager.Instance.IsBonusActive())
        {
            // opcional: podrías mostrar una notificación en pantalla aquí
            yield return null;
        }

        StartMiniGame();
    }

    public void StartMiniGame()
    {
        if (isMiniGameActive) return;
        isMiniGameActive = true;
        hasHandledBossDeath = false;

        Debug.Log("[MiniGame] Iniciando minigame...");

        if (tilemapScroller != null)
        {
            tilemapScroller.SaveOriginalSpeed();
            tilemapScroller.SetScrollSpeed(0f);
        }
        else Debug.LogWarning("[MiniGame] No hay TilemapScroller activo.");

        horseSkill?.ForceStopHorseSkill();
        horseSkill?.SetMiniGameActive(true);

        enemySpawner?.StopSpawning();

        playerMovement?.SetLockedForMiniGame(true);
        playerMovement?.CenterToMiddleLane();

        if (bossPrefabs != null && bossPrefabs.Count > 0 && bossStartPosition != null)
        {
            int bossIndex = Random.Range(0, bossPrefabs.Count);
            activeBoss = Instantiate(bossPrefabs[bossIndex], bossStartPosition.position, Quaternion.identity);

            activeBossController = activeBoss.GetComponent<MiniBossController>();
            if (activeBossController != null)
            {
                activeBossController.AssignDeathCallback(OnMiniBossDefeated);

                // ASIGNAR target al shooter (evita buscarlo en cada hit)
                playerShooter?.SetTarget(activeBossController);

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

    public void OnMiniBossDefeated()
    {
        if (hasHandledBossDeath) return;
        hasHandledBossDeath = true;

        Debug.Log("[MiniGame] Boss derrotado. Iniciando cierre y cambio de nivel...");

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

        // Limpiar referencia target del shooter
        playerShooter?.StopShooting();
        playerShooter?.SetTarget(null);

        StartCoroutine(HandleEndOfMinigame());
    }

    private IEnumerator HandleEndOfMinigame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NextLevel();
        }
        else
        {
            Debug.LogWarning("[MiniGame] GameManager no encontrado. No se puede cambiar de nivel automáticamente.");
        }

        yield break;
    }

    public void StopMiniGame()
    {
        if (!isMiniGameActive) return;
        isMiniGameActive = false;

        if (minigameTimerCoroutine != null) StopCoroutine(minigameTimerCoroutine);
        minigameTimerCoroutine = null;

        playerShooter?.StopShooting();
        playerMovement?.SetLockedForMiniGame(false);

        if (tilemapScroller != null) tilemapScroller.RestoreOriginalSpeed();

        enemySpawner?.RestartSpawning();

        horseSkill?.SetMiniGameActive(false);

        // limpiar target del shooter por seguridad
        playerShooter?.SetTarget(null);

        hasHandledBossDeath = false;
    }

    public void ReassignReferences(TilemapScroller newScroller, EnemySpawner newSpawner)
    {
        tilemapScroller = newScroller;
        enemySpawner = newSpawner;
    }
}

