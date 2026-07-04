/*
 * MasteryManager
 * --------------
 * Sistema de Maestría / Prestigio (pilar de diseño #3 del GDD: "Progresión Infinita").
 *
 * FUNCIONAMIENTO:
 * - El oro total ganado en la run actual (GoldManager.LifetimeGoldThisRun) determina
 *   cuántos puntos de maestría se obtienen al prestigiar:
 *       puntos = floor( sqrt(oroGanadoEnLaRun / goldForFirstPoint) )
 *   (el primer punto cuesta goldForFirstPoint, el segundo 4x, el tercero 9x, etc.)
 * - Cada punto de maestría otorga una bonificación PERMANENTE de +bonusPerPoint
 *   (por defecto 2%) a todo el oro ganado. Se aplica vía GoldManager.SetMasteryMultiplier.
 * - Prestige() suma los puntos, reinicia la run (upgrades, oro, nivel) mediante
 *   GameSaveManager.ResetRunProgress() y vuelve a la primera zona.
 *
 * PERSISTENCIA:
 * - masteryPoints y prestigeCount viven en GameSaveData; este manager los restaura
 *   en Start (o cuando aparece GameSaveManager tras un cambio de escena).
 *
 * ESCENA:
 * - No hace falta agregarlo a mano: ProgressionBootstrap lo instancia solo.
 */

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MasteryManager : MonoBehaviour
{
    public static MasteryManager Instance { get; private set; }

    [Header("Balance")]
    [Tooltip("Oro ganado en la run necesario para el primer punto de maestría")]
    [SerializeField] private double goldForFirstPoint = 100000;
    [Tooltip("Bonificación permanente de oro por punto (0.02 = +2%)")]
    [SerializeField] private double bonusPerPoint = 0.02;

    public int MasteryPoints { get; private set; }
    public int PrestigeCount { get; private set; }

    /// <summary>Se dispara cuando cambian los puntos o se prestigia.</summary>
    public event Action OnMasteryChanged;

    private bool stateLoaded;

    public double GoldMultiplier => 1.0 + MasteryPoints * bonusPerPoint;
    public double BonusPerPoint => bonusPerPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        TryLoadState();
        ApplyMultiplier();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // GameSaveManager/GoldManager pueden aparecer recién al entrar a GameScene
        TryLoadState();
        ApplyMultiplier();
    }

    private void TryLoadState()
    {
        if (stateLoaded) return;

        var data = GameSaveManager.Instance != null ? GameSaveManager.Instance.LoadedData : null;
        if (data == null) return;

        MasteryPoints = Mathf.Max(0, data.masteryPoints);
        PrestigeCount = Mathf.Max(0, data.prestigeCount);
        stateLoaded = true;
        OnMasteryChanged?.Invoke();
    }

    private void ApplyMultiplier()
    {
        GoldManager.Instance?.SetMasteryMultiplier(GoldMultiplier);
    }

    // ================== CÁLCULOS ==================

    /// <summary>Puntos que ganaría el jugador si prestigia ahora.</summary>
    public int PointsEarnedOnPrestige()
    {
        double lifetime = GoldManager.Instance != null ? GoldManager.Instance.LifetimeGoldThisRun : 0;
        if (lifetime <= 0 || goldForFirstPoint <= 0) return 0;
        return (int)Math.Floor(Math.Sqrt(lifetime / goldForFirstPoint));
    }

    /// <summary>Progreso 0..1 hacia el próximo punto de maestría (para la barra del HUD).</summary>
    public float ProgressToNextPoint()
    {
        double lifetime = GoldManager.Instance != null ? GoldManager.Instance.LifetimeGoldThisRun : 0;
        int current = PointsEarnedOnPrestige();

        double goldForCurrent = goldForFirstPoint * current * current;
        double goldForNext = goldForFirstPoint * (current + 1) * (current + 1);
        double range = goldForNext - goldForCurrent;
        if (range <= 0) return 0f;

        return Mathf.Clamp01((float)((lifetime - goldForCurrent) / range));
    }

    /// <summary>Oro de la run que falta para el próximo punto.</summary>
    public double GoldRemainingForNextPoint()
    {
        double lifetime = GoldManager.Instance != null ? GoldManager.Instance.LifetimeGoldThisRun : 0;
        int current = PointsEarnedOnPrestige();
        double goldForNext = goldForFirstPoint * (current + 1) * (current + 1);
        return Math.Max(0, goldForNext - lifetime);
    }

    public bool CanPrestige() => PointsEarnedOnPrestige() >= 1;

    // ================== ACCIONES ==================

    /// <summary>
    /// Ejecuta el prestigio: suma puntos, reinicia la run y vuelve a la primera zona.
    /// Devuelve false si todavía no hay puntos para ganar.
    /// </summary>
    public bool Prestige()
    {
        int earned = PointsEarnedOnPrestige();
        if (earned <= 0) return false;

        MasteryPoints += earned;
        PrestigeCount++;

        ApplyMultiplier();

        // Reinicia upgrades/oro/nivel conservando maestría, misiones y tutorial
        GameSaveManager.Instance?.ResetRunProgress();
        GameManager.Instance?.GotoLevel(0);

        Debug.Log($"[MasteryManager] Prestigio #{PrestigeCount}: +{earned} puntos (total {MasteryPoints}).");
        OnMasteryChanged?.Invoke();
        return true;
    }

    /// <summary>Recompensas de misiones u otros sistemas pueden otorgar puntos directos.</summary>
    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        MasteryPoints += amount;
        ApplyMultiplier();
        OnMasteryChanged?.Invoke();
    }

    /// <summary>Usado por el reset TOTAL del juego (borra también la maestría).</summary>
    public void ResetAll()
    {
        MasteryPoints = 0;
        PrestigeCount = 0;
        ApplyMultiplier();
        OnMasteryChanged?.Invoke();
    }
}
