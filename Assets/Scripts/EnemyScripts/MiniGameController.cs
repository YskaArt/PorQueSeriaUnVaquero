using UnityEngine;

public class MiniGameController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TilemapScroller tilemapScroller;
    [SerializeField] private GameObject specialEnemy;
    [SerializeField] private Transform enemyTargetPosition;
    [SerializeField] private PlayerShootController playerShooter;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private HorseSkillController horseSkill;

    private void Awake()
    {
        if (horseSkill == null)
            horseSkill = FindAnyObjectByType<HorseSkillController>();
    }

    public void StartMiniGame()
    {
        if (horseSkill != null)
          //  horseSkill.SetMiniGameActive(true);

        if (tilemapScroller != null)
           // tilemapScroller.ScrollSpeed = 0f;

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

    public void OnEnemyDefeated()
    {
        if (playerShooter != null)
            playerShooter.StopShooting();

        if (playerShooter != null)
        {
            playerShooter.MoveOut(() =>
            {
                if (horseSkill != null)
                   // horseSkill.SetMiniGameActive(false);

                // AHORA: en lugar de cargar Scene - cambiar de level interno
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NextLevel();
                }
                else
                {
                    Debug.LogWarning("MiniGameController: GameManager no encontrado, no se puede cambiar de level.");
                }
            });
        }
        else
        {
            if (horseSkill != null)
               // horseSkill.SetMiniGameActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.NextLevel();
        }
    }
}
