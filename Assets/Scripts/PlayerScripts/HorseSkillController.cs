using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HorseSkillController : MonoBehaviour
{
    [Header("Duraciones")]
    [SerializeField] private float skillDuration; // Duración de la habilidad del caballo

    [Header("UI")]
    [SerializeField] private Button horseButton;       // Botón que activa la habilidad
    [SerializeField] private Image cooldownImage;     // Imagen de cooldown que se llena/desocupa

    [Header("Referencia al Player")]
    [SerializeField] private Animator playerAnimator; // Animador del jugador (para animación Horse)

    [Header("Enemigos")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float spawnMultiplier = 0.5f; // Modificador del spawn de enemigos durante la habilidad

    [Header("Velocidad Afectada")]
    [SerializeField] private InfiniteTilemapLoop tilemapMover;
    [SerializeField] private float speedMultiplier = 3f; // Velocidad del scroll durante la habilidad

    private float originalScrollSpeed;
    private float originalMinSpawn, originalMaxSpawn;

    private Coroutine activeSkillCoroutine;
    private bool isSkillActive = false;
    private bool isMiniGameActive = false;

    // MÉTODO: Start()
    // Configura el botón, guarda valores originales y inicializa cooldown UI
    private void Start()
    {
        horseButton.onClick.AddListener(ActivateHorse);
        cooldownImage.fillAmount = 0f;

        originalScrollSpeed = tilemapMover.ScrollSpeed;
        originalMinSpawn = enemySpawner.MinSpawnTime;
        originalMaxSpawn = enemySpawner.MaxSpawnTime;
    }

    // MÉTODO: Update()
    // Actualiza el estado del botón y la imagen de cooldown cada frame
    private void Update()
    {
        horseButton.interactable = HorseCooldownManager.Instance.IsReady() && !isMiniGameActive;
        cooldownImage.fillAmount = HorseCooldownManager.Instance.GetCooldownProgress();
    }

    // MÉTODO: ActivateHorse()
    // Activa la habilidad del caballo:
    // - Inicia cooldown
    // - Aumenta velocidad del scroll
    // - Reduce tiempos de spawn de enemigos
    // - Activa animación del jugador
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

    // COROUTINE: HorseDurationCoroutine()
    // Espera la duración de la habilidad y luego termina el efecto
    private IEnumerator HorseDurationCoroutine()
    {
        yield return new WaitForSeconds(skillDuration);
        EndHorseSkill();
    }

    // MÉTODO: EndHorseSkill()
    // Restaura todos los valores originales y desactiva la animación
    private void EndHorseSkill()
    {
        if (!isSkillActive) return;

        isSkillActive = false;

        tilemapMover.ScrollSpeed = originalScrollSpeed;
        enemySpawner.MinSpawnTime = originalMinSpawn;
        enemySpawner.MaxSpawnTime = originalMaxSpawn;

        playerAnimator.SetBool("Horse", false);
    }

    // MÉTODO: ForceStopHorseSkill()
    // Detiene la habilidad de forma inmediata (llamado desde MiniGameController)
    public void ForceStopHorseSkill()
    {
        if (activeSkillCoroutine != null)
        {
            StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = null;
        }
        EndHorseSkill();
    }

    // MÉTODO: SetMiniGameActive(bool state)
    // Notifica al HorseSkillController que se inició o terminó un minijuego
    // Si inicia y la habilidad estaba activa, la detiene automáticamente
    public void SetMiniGameActive(bool state)
    {
        isMiniGameActive = state;

        if (isMiniGameActive && isSkillActive)
        {
            ForceStopHorseSkill();
        }
    }
}
