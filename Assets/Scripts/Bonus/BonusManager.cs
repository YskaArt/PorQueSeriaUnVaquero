using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusManager : MonoBehaviour
{
    public static BonusManager Instance { get; private set; }

    public enum BonusType
    {
        GPSDouble,
        EnemyDouble,
        Frenzy
    }

    // Eventos públicos para UI / otros sistemas
    public event Action<BonusType, float> OnBonusStarted; // (type, duration)
    public event Action<float> OnBonusTick;               // remaining seconds
    public event Action OnBonusEnded;

    [Header("Settings")]
    [Tooltip("Duración mínima del bonus (si duration <= 0 se elige aleatorio entre min/max)")]
    [SerializeField] private float minDuration = 30f;
    [Tooltip("Duración máxima del bonus (si duration <= 0 se elige aleatorio entre min/max)")]
    [SerializeField] private float maxDuration = 60f;
    [Tooltip("Multiplicador aplicado al GPS (ej. 2.0 = x2)")]
    [SerializeField] private double gpsMultiplier = 2.0;
    [Tooltip("Multiplicador aplicado al reward enemigo (ej. 2.0 = x2)")]
    [SerializeField] private double enemyMultiplier = 2.0;
    [Tooltip("Si true, los pickups aplicarán una duración aleatoria entre min/max")]
    [SerializeField] private bool randomizeDuration = true;

    [Header("Frenzy (Horde) settings")]
    [Tooltip("Duración por defecto de la horda (s) -- se ignorará randomizeDuration para Frenzy)")]
    [SerializeField] private float frenzyDuration = 15f;
    [Tooltip("Intervalo entre spawns adicionales durante la horda (s)")]
    [SerializeField] private float frenzySpawnInterval = 0.2f;
    [Tooltip("Velocidad usada para los enemigos generados por la horda (0 = usar NormalEnemySpeed)")]
    [SerializeField] private float frenzyEnemySpeedMultiplier = 1.0f;

    private bool isActive = false;
    private BonusType activeType;
    private float remaining = 0f;
    private Coroutine tickCoroutine;

    // Para manejar el GPS extra que añadimos a GoldManager (para revertirlo al finalizar)
    private double currentGpsExtra = 0.0;

    // Cache de GPS upgrades para recalcular extra GPS si cambian niveles mientras el bonus está activo
    private List<GPSUpgradeData> gpsUpgrades = new List<GPSUpgradeData>();

    private EnemySpawner enemySpawner;

    // Boosted (rewarded) support
    private bool boostedActive = false;
    private double originalGpsMultiplier = 0.0;
    private double originalEnemyMultiplier = 0.0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Intentar encontrar EnemySpawner en escena
        enemySpawner = FindAnyObjectByType<EnemySpawner>();

        // Cachear los GPS upgrades (para recálculo dinámico)
        RebuildGPSUpgradeCache();

        // Suscribir a cambios de nivel para recalcular extra GPS si es necesario
        SubscribeToGPSUpgradeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGPSUpgradeEvents();
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
    }

    private void RebuildGPSUpgradeCache()
    {
        gpsUpgrades.Clear();
        var list = Resources.FindObjectsOfTypeAll<GPSUpgradeData>();
        if (list != null)
        {
            foreach (var g in list)
            {
                if (g != null) gpsUpgrades.Add(g);
            }
        }
    }

    private void SubscribeToGPSUpgradeEvents()
    {
        foreach (var g in gpsUpgrades)
            g.OnLevelChanged += OnGPSUpgradeLevelChanged;
    }

    private void UnsubscribeFromGPSUpgradeEvents()
    {
        foreach (var g in gpsUpgrades)
            g.OnLevelChanged -= OnGPSUpgradeLevelChanged;
    }

    private void OnGPSUpgradeLevelChanged()
    {
        // Si estamos en un bonus que afecta GPS, recalculemos el extra
        if (!isActive) return;
        if (activeType != BonusType.GPSDouble) return;

        RecalculateGpsExtra();
    }

    private double CalculateTotalEffectiveGPS()
    {
        double total = 0.0;
        foreach (var g in gpsUpgrades)
            if (g != null)
                total += g.GetEffectiveGPS();
        return total;
    }

    private void RecalculateGpsExtra()
    {
        // extra = totalEffectiveGPS * (gpsMultiplier - 1)
        double totalEffective = CalculateTotalEffectiveGPS();
        double desiredExtra = totalEffective * (gpsMultiplier - 1.0);
        double delta = desiredExtra - currentGpsExtra;

        if (Math.Abs(delta) > double.Epsilon)
        {
            GoldManager.Instance?.AddGoldPerSecond(delta);
            currentGpsExtra = desiredExtra;
        }
    }

    /// <summary>
    /// Devuelve un BonusType aleatorio (equiprobable entre los tipos del enum).
    /// </summary>
    public BonusType GetRandomBonusType()
    {
        int count = Enum.GetValues(typeof(BonusType)).Length;
        return (BonusType)UnityEngine.Random.Range(0, count);
    }

    /// <summary>
    /// Activa un bonus aleatorio. Si duration <= 0 se usará el comportamiento aleatorio del manager.
    /// </summary>
    public void ActivateRandomBonus(float duration = -1f)
    {
        var randomType = GetRandomBonusType();
        ActivateBonus(randomType, duration);
    }

    /// <summary>
    /// Activa un bonus aleatorio pero con multiplicadores/duración boosteada (usado por rewarded pickups).
    /// </summary>
    public void ActivateRandomBoostedBonus(float duration, double gpsMult, double enemyMult, float frenzyBoostDuration)
    {
        // Guardar originales y aplicar boost
        originalGpsMultiplier = gpsMultiplier;
        originalEnemyMultiplier = enemyMultiplier;
        boostedActive = true;

        gpsMultiplier = gpsMult > 0.0 ? gpsMult : gpsMultiplier;
        enemyMultiplier = enemyMult > 0.0 ? enemyMult : enemyMultiplier;

        // Elegir tipo aleatorio
        var randomType = GetRandomBonusType();

        // Para Frenzy, si duration <=0 usamos frenzyBoostDuration
        float useDuration = duration;
        if (randomType == BonusType.Frenzy && useDuration <= 0f)
            useDuration = frenzyBoostDuration > 0f ? frenzyBoostDuration : frenzyDuration;

        ActivateBonus(randomType, useDuration);
    }

    /// <summary>
    /// Activar un bonus del tipo dado con duración opcional (si duration <= 0 -> usa min/max o min)
    /// </summary>
    public void ActivateBonus(BonusType type, float duration = -1f)
    {
        if (isActive)
        {
            Debug.Log($"[BonusManager] Ya hay un bonus activo ({activeType}). Ignorando nueva activación.");
            return;
        }

        if (type == BonusType.Frenzy)
        {
            // For Frenzy use fixed duration (approx) regardless of randomizeDuration
            if (duration <= 0f) duration = frenzyDuration;
        }
        else
        {
            if (duration <= 0f)
            {
                duration = randomizeDuration ? UnityEngine.Random.Range(minDuration, maxDuration) : minDuration;
            }
        }

        activeType = type;
        remaining = duration;
        isActive = true;

        // Ejecutar efectos según tipo
        switch (type)
        {
            case BonusType.GPSDouble:
                // calcular extra y añadirlo al GoldManager
                currentGpsExtra = 0.0;
                RecalculateGpsExtra();
                break;

            case BonusType.EnemyDouble:
                // En EnemyGoldManager la multiplicación se aplicará al devolver GetEnemyGoldReward().
                // Forzamos recalculo para que cachedReward se reevalúe si es necesario.
                EnemyGoldManager.Instance?.OnEnemyUpgradeChanged();
                break;

            case BonusType.Frenzy:
                // Start a controlled horde using EnemySpawner's API to avoid spawn overlap
                enemySpawner = enemySpawner ?? FindAnyObjectByType<EnemySpawner>();
                if (enemySpawner != null)
                {
                    // Start the horde on the spawner; it will pause normal patterns while active
                    float enemySpeed = enemySpawner.NormalEnemySpeed * Mathf.Max(0.01f, frenzyEnemySpeedMultiplier);
                    enemySpawner.StartBonusHorde(remaining, frenzySpawnInterval, enemySpeed);
                }
                else
                {
                    Debug.LogWarning("[BonusManager] No se encontró EnemySpawner para activar la horda.");
                }
                break;
        }

        // Notificar inicio
        OnBonusStarted?.Invoke(type, remaining);

        // Iniciar tick
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        tickCoroutine = StartCoroutine(BonusTickRoutine());
        Debug.Log($"[BonusManager] Bonus {type} activado por {remaining:F1}s. (boosted={boostedActive})");
    }

    private IEnumerator BonusTickRoutine()
    {
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            OnBonusTick?.Invoke(Mathf.Max(0f, remaining));
            yield return null;
        }

        EndActiveBonus();
    }

    private void EndActiveBonus()
    {
        if (!isActive) return;

        // Revertir efectos
        switch (activeType)
        {
            case BonusType.GPSDouble:
                // Restar la cantidad extra añadida
                if (Math.Abs(currentGpsExtra) > double.Epsilon)
                {
                    GoldManager.Instance?.AddGoldPerSecond(-currentGpsExtra);
                    currentGpsExtra = 0.0;
                }
                break;

            case BonusType.EnemyDouble:
                // Forzar recalculo (volverá a cachedReward sin multiplicador extra)
                EnemyGoldManager.Instance?.OnEnemyUpgradeChanged();
                break;

            case BonusType.Frenzy:
                // Stop enemy horde via spawner API to ensure clean stop
                if (enemySpawner == null) enemySpawner = FindAnyObjectByType<EnemySpawner>();
                if (enemySpawner != null)
                {
                    enemySpawner.StopBonusHordeImmediate();
                }
                break;
        }

        // If we had applied boosted multipliers, restore originals
        if (boostedActive)
        {
            gpsMultiplier = originalGpsMultiplier;
            enemyMultiplier = originalEnemyMultiplier;
            boostedActive = false;
        }

        isActive = false;
        activeType = default;
        remaining = 0f;

        OnBonusEnded?.Invoke();
        Debug.Log("[BonusManager] Bonus finalizado.");
    }

    // Consultas públicas
    public bool IsBonusActive() => isActive;
    public float GetRemainingTime() => remaining;

    // Usados por otros managers/UI
    public double GetEnemyRewardMultiplier() => isActive && activeType == BonusType.EnemyDouble ? enemyMultiplier : 1.0;
    public double GetGpsMultiplier() => isActive && activeType == BonusType.GPSDouble ? gpsMultiplier : 1.0;

    // Expose whether the currently active bonus was boosted (rewarded)
    public bool IsBoosted() => boostedActive;
}
