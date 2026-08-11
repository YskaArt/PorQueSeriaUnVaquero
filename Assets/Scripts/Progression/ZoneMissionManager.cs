/*
 * ZoneMissionManager
 * -------------------
 * Sistema de misiones de zona: un set chico de misiones que se otorgan
 * al ENTRAR a una zona (recompensa "aceptable", pensadas para completarse
 * durante esa visita, a diferencia de las Daily que son largas).
 *
 * FUNCIONAMIENTO:
 * - Carga el pool de misiones desde Resources/ZoneMissions (o el fallback del inspector).
 * - GameManager llama a OnZoneEntered() cada vez que se aplica un nivel:
 *      * isNewEntry = true  -> el jugador está entrando/cambiando de zona:
 *        se descarta el set anterior (si había algo sin reclamar, se pierde)
 *        y se sortea un set nuevo para esa zona.
 *      * isNewEntry = false -> la app se está iniciando/resumiendo (no es un
 *        cambio de zona real): si el save tiene un set guardado para ESA MISMA
 *        zona, se restaura tal cual (con su progreso); si no matchea, se genera
 *        un set nuevo.
 * - El progreso llega por los mismos hooks que las Daily:
 *      RunnerEnemy.Eliminar       -> KillEnemies
 *      MiniGameController         -> DefeatBoss
 *   (EarnGold / BuyUpgradeLevels / WatchRewardedAd también funcionan si se
 *   arma una misión de zona de ese tipo, aunque lo típico acá es Kill/Boss).
 *
 * PERSISTENCIA:
 * - zoneIndex + progreso por missionId en GameSaveData.zoneMissions.
 * - Se guarda en cada cambio relevante (igual que las Daily) para sobrevivir
 *   un cierre de la app a mitad de zona.
 *
 * ESCENA:
 * - No hace falta agregarlo a mano: ProgressionBootstrap lo instancia solo.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ZoneMissionManager : MonoBehaviour
{
    public static ZoneMissionManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int missionsPerZone = 2;
    [Tooltip("Fallback si no hay misiones en Resources/ZoneMissions")]
    [SerializeField] private MissionData[] missionPoolFallback;

    public class ActiveZoneMission
    {
        public MissionData data;
        public double progress;
        public double target;
        public bool claimed;

        public bool IsCompleted => progress >= target;
        public float Progress01 => target <= 0 ? 0f : Mathf.Clamp01((float)(progress / target));
    }

    private readonly List<ActiveZoneMission> activeMissions = new List<ActiveZoneMission>();
    private MissionData[] pool;
    private int currentZoneIndex = -1;
    private bool stateLoaded;

    /// <summary>Se dispara al entrar a una zona nueva, completar o reclamar una misión.</summary>
    public event Action OnMissionsChanged;

    public IReadOnlyList<ActiveZoneMission> ActiveMissions => activeMissions;
    public int CurrentZoneIndex => currentZoneIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>
    /// Red de seguridad: garantiza que haya misiones activas para la zona indicada.
    /// Si por algún motivo OnZoneEntered no se disparó a tiempo (orden de inicialización,
    /// timing entre escenas, etc.), esto genera el set on-demand. Es barato llamarlo seguido:
    /// si ya está todo al día, no hace nada.
    /// </summary>
    public void EnsureZone(int zoneIndex)
    {
        LoadPool();

        if (currentZoneIndex == zoneIndex && activeMissions.Count > 0)
            return; // ya está generado y corresponde a la zona actual

        Debug.LogWarning($"[ZoneMissionManager] EnsureZone: no había misiones válidas para la zona {zoneIndex}. Generando ahora (fallback).");
        GenerateForZone(zoneIndex);
    }

    // ================== ENTRADA A ZONA (llamado por GameManager) ==================

    /// <summary>
    /// Llamado por GameManager.ApplyLevel() cada vez que se aplica un nivel.
    /// </summary>
    /// <param name="zoneIndex">Índice del nivel/zona aplicado (LevelData).</param>
    /// <param name="isNewEntry">
    /// true = cambio de zona real (sortear set nuevo).
    /// false = resume de la app (restaurar si el save matchea esa zona).
    /// </param>
    public void OnZoneEntered(int zoneIndex, bool isNewEntry)
    {
        LoadPool();

        if (!isNewEntry)
        {
            if (TryRestoreState(zoneIndex))
                return;
            // No había nada guardado para esta zona: generar un set nuevo igual.
        }

        GenerateForZone(zoneIndex);
    }

    /// <summary>Suma progreso a todas las misiones activas del tipo dado.</summary>
    public void ReportProgress(MissionType type, double amount, string sourceTypeId = null)
    {
        if (amount <= 0) return;

        bool anyCompletedNow = false;
        foreach (var m in activeMissions)
        {
            if (m.claimed || m.data == null || m.data.type != type || m.IsCompleted) continue;

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

        GameSaveManager.Instance?.RequestSave();
    }

    /// <summary>Reclama la recompensa de una misión completada. Devuelve true si se otorgó.</summary>
    public bool ClaimMission(ActiveZoneMission mission)
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

    // ================== GENERACIÓN ==================

    private void LoadPool()
    {
        if (pool != null && pool.Length > 0) return;

        pool = Resources.LoadAll<MissionData>("ZoneMissions");
        if ((pool == null || pool.Length == 0) && missionPoolFallback != null && missionPoolFallback.Length > 0)
            pool = missionPoolFallback;

        if (pool == null || pool.Length == 0)
            Debug.LogWarning("[ZoneMissionManager] No hay misiones en Resources/ZoneMissions ni en el inspector.");
        else
            pool = pool.Where(m => m != null).OrderBy(m => m.missionId, StringComparer.Ordinal).ToArray();
    }

    private void GenerateForZone(int zoneIndex)
    {
        currentZoneIndex = zoneIndex;
        activeMissions.Clear();

        if (pool == null || pool.Length == 0)
        {
            OnMissionsChanged?.Invoke();
            return;
        }

        // Selección aleatoria (no determinística: cada visita a la zona es distinta)
        var indices = Enumerable.Range(0, pool.Length).OrderBy(_ => UnityEngine.Random.value).ToList();

        int count = Mathf.Min(missionsPerZone, pool.Length);
        for (int i = 0; i < count; i++)
        {
            var data = pool[indices[i]];
            activeMissions.Add(new ActiveZoneMission
            {
                data = data,
                progress = 0,
                target = data.ResolveTarget(),
                claimed = false
            });
        }

        Debug.Log($"[ZoneMissionManager] Misiones de zona {zoneIndex}: " +
                  string.Join(", ", activeMissions.Select(m => m.data.missionId)));

        OnMissionsChanged?.Invoke();
        GameSaveManager.Instance?.RequestSave();
    }

    // ================== PERSISTENCIA ==================

    /// <summary>Intenta restaurar el set guardado si pertenece a la misma zona. Devuelve true si lo logró.</summary>
    private bool TryRestoreState(int zoneIndex)
    {
        var data = GameSaveManager.Instance != null ? GameSaveManager.Instance.LoadedData : null;
        if (data == null) return false;

        var saved = data.zoneMissions;
        if (saved == null || saved.zoneIndex != zoneIndex || saved.missions == null)
            return false;

        if (pool == null || pool.Length == 0) return false;

        currentZoneIndex = saved.zoneIndex;
        activeMissions.Clear();

        foreach (var mp in saved.missions)
        {
            var missionData = pool.FirstOrDefault(m => m.missionId == mp.missionId);
            if (missionData == null) continue;

            activeMissions.Add(new ActiveZoneMission
            {
                data = missionData,
                progress = mp.progress,
                target = mp.resolvedTarget > 0 ? mp.resolvedTarget : missionData.ResolveTarget(),
                claimed = mp.claimed
            });
        }

        if (activeMissions.Count == 0) return false;

        OnMissionsChanged?.Invoke();
        return true;
    }

    /// <summary>Snapshot del estado para GameSaveManager.</summary>
    public ZoneMissionsSaveData GetSaveData()
    {
        var data = new ZoneMissionsSaveData { zoneIndex = currentZoneIndex };
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
        currentZoneIndex = -1;
        stateLoaded = false;
    }
}
