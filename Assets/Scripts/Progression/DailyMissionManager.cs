/*
 * DailyMissionManager
 * -------------------
 * Sistema de misiones diarias (retención D1 del roadmap del GDD).
 *
 * FUNCIONAMIENTO:
 * - Carga el pool de misiones desde Resources/Missions (o el fallback del inspector).
 * - Cada día (fecha local) elige missionsPerDay misiones del pool de forma
 *   DETERMINÍSTICA usando la fecha como semilla: todos ven las mismas misiones
 *   ese día y no se pueden re-rollear cerrando el juego.
 * - El progreso llega vía ReportProgress() desde los hooks del juego:
 *      GoldManager.AddGold        -> EarnGold
 *      RunnerEnemy.Eliminar       -> KillEnemies
 *      UpgradeBase.LevelUp        -> BuyUpgradeLevels (evento estático)
 *      AdsManager (rewarded ok)   -> WatchRewardedAd (evento estático)
 *      MiniGameController         -> DefeatBoss
 * - Al reclamar (ClaimMission) se otorga la recompensa del MissionData y se guarda.
 *
 * PERSISTENCIA:
 * - dateKey + progreso por missionId en GameSaveData.dailyMissions.
 * - Si cambia el día (incluso con el juego abierto), se regeneran las misiones.
 *
 * ESCENA:
 * - No hace falta agregarlo a mano: ProgressionBootstrap lo instancia solo.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int missionsPerDay = 3;
    [Tooltip("Fallback si no hay misiones en Resources/Missions")]
    [SerializeField] private MissionData[] missionPoolFallback;

    public class ActiveMission
    {
        public MissionData data;
        public double progress;
        public double target;
        public bool claimed;

        public bool IsCompleted => progress >= target;
        public float Progress01 => target <= 0 ? 0f : Mathf.Clamp01((float)(progress / target));
    }

    private readonly List<ActiveMission> activeMissions = new List<ActiveMission>();
    private MissionData[] pool;
    private string currentDateKey;
    private bool stateLoaded;
    private float dateCheckTimer;

    /// <summary>Se dispara al completar/reclamar una misión o al rotar el día.</summary>
    public event Action OnMissionsChanged;

    public IReadOnlyList<ActiveMission> ActiveMissions => activeMissions;

    private static string TodayKey() => DateTime.Now.ToString("yyyyMMdd");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpgradeBase.OnAnyLevelPurchased += OnUpgradeLevelPurchased;
        AdsManager.OnRewardedAdGranted += OnRewardedAdGranted;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UpgradeBase.OnAnyLevelPurchased -= OnUpgradeLevelPurchased;
        AdsManager.OnRewardedAdGranted -= OnRewardedAdGranted;
    }

    private void Start()
    {
        LoadPool();
        TryRestoreState();
        EnsureMissionsForToday();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // El save puede aparecer recién al entrar a GameScene
        TryRestoreState();
        EnsureMissionsForToday();
    }

    private void Update()
    {
        // Chequeo barato de cambio de día (jugador que deja el juego abierto)
        dateCheckTimer += Time.unscaledDeltaTime;
        if (dateCheckTimer >= 60f)
        {
            dateCheckTimer = 0f;
            if (currentDateKey != TodayKey())
                EnsureMissionsForToday();
        }
    }

    // ================== HOOKS ==================

    private void OnUpgradeLevelPurchased(UpgradeBase upgrade) =>
        ReportProgress(MissionType.BuyUpgradeLevels, 1);

    private void OnRewardedAdGranted() =>
        ReportProgress(MissionType.WatchRewardedAd, 1);

    /// <summary>
    /// Suma progreso a todas las misiones activas del tipo dado.
    /// </summary>
    /// <param name="sourceTypeId">
    /// Id del enemigo/jefe que originó el progreso (RunnerEnemy.EnemyTypeId o MiniBossController.BossId).
    /// Opcional: si una misión tiene enemyTypeFilter vacío, cuenta igual sin importar este valor
    /// (comportamiento genérico, como antes). Si la misión tiene un filtro, solo cuenta cuando coincide.
    /// </param>
    public void ReportProgress(MissionType type, double amount, string sourceTypeId = null)
    {
        if (amount <= 0) return;

        bool anyCompletedNow = false;
        foreach (var m in activeMissions)
        {
            if (m.claimed || m.data == null || m.data.type != type || m.IsCompleted) continue;

            // Filtro por tipo de enemigo/jefe (solo aplica si la misión pide uno específico)
            string filter = m.data.enemyTypeFilter;
            if (!string.IsNullOrEmpty(filter))
            {
                if (string.IsNullOrEmpty(sourceTypeId) || !string.Equals(filter, sourceTypeId, StringComparison.Ordinal))
                    continue;
            }

            m.progress = Math.Min(m.target, m.progress + amount);
            if (m.IsCompleted)
                anyCompletedNow = true;
        }

        if (anyCompletedNow)
            OnMissionsChanged?.Invoke();
    }

    /// <summary>Reclama la recompensa de una misión completada. Devuelve true si se otorgó.</summary>
    public bool ClaimMission(ActiveMission mission)
    {
        if (mission == null || mission.claimed || !mission.IsCompleted || mission.data == null)
            return false;

        mission.claimed = true;
        mission.data.GrantReward();
        GameSaveManager.Instance?.RequestSave();
        OnMissionsChanged?.Invoke();
        return true;
    }

    /// <summary>Cantidad de misiones completadas sin reclamar (para badge en el botón).</summary>
    public int PendingClaimCount() =>
        activeMissions.Count(m => m.IsCompleted && !m.claimed);

    // ================== ROTACIÓN DIARIA ==================

    /// <summary>
    /// Precarga el pool de misiones (Resources.LoadAll) sin generar nada todavia.
    /// Pensado para llamarse desde la LoadingScreen, asi el primer uso real
    /// (cuando arranca el dia) no paga el costo de I/O en un frame critico.
    /// Idempotente: si ya esta cargado, no hace nada.
    /// </summary>
    public void WarmUp() => LoadPool();

    private void LoadPool()
    {
        if (pool != null && pool.Length > 0) return;

        pool = Resources.LoadAll<MissionData>("Missions");
        if ((pool == null || pool.Length == 0) && missionPoolFallback != null && missionPoolFallback.Length > 0)
            pool = missionPoolFallback;

        if (pool == null || pool.Length == 0)
            Debug.LogWarning("[DailyMissionManager] No hay misiones en Resources/Missions ni en el inspector.");
        else
            // Orden estable por id para que la selección con semilla sea igual en todos lados
            pool = pool.Where(m => m != null).OrderBy(m => m.missionId, StringComparer.Ordinal).ToArray();
    }

    private void EnsureMissionsForToday()
    {
        string today = TodayKey();
        if (currentDateKey == today && activeMissions.Count > 0)
            return;

        LoadPool();
        if (pool == null || pool.Length == 0) return;

        currentDateKey = today;
        activeMissions.Clear();

        // Selección determinística: la fecha es la semilla
        var rng = new System.Random(int.Parse(today));
        var indices = Enumerable.Range(0, pool.Length).OrderBy(_ => rng.Next()).ToList();

        int count = Mathf.Min(missionsPerDay, pool.Length);
        for (int i = 0; i < count; i++)
        {
            var data = pool[indices[i]];
            activeMissions.Add(new ActiveMission
            {
                data = data,
                progress = 0,
                target = data.ResolveTarget(),
                claimed = false
            });
        }

        Debug.Log($"[DailyMissionManager] Misiones del día {today}: " +
                  string.Join(", ", activeMissions.Select(m => m.data.missionId)));

        OnMissionsChanged?.Invoke();
        GameSaveManager.Instance?.RequestSave();
    }

    // ================== PERSISTENCIA ==================

    private void TryRestoreState()
    {
        if (stateLoaded) return;

        var data = GameSaveManager.Instance != null ? GameSaveManager.Instance.LoadedData : null;
        if (data == null) return;

        stateLoaded = true;

        var saved = data.dailyMissions;
        if (saved == null || saved.dateKey != TodayKey() || saved.missions == null)
            return; // no hay nada de hoy; EnsureMissionsForToday genera nuevas

        LoadPool();
        if (pool == null || pool.Length == 0) return;

        currentDateKey = saved.dateKey;
        activeMissions.Clear();

        foreach (var mp in saved.missions)
        {
            var missionData = pool.FirstOrDefault(m => m.missionId == mp.missionId);
            if (missionData == null) continue; // la misión ya no existe en el pool

            activeMissions.Add(new ActiveMission
            {
                data = missionData,
                progress = mp.progress,
                target = mp.resolvedTarget > 0 ? mp.resolvedTarget : missionData.ResolveTarget(),
                claimed = mp.claimed
            });
        }

        OnMissionsChanged?.Invoke();
    }

    /// <summary>Snapshot del estado para GameSaveManager.</summary>
    public DailyMissionsSaveData GetSaveData()
    {
        var data = new DailyMissionsSaveData { dateKey = currentDateKey };
        foreach (var m in activeMissions)
        {
            if (m.data == null) continue;
            data.missions.Add(new MissionProgressData
            {
                missionId = m.data.missionId,
                progress = m.progress,
                resolvedTarget = m.target,
                claimed = m.claimed
            });
        }
        return data;
    }

    /// <summary>Usado por el reset TOTAL del juego.</summary>
    public void ResetAll()
    {
        activeMissions.Clear();
        currentDateKey = null;
        EnsureMissionsForToday();
    }
}
