using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HorseSkillController : MonoBehaviour
{
    [Header("Duraciones")]
    [SerializeField] private float skillDuration = 30f;

    [Header("UI")]
    [SerializeField] private Button horseButton;
    [SerializeField] private Image cooldownImage;

    [Header("Referencia al Player")]
    [SerializeField] private Animator playerAnimator;

    [Header("Enemigos")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float spawnMultiplier = 0.5f;

    [Header("Velocidad Afectada")]
    [SerializeField] private InfiniteTilemapLoop tilemapMover;
    [SerializeField] private float speedMultiplier = 3f;

    private float originalScrollSpeed;
    private float originalMinSpawn, originalMaxSpawn;

    private void Start()
    {
        horseButton.onClick.AddListener(ActivateHorse);
        cooldownImage.fillAmount = 0f;

        originalScrollSpeed = tilemapMover.ScrollSpeed;
        originalMinSpawn = enemySpawner.MinSpawnTime;
        originalMaxSpawn = enemySpawner.MaxSpawnTime;
    }

    private void Update()
    {
        // Actualizar UI del cooldown
        if (!HorseCooldownManager.Instance.IsReady())
        {
            horseButton.interactable = false;
            cooldownImage.fillAmount = HorseCooldownManager.Instance.GetCooldownProgress();
        }
        else
        {
            horseButton.interactable = true;
            cooldownImage.fillAmount = 0f;
        }
    }

    private void ActivateHorse()
    {
        if (!HorseCooldownManager.Instance.IsReady()) return;

        HorseCooldownManager.Instance.StartCooldown();
        horseButton.interactable = false;

        // Activar animación Horse
        playerAnimator.SetBool("Horse", true);

        // Aumentar velocidad tilemap y reducir spawn
        tilemapMover.ScrollSpeed = originalScrollSpeed * speedMultiplier;
        enemySpawner.MinSpawnTime = originalMinSpawn * spawnMultiplier;
        enemySpawner.MaxSpawnTime = originalMaxSpawn * spawnMultiplier;

        StartCoroutine(HorseDurationCoroutine());
    }

    private IEnumerator HorseDurationCoroutine()
    {
        yield return new WaitForSeconds(skillDuration);

        // Volver a valores originales
        tilemapMover.ScrollSpeed = originalScrollSpeed;
        enemySpawner.MinSpawnTime = originalMinSpawn;
        enemySpawner.MaxSpawnTime = originalMaxSpawn;

        playerAnimator.SetBool("Horse", false);
    }
}
