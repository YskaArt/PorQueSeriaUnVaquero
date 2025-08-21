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
    [SerializeField] private HorseSkillController horseSkill; // asignaló en el Inspector

    [Header("Escenas siguientes (elige una al azar)")]
    [SerializeField] private string[] nextScenes; // poné acá tus 4 escenas

    private void Awake()
    {
        // Por si te olvidás de asignarlo en el Inspector
        if (horseSkill == null)
            horseSkill = FindAnyObjectByType< HorseSkillController>();
    }

    public void StartMiniGame()
    {
        // Notificar: bloquear skill del caballo y cortar si estuviera activa
        if (horseSkill != null)
            horseSkill.SetMiniGameActive(true);

        // Detener el scroll
        if (tilemapScroller != null)
            tilemapScroller.ScrollSpeed = 0f;

        // Centrar jugador al carril del medio
        if (playerMovement != null)
            playerMovement.CenterToMiddleLane();

        // Activar jefe y moverlo a su punto
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

    public void OnEnemyDefeated()
    {
        // Detener el loop de disparo del player y hacer su salida
        if (playerShooter != null)
            playerShooter.StopShooting();

        if (playerShooter != null)
        {
            playerShooter.MoveOut(() =>
            {
                // Avisar fin de minijuego (habilita el botón si el cooldown ya terminó)
                if (horseSkill != null)
                    horseSkill.SetMiniGameActive(false);

                // Elegir escena aleatoria y pedir al GameStartManager el fade + load
                string targetScene = PickRandomNextScene();
                if (!string.IsNullOrEmpty(targetScene) && sceneManager != null)
                {
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
            // Fallback si no hay animación de salida
            if (horseSkill != null)
                horseSkill.SetMiniGameActive(false);

            string targetScene = PickRandomNextScene();
            if (!string.IsNullOrEmpty(targetScene) && sceneManager != null)
                sceneManager.EndSceneAndLoadNext(targetScene);
        }
    }

    private string PickRandomNextScene()
    {
        if (nextScenes == null || nextScenes.Length == 0)
            return null;

        int index = Random.Range(0, nextScenes.Length);
        return nextScenes[index];
    }
}
