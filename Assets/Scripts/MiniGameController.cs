using UnityEngine;
using System.Collections;

public class MiniGameController : MonoBehaviour
{
    [SerializeField] private InfiniteTilemapLoop tilemapScroller;
    [SerializeField] private GameObject specialEnemy;
    [SerializeField] private Transform enemyTargetPosition;
    [SerializeField] private PlayerShootController playerShooter;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameStartManager sceneManager;

    [Header("Escenas siguientes")]
    [SerializeField] private string[] nextScenes; // 4 escenas posibles

    public void StartMiniGame()
    {

        // Detener el tilemap
        tilemapScroller.ScrollSpeed = 0f;

        // Centrar jugador
        playerMovement.CenterToMiddleLane();

        // Activar jefe y moverlo
        specialEnemy.SetActive(true);
        specialEnemy.GetComponent<MiniBossController>().MoveTo(enemyTargetPosition.position, () =>
        {
            playerShooter.StartShooting();
        });
    }

    public void OnEnemyDefeated()
    {
        playerShooter.StopShooting();
        playerShooter.MoveOut(() =>
        {
            // Elegir una escena aleatoria
            string randomScene = nextScenes[Random.Range(0, nextScenes.Length)];
            sceneManager.EndSceneAndLoadNext(randomScene);
        });
    }
}
