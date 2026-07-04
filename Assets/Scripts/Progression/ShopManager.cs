/*
 * ShopManager
 * -----------
 * Tienda funcional (botón dedicado de la UI según el GDD).
 *
 * ITEMS:
 * 1) GOLD RUSH (rewarded ad): oro instantáneo equivalente a N minutos de GPS.
 * 2) FRENZY (rewarded ad): multiplicador x2 a TODO el oro ganado por un tiempo.
 * 3) LUCKY HORSESHOE (se paga con oro): multiplicador menor por más tiempo.
 *
 * BOOSTS:
 * - Solo puede haber un boost temporal activo a la vez; comprar el mismo boost
 *   extiende su duración, comprar otro lo reemplaza.
 * - El multiplicador se aplica vía GoldManager.SetBoostMultiplier y se stackea
 *   multiplicativamente con la maestría.
 * - El tiempo restante persiste en el save y descuenta el tiempo offline.
 *
 * ESCENA:
 * - No hace falta agregarlo a mano: ProgressionBootstrap lo instancia solo.
 */

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Gold Rush (rewarded ad)")]
    [Tooltip("Oro instantáneo = X minutos del GPS actual")]
    [SerializeField] private double goldRushMinutesOfGPS = 10;
    [Tooltip("Oro mínimo si el GPS todavía es muy bajo")]
    [SerializeField] private double goldRushMinimumGold = 500;

    [Header("Frenzy (rewarded ad)")]
    [SerializeField] private double frenzyMultiplier = 2.0;
    [SerializeField] private float frenzyDurationSeconds = 600f; // 10 min

    [Header("Lucky Horseshoe (se compra con oro)")]
    [SerializeField] private double horseshoeMultiplier = 1.5;
    [SerializeField] private float horseshoeDurationSeconds = 1800f; // 30 min
    [Tooltip("Costo = X minutos del GPS actual")]
    [SerializeField] private double horseshoeCostMinutesOfGPS = 20;
    [Tooltip("Costo mínimo si el GPS todavía es muy bajo")]
    [SerializeField] private double horseshoeMinimumCost = 1000;

    // Boost temporal activo
    private double activeBoostMultiplier = 1.0;
    private float boostRemainingSeconds;
    private bool stateLoaded;

    /// <summary>Se dispara al activar/expirar un boost o completar una compra.</summary>
    public event Action OnShopStateChanged;

    public bool BoostActive => boostRemainingSeconds > 0f && activeBoostMultiplier > 1.0;
    public double ActiveBoostMultiplier => BoostActive ? activeBoostMultiplier : 1.0;
    public float BoostRemainingSeconds => Mathf.Max(0f, boostRemainingSeconds);

    public double FrenzyMultiplier => frenzyMultiplier;
    public float FrenzyDurationSeconds => frenzyDurationSeconds;
    public double HorseshoeMultiplier => horseshoeMultiplier;
    public float HorseshoeDurationSeconds => horseshoeDurationSeconds;

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

    private void Start() => TryRestoreState();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryRestoreState();

    private void Update()
    {
        if (!BoostActive) return;

        boostRemainingSeconds -= Time.unscaledDeltaTime;
        if (boostRemainingSeconds <= 0f)
        {
            boostRemainingSeconds = 0f;
            activeBoostMultiplier = 1.0;
            ApplyBoost();
            OnShopStateChanged?.Invoke();
            Debug.Log("[ShopManager] Boost expirado.");
        }
    }

    private void ApplyBoost() =>
        GoldManager.Instance?.SetBoostMultiplier(ActiveBoostMultiplier);

    // ================== PRECIOS / VALORES ==================

    public double GetGoldRushAmount()
    {
        double gps = GoldManager.Instance != null ? GoldManager.Instance.CurrentGoldPerSecond : 0;
        return Math.Max(goldRushMinimumGold, goldRushMinutesOfGPS * 60.0 * gps);
    }

    public double GetHorseshoeCost()
    {
        double gps = GoldManager.Instance != null ? GoldManager.Instance.CurrentGoldPerSecond : 0;
        return Math.Max(horseshoeMinimumCost, horseshoeCostMinutesOfGPS * 60.0 * gps);
    }

    // ================== COMPRAS ==================

    /// <summary>Gold Rush: ver un rewarded ad a cambio de oro instantáneo.</summary>
    public void BuyGoldRushWithAd(Action<bool> onDone = null)
    {
        RunRewardedPurchase(granted =>
        {
            if (granted)
            {
                GoldManager.Instance?.AddGold(GetGoldRushAmount());
                OnShopStateChanged?.Invoke();
                GameSaveManager.Instance?.SaveGame();
            }
            onDone?.Invoke(granted);
        });
    }

    /// <summary>Frenzy: ver un rewarded ad a cambio de x2 oro temporal.</summary>
    public void BuyFrenzyWithAd(Action<bool> onDone = null)
    {
        RunRewardedPurchase(granted =>
        {
            if (granted)
                ActivateBoost(frenzyMultiplier, frenzyDurationSeconds);
            onDone?.Invoke(granted);
        });
    }

    /// <summary>Lucky Horseshoe: boost menor pagado con oro. Devuelve false si no alcanza.</summary>
    public bool BuyHorseshoe()
    {
        if (GoldManager.Instance == null) return false;
        if (!GoldManager.Instance.SpendGold(GetHorseshoeCost())) return false;

        ActivateBoost(horseshoeMultiplier, horseshoeDurationSeconds);
        return true;
    }

    private void RunRewardedPurchase(Action<bool> onDone)
    {
        if (AdsManager.Instance == null)
        {
            Debug.LogWarning("[ShopManager] AdsManager no disponible.");
            onDone?.Invoke(false);
            return;
        }
        StartCoroutine(AdsManager.Instance.ShowRewardedAdCoroutine(onDone));
    }

    private void ActivateBoost(double multiplier, float durationSeconds)
    {
        bool sameBoost = BoostActive && Math.Abs(activeBoostMultiplier - multiplier) < 0.0001;

        activeBoostMultiplier = multiplier;
        boostRemainingSeconds = sameBoost
            ? boostRemainingSeconds + durationSeconds  // extender el mismo boost
            : durationSeconds;                          // reemplazar por el nuevo

        ApplyBoost();
        OnShopStateChanged?.Invoke();
        GameSaveManager.Instance?.SaveGame();

        Debug.Log($"[ShopManager] Boost x{multiplier} activo por {boostRemainingSeconds / 60f:0.#} min.");
    }

    // ================== PERSISTENCIA ==================

    private void TryRestoreState()
    {
        if (stateLoaded) { ApplyBoost(); return; }

        var data = GameSaveManager.Instance != null ? GameSaveManager.Instance.LoadedData : null;
        if (data == null) return;

        stateLoaded = true;

        if (data.activeBoost != null && data.activeBoost.remainingSeconds > 0f && data.activeBoost.multiplier > 1.0)
        {
            // Descontar el tiempo que el juego estuvo cerrado
            float offlineSeconds = 0f;
            if (data.lastSaveTimestamp > 0)
            {
                var lastSave = DateTime.FromBinary(data.lastSaveTimestamp);
                offlineSeconds = Mathf.Max(0f, (float)(DateTime.Now - lastSave).TotalSeconds);
            }

            boostRemainingSeconds = Mathf.Max(0f, data.activeBoost.remainingSeconds - offlineSeconds);
            activeBoostMultiplier = boostRemainingSeconds > 0f ? data.activeBoost.multiplier : 1.0;
        }

        ApplyBoost();
        OnShopStateChanged?.Invoke();
    }

    /// <summary>Snapshot del boost para GameSaveManager.</summary>
    public BoostSaveData GetBoostSaveData() => new BoostSaveData
    {
        multiplier = BoostActive ? activeBoostMultiplier : 1.0,
        remainingSeconds = BoostRemainingSeconds
    };

    /// <summary>Usado por el reset TOTAL del juego.</summary>
    public void ResetAll()
    {
        activeBoostMultiplier = 1.0;
        boostRemainingSeconds = 0f;
        ApplyBoost();
        OnShopStateChanged?.Invoke();
    }
}
