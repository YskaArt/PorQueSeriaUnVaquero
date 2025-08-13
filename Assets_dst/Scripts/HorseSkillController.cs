using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HorseSkillController : MonoBehaviour
{
    [Header("Duraciones")]
    [SerializeField] private float skillDuration = 30f;
    [SerializeField] private float cooldownDuration = 180f;

    [Header("UI")]
    [SerializeField] private Button horseButton;
    [SerializeField] private Image cooldownImage; // Image con Fill 360 radial

    [Header("Referencia al Player")]
    [SerializeField] private Animator playerAnimator;

    [Header("Enemigos")]
    [SerializeField] private EnemySpawner enemySpawner; // Referencia al spawner
    [SerializeField] private float spawnMultiplier = 0.5f;

    [Header("Velocidad Afectada")]
    [SerializeField] private InfiniteTilemapLoop tilemapMover;
    [SerializeField] private float speedMultiplier = 3f;

    private float currentCooldown = 0f;
    private bool isOnCooldown = false;
    private float originalScrollSpeed;
    private float originalMinSpawn, originalMaxSpawn;

    private void Start()
    {
        horseButton.onClick.AddListener(ActivateHorse);
        cooldownImage.fillAmount = 0f;

        // Guardamos valores originales
        originalScrollSpeed = tilemapMover.ScrollSpeed;
        originalMinSpawn = enemySpawner.MinSpawnTime;
        originalMaxSpawn = enemySpawner.MaxSpawnTime;
    }

    private void Update()
    {
        if (isOnCooldown)
        {
            currentCooldown -= Time.deltaTime;
            cooldownImage.fillAmount = currentCooldown / cooldownDuration;

            if (currentCooldown <= 0f)
            {
                isOnCooldown = false;
                horseButton.interactable = true;
                cooldownImage.fillAmount = 0f;
            }
        }
    }

    private void ActivateHorse()
    {
        if (isOnCooldown) return;

        horseButton.interactable = false;
        currentCooldown = cooldownDuration;
        isOnCooldown = true;

        // Activar animación Horse
        playerAnimator.SetBool("Horse", true);

        // Aumentar velocidad tilemap y enemigos
        tilemapMover.ScrollSpeed = originalScrollSpeed * speedMultiplier;

        // Reducir tiempo de spawn
        enemySpawner.MinSpawnTime = originalMinSpawn * spawnMultiplier;
        enemySpawner.MaxSpawnTime = originalMaxSpawn * spawnMultiplier;

        StartCoroutine(HorseDurationCoroutine());
    }

    private IEnumerator HorseDurationCoroutine()
    {
        yield return new WaitForSeconds(skillDuration);

        // Volver todo a normal
        tilemapMover.ScrollSpeed = originalScrollSpeed;
        enemySpawner.MinSpawnTime = originalMinSpawn;
        enemySpawner.MaxSpawnTime = originalMaxSpawn;

        playerAnimator.SetBool("Horse", false);
    }
}
