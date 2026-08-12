/*
 * MissionData (ScriptableObject)
 * ------------------------------
 * Define una misión diaria: qué hay que hacer, cuánto, y qué recompensa da.
 *
 * USO:
 * - Crear assets con "Create > Idle > Missions > Mission" y guardarlos en
 *   Assets/Resources/Missions/ para que DailyMissionManager los encuentre.
 * - missionId debe ser único y estable (se usa en el save para restaurar progreso).
 *
 * TARGETS:
 * - targetAmount: objetivo fijo (enemigos, niveles, ads, jefes...).
 * - targetMinutesOfGPS (> 0, solo para EarnGold): el objetivo se resuelve al asignar
 *   la misión como "X minutos de tu GPS actual", así escala con el progreso del jugador.
 *   Si el GPS es 0 (jugador nuevo), se usa targetAmount como fallback.
 *
 * RECOMPENSAS (se pueden combinar):
 * - rewardFlatGold: oro fijo.
 * - rewardMinutesOfGPS: oro equivalente a X minutos del GPS actual al reclamar.
 * - rewardMasteryPoints: puntos de maestría directos.
 */

using System;
using UnityEngine;

public enum MissionType
{
    KillEnemies,        // eliminar N enemigos
    EarnGold,           // ganar N de oro (o N minutos de GPS)
    BuyUpgradeLevels,   // comprar N niveles de cualquier upgrade
    WatchRewardedAd,    // ver N rewarded ads completos
    DefeatBoss          // derrotar N jefes de zona
}

[CreateAssetMenu(fileName = "NewMission", menuName = "Idle/Missions/Mission")]
public class MissionData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Id único y estable; se usa en el save")]
    public string missionId;
    [TextArea]
    public string description;

    [Header("Objetivo")]
    public MissionType type;
    [Tooltip("Objetivo fijo (y fallback si targetMinutesOfGPS no aplica)")]
    public double targetAmount = 10;
    [Tooltip("Solo EarnGold: objetivo = X minutos del GPS al asignar (0 = usar targetAmount fijo)")]
    public double targetMinutesOfGPS = 0;

    [Header("Filtro por tipo (solo KillEnemies / DefeatBoss)")]
    [Tooltip("Debe coincidir EXACTO con el enemyTypeId del RunnerEnemy o el bossId del MiniBossController. " +
             "Vacío = cuenta cualquier enemigo/jefe (comportamiento genérico).")]
    public string enemyTypeFilter = "";

    [Header("Recompensa")]
    public double rewardFlatGold = 0;
    [Tooltip("Oro equivalente a X minutos del GPS actual al reclamar")]
    public double rewardMinutesOfGPS = 0;
    public int rewardMasteryPoints = 0;

    /// <summary>Resuelve el objetivo real de la misión al momento de asignarla.</summary>
    public double ResolveTarget()
    {
        if (type == MissionType.EarnGold && targetMinutesOfGPS > 0)
        {
            double gps = GoldManager.Instance != null ? GoldManager.Instance.CurrentGoldPerSecond : 0;
            if (gps > 0)
                return targetMinutesOfGPS * 60.0 * gps;
        }
        return targetAmount;
    }

    /// <summary>Texto corto de la recompensa para la UI (ej: "1.2K Gold + 1 Mastery").</summary>
    /// <summary>
    /// GPS mínimo asumido para calcular recompensas "en minutos de GPS" cuando el
    /// jugador todavía no tiene ningún GPS real (0). Sin esto, las misiones que
    /// solo dan rewardMinutesOfGPS (sin rewardFlatGold) otorgarían 0 oro y se
    /// verían como "-" justo a los jugadores nuevos, que son los que más lo necesitan.
    /// Ajustable si no coincide con el ritmo real de la economía temprana del juego.
    /// </summary>
    private const double MinimumAssumedGPS = 1.0;

    public string BuildRewardLabel()
    {
        double gold = rewardFlatGold;
        double gps = GoldManager.Instance != null ? GoldManager.Instance.CurrentGoldPerSecond : 0;
        if (rewardMinutesOfGPS > 0)
            gold += rewardMinutesOfGPS * 60.0 * Math.Max(gps, MinimumAssumedGPS);

        string label = "";
        if (gold > 0)
            label += GoldManager.FormatNumber(gold) + " Gold";

        if (rewardMasteryPoints > 0)
        {
            if (label.Length > 0) label += " + ";
            label += rewardMasteryPoints + " Mastery";
        }

        return label.Length > 0 ? label : "-";
    }

    /// <summary>Otorga la recompensa (llamado por DailyMissionManager al reclamar).</summary>
    public void GrantReward()
    {
        double gold = rewardFlatGold;
        double gps = GoldManager.Instance != null ? GoldManager.Instance.CurrentGoldPerSecond : 0;
        if (rewardMinutesOfGPS > 0)
            gold += rewardMinutesOfGPS * 60.0 * Math.Max(gps, MinimumAssumedGPS);

        if (gold > 0)
            GoldManager.Instance?.AddGold(gold);

        if (rewardMasteryPoints > 0)
            MasteryManager.Instance?.AddPoints(rewardMasteryPoints);
    }
}