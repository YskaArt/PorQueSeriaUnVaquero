using UnityEngine;

public class MiniGameController : MonoBehaviour
{
    [SerializeField] private InfiniteTilemapLoop tilemapScroller;
    [SerializeField] private GameObject specialEnemy;
    [SerializeField] private Transform enemyTargetPosition;
    [SerializeField] private PlayerShootController playerShooter;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameStartManager sceneManager;
    [SerializeField] private string nextSceneName = "NextScene";

    public void StartMiniGame()
    {
        // Detener el tilemap
        tilemapScroller.ScrollSpeed = 0f;

        // Posicionar jugador en el carril central
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
            sceneManager.EndSceneAndLoadNext(nextSceneName);
        });
    }
}
