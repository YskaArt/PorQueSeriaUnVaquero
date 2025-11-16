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
    [SerializeField] private List<GameObject> bossPrefabs;

    private GameObject activeBoss;
    private MiniBossController activeBossController;

    [Header("Minigame config")]
    [SerializeField] private float minigameDelay = 240f;
    [SerializeField] private float preDisableTime = 10f;

    private Coroutine minigameTimerCoroutine;
    private bool isMiniGameActive = false;
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

    // Temporizador principal del minigame
    public void StartMiniGameCountdown()
    {
        if (minigameTimerCoroutine != null)
            StopCoroutine(minigameTimerCoroutine);

        minigameTimerCoroutine = StartCoroutine(MinigameCountdown());
    }

    private IEnumerator MinigameCountdown()
    {
        float remaining = minigameDelay;

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;

            if (remaining <= preDisableTime &&
                enemySpawner != null &&
                enemySpawner.IsSpawning)
            {
                Debug.Log("[MiniGame] Desactivando spawner previo al minigame...");
                enemySpawner.StopSpawning();
            }

            yield return null;
        }

        StartMiniGame();
    }

    // Inicio del minigame
    public void StartMiniGame()
    {
        if (isMiniGameActive) return;
        isMiniGameActive = true;
        hasHandledBossDeath = false;

        Debug.Log("[MiniGame] Iniciando minigame...");

        tilemapScroller?.SaveOriginalSpeed();
        tilemapScroller?.SetScrollSpeed(0f);

        horseSkill?.ForceStopHorseSkill();
        horseSkill?.SetMiniGameActive(true);

        enemySpawner?.StopSpawning();

        playerMovement?.SetLockedForMiniGame(true);
        playerMovement?.CenterToMiddleLane();

        if (bossPrefabs != null && bossPrefabs.Count > 0 && bossStartPosition != null)
        {
            int bossIndex = Random.Range(0, bossPrefabs.Count);
            activeBoss = Instantiate(bossPrefabs[bossIndex],
                                     bossStartPosition.position,
                                     Quaternion.identity);

            activeBossController = activeBoss.GetComponent<MiniBossController>();

            if (activeBossController != null)
            {
                activeBossController.AssignDeathCallback(OnMiniBossDefeated);

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

    // Handler de muerte del miniboss
    public void OnMiniBossDefeated()
    {
        if (hasHandledBossDeath) return;
        hasHandledBossDeath = true;

        Debug.Log("[MiniGame] Boss derrotado. Cerrando minigame...");

        activeBossController?.ClearDeathCallback();

        if (activeBoss != null)
        {
            Destroy(activeBoss);
            activeBoss = null;
            activeBossController = null;
        }

        playerShooter?.StopShooting();

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
            Debug.LogWarning("[MiniGame] GameManager no encontrado. No se puede cambiar de nivel.");
        }

        yield break;
    }

    // Llamado para cancelar un minigame en curso o limpiar el estado
    public void StopMiniGame()
    {
        if (!isMiniGameActive) return;
        isMiniGameActive = false;

        if (minigameTimerCoroutine != null)
            StopCoroutine(minigameTimerCoroutine);

        minigameTimerCoroutine = null;

        playerShooter?.StopShooting();
        playerMovement?.SetLockedForMiniGame(false);

        tilemapScroller?.RestoreOriginalSpeed();
        enemySpawner?.RestartSpawning();

        horseSkill?.SetMiniGameActive(false);

        hasHandledBossDeath = false;
    }

    // Reasignación de referencias desde GameManager
    public void ReassignReferences(TilemapScroller newScroller, EnemySpawner newSpawner)
    {
        tilemapScroller = newScroller;
        enemySpawner = newSpawner;
    }
}
