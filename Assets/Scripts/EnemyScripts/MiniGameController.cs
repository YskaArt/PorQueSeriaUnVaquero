using UnityEngine;

public class MiniGameController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InfiniteTilemapLoop tilemapScroller;
    [SerializeField] private GameObject specialEnemy;
    [SerializeField] private Transform enemyTargetPosition;
    [SerializeField] private PlayerShootController playerShooter;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameStartManager sceneManager;
    [SerializeField] private HorseSkillController horseSkill;

    [Header("Escenas siguientes (elige una al azar)")]
    [SerializeField] private string[] nextScenes;

    // Asegura referencia a HorseSkillController si no fue asignada en Inspector
    private void Awake()
    {
        if (horseSkill == null)
            horseSkill = FindAnyObjectByType<HorseSkillController>();
    }

    // Inicia el flujo del minijuego
    public void StartMiniGame()
    {
        if (horseSkill != null)
            horseSkill.SetMiniGameActive(true);

        if (tilemapScroller != null)
            tilemapScroller.ScrollSpeed = 0f;

        if (playerMovement != null)
            playerMovement.CenterToMiddleLane();

        if (specialEnemy != null && enemyTargetPosition != null)
        {
            specialEnemy.SetActive(true);
            var boss = specialEnemy.GetComponent<MiniBossController>();
            if (boss != null)
            {
                boss.MoveTo(enemyTargetPosition.position, () =>
                {
                    if (playerShooter != null)
                        playerShooter.StartShooting();
                });
            }
        }
    }

    // Llamado cuando el miniboss muere
    public void OnEnemyDefeated()
    {
        if (playerShooter != null)
            playerShooter.StopShooting();

        if (playerShooter != null)
        {
            // Animación de salida del jugador; al terminar ejecutamos transición
            playerShooter.MoveOut(() =>
            {
                if (horseSkill != null)
                    horseSkill.SetMiniGameActive(false);

                string targetScene = PickRandomNextScene();
                if (!string.IsNullOrEmpty(targetScene) && sceneManager != null)
                {
                    // ⚠️ Ya no mostramos anuncios aquí.
                    // El intersticial se muestra dentro de GameStartManager.EndSceneAndLoadNext(),
                    // después del fade a negro y antes de cargar la escena.
                    sceneManager.EndSceneAndLoadNext(targetScene);
                }
                else
                {
                    Debug.LogWarning("MiniGameController: No hay escenas en nextScenes o falta SceneManager.");
                }
            });
        }
        else
        {
            if (horseSkill != null)
                horseSkill.SetMiniGameActive(false);

            string targetScene = PickRandomNextScene();
            if (!string.IsNullOrEmpty(targetScene) && sceneManager != null)
                sceneManager.EndSceneAndLoadNext(targetScene);
        }
    }

    // Selecciona una escena aleatoria
    private string PickRandomNextScene()
    {
        if (nextScenes == null || nextScenes.Length == 0)
            return null;

        int index = Random.Range(0, nextScenes.Length);
        return nextScenes[index];
    }
}
