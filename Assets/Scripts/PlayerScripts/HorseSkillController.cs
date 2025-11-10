using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HorseSkillController : MonoBehaviour
{
    [Header("Duraciones")]
    [SerializeField] private float skillDuration = 10f; // Duración de la habilidad del caballo

    [Header("UI")]
    [SerializeField] private Button horseButton;
    [SerializeField] private Image cooldownImage;

    [Header("Player")]
    [SerializeField] private Animator playerAnimator;

    [Header("Referencias globales")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private TilemapScroller tilemapScroller;

    [Header("Multiplicadores")]
    [SerializeField] private float enemySpeedMultiplier = 2f;
    [SerializeField] private float worldSpeedMultiplier = 2f;
    [SerializeField] private float frenzySpawnDelay = 0.12f; // spawn delay durante frenzy

    // Valores originales (para restaurar)
    private float originalScrollSpeed = 1f;
    private float originalMinSpawn;
    private float originalMaxSpawn;
    private float originalNormalEnemySpeed;

    private Coroutine activeSkillCoroutine;
    private bool isSkillActive = false;
    private bool isMiniGameActive = false;

    private void Start()
    {
        if (horseButton != null) horseButton.onClick.AddListener(ActivateHorse);
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;

        if (tilemapScroller != null) originalScrollSpeed = tilemapScroller.GetScrollSpeed();

        if (enemySpawner != null)
        {
            originalMinSpawn = enemySpawner.MinSpawnTime;
            originalMaxSpawn = enemySpawner.MaxSpawnTime;
            originalNormalEnemySpeed = enemySpawner.NormalEnemySpeed;
        }
    }

    private void Update()
    {
        if (horseButton != null && HorseCooldownManager.Instance != null)
            horseButton.interactable = HorseCooldownManager.Instance.IsReady() && !isMiniGameActive;

        if (cooldownImage != null && HorseCooldownManager.Instance != null)
            cooldownImage.fillAmount = HorseCooldownManager.Instance.GetCooldownProgress();
    }

    private void ActivateHorse()
    {
        if (HorseCooldownManager.Instance == null) return;
        if (!HorseCooldownManager.Instance.IsReady() || isMiniGameActive || isSkillActive) return;

        HorseCooldownManager.Instance.StartCooldown();
        if (horseButton != null) horseButton.interactable = false;

        isSkillActive = true;
        playerAnimator?.SetBool("Horse", true);

        // Tilemap: acelerar
        if (tilemapScroller != null)
            tilemapScroller.SetScrollSpeed(originalScrollSpeed * worldSpeedMultiplier);

        // Spawner: activar frenzy mode (solo enemigos, spawn continuo)
        if (enemySpawner != null)
        {
            // Ajustar spawn times opcional (si quieres reducir min/max)
            enemySpawner.MinSpawnTime = originalMinSpawn * 0.5f;
            enemySpawner.MaxSpawnTime = originalMaxSpawn * 0.5f;

            enemySpawner.ActivateHorseMode(enemySpeedMultiplier, frenzySpawnDelay);
        }

        // Hacer que TODOS los enemigos activos aumenten su velocidad
        var enemies = FindObjectsByType<RunnerEnemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e != null)
                e.SetFallSpeed((enemySpawner != null ? enemySpawner.HorseSkillEnemySpeed : e.GetFallSpeed() * enemySpeedMultiplier));
        }

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

        playerAnimator?.SetBool("Horse", false);

        // Restaurar tilemap
        if (tilemapScroller != null)
            tilemapScroller.SetScrollSpeed(originalScrollSpeed);

        // Restaurar spawner y valores
        if (enemySpawner != null)
        {
            enemySpawner.DeactivateHorseMode();
            enemySpawner.MinSpawnTime = originalMinSpawn;
            enemySpawner.MaxSpawnTime = originalMaxSpawn;
        }

        // Restaurar velocidad de enemigos existentes
        var enemies = FindObjectsByType<RunnerEnemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e != null)
                e.SetFallSpeed(enemySpawner != null ? enemySpawner.NormalEnemySpeed : e.GetFallSpeed());
        }
    }

    public void ForceStopHorseSkill()
    {
        if (activeSkillCoroutine != null)
        {
            StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = null;
        }
        EndHorseSkill();
    }

    public void SetMiniGameActive(bool state)
    {
        isMiniGameActive = state;
        if (isMiniGameActive && isSkillActive)
            ForceStopHorseSkill();
    }
}
