/*
    HorseSkillController
    --------------------
    Controla completamente la habilidad del Caballo ("Horse Skill"), que acelera el
    desplazamiento del mundo, incrementa la velocidad de los enemigos y activa un modo
    de "frenesí" en el EnemySpawner.

    FUNCIONALIDADES PRINCIPALES:
    • Activa la habilidad al presionar el botón correspondiente (si el cooldown lo permite).
    • Modifica varios sistemas globales mientras dura la habilidad:
        - Aumenta la velocidad del TilemapScroller (mundo).
        - Cambia EnemySpawner a HorseMode (spawn rápido y enemigos acelerados).
        - Acelera enemigos ya existentes en escena.
        - Activa animación "Horse" en el jugador.
    • Administra duración, cooldown y finalización de la habilidad.
    • Se desactiva automáticamente si se inicia un Minigame.
    • Permite reasignar TilemapScroller y EnemySpawner cuando GameManager cambia de Level.

    NOTAS:
    • Al terminar la habilidad, restaura todos los valores originales.
    • HorseCooldownManager gestiona el cooldown y estado de disponibilidad.
    • El método ReassignReferences es importante para niveles que regeneran sistemas.
    • La habilidad NO debe funcionar durante minijuegos (se fuerza apagado).
*/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HorseSkillController : MonoBehaviour
{
    [Header("Duraciones")]
    [SerializeField] private float skillDuration = 10f;

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
    [SerializeField] private float frenzySpawnDelay = 0.12f;

    private float originalScrollSpeed = 1f;
    private float originalMinSpawn;
    private float originalMaxSpawn;
    private float originalNormalEnemySpeed;

    private Coroutine activeSkillCoroutine;
    private bool isSkillActive = false;
    private bool isMiniGameActive = false;

    private void Start()
    {
        if (horseButton != null)
            horseButton.onClick.AddListener(ActivateHorse);

        if (cooldownImage != null)
            cooldownImage.fillAmount = 0f;

        if (tilemapScroller != null)
            originalScrollSpeed = tilemapScroller.GetScrollSpeed();

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

        if (horseButton != null)
            horseButton.interactable = false;

        isSkillActive = true;
        playerAnimator?.SetBool("Horse", true);

        if (tilemapScroller != null)
            tilemapScroller.SetScrollSpeed(originalScrollSpeed * worldSpeedMultiplier);

        if (enemySpawner != null)
            enemySpawner.ActivateHorseMode(enemySpeedMultiplier, frenzySpawnDelay);

        var enemies = FindObjectsByType<RunnerEnemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
            e?.SetFallSpeed(enemySpawner != null ? enemySpawner.HorseSkillEnemySpeed : e.GetFallSpeed() * enemySpeedMultiplier);

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

        if (tilemapScroller != null)
            tilemapScroller.SetScrollSpeed(originalScrollSpeed);

        if (enemySpawner != null)
            enemySpawner.DeactivateHorseMode();

        var enemies = FindObjectsByType<RunnerEnemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
            e?.SetFallSpeed(enemySpawner != null ? enemySpawner.NormalEnemySpeed : e.GetFallSpeed());
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

    public void ReassignReferences(TilemapScroller newScroller, EnemySpawner newSpawner)
    {
        tilemapScroller = newScroller;
        enemySpawner = newSpawner;

        if (tilemapScroller != null)
            originalScrollSpeed = tilemapScroller.GetScrollSpeed();

        if (enemySpawner != null)
        {
            originalMinSpawn = enemySpawner.MinSpawnTime;
            originalMaxSpawn = enemySpawner.MaxSpawnTime;
            originalNormalEnemySpeed = enemySpawner.NormalEnemySpeed;
        }

        Debug.Log("[HorseSkill] Referencias reasignadas tras cambio de nivel.");
    }
}
