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

    private Coroutine activeSkillCoroutine;
    private bool isSkillActive = false;
    private bool isMiniGameActive = false;

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
        if (!HorseCooldownManager.Instance.IsReady() || isMiniGameActive)
        {
            horseButton.interactable = false;
        }
        else
        {
            horseButton.interactable = true;
        }

        cooldownImage.fillAmount = HorseCooldownManager.Instance.GetCooldownProgress();
    }

    private void ActivateHorse()
    {
        if (!HorseCooldownManager.Instance.IsReady() || isMiniGameActive) return;

        HorseCooldownManager.Instance.StartCooldown();
        horseButton.interactable = false;

        isSkillActive = true;

        // Animación y velocidad
        playerAnimator.SetBool("Horse", true);
        tilemapMover.ScrollSpeed = originalScrollSpeed * speedMultiplier;
        enemySpawner.MinSpawnTime = originalMinSpawn * spawnMultiplier;
        enemySpawner.MaxSpawnTime = originalMaxSpawn * spawnMultiplier;

        activeSkillCoroutine = StartCoroutine(HorseDurationCoroutine());
    }

    private IEnumerator HorseDurationCoroutine()
    {
        yield return new WaitForSeconds(skillDuration);
        EndHorseSkill();
    }

    private void EndHorseSkill()
    {
        if (!isSkillActive) return;

        isSkillActive = false;

        tilemapMover.ScrollSpeed = originalScrollSpeed;
        enemySpawner.MinSpawnTime = originalMinSpawn;
        enemySpawner.MaxSpawnTime = originalMaxSpawn;

        playerAnimator.SetBool("Horse", false);
    }

    // ✅ Llamado desde MiniGameController cuando inicia el minigame
    public void ForceStopHorseSkill()
    {
        if (activeSkillCoroutine != null)
        {
            StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = null;
        }
        EndHorseSkill();
    }

    // ✅ Llamado desde MiniGameController cuando inicia/termina
    public void SetMiniGameActive(bool state)
    {
        isMiniGameActive = state;

        if (isMiniGameActive && isSkillActive)
        {
            ForceStopHorseSkill();
        }
    }
}
