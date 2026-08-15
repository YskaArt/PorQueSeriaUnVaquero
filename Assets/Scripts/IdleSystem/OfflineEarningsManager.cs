/*
 * OfflineEarningsManager
 * -----------------------
 * Calcula el oro que el jugador "se perdió" mientras la app estaba cerrada,
 * en base al tiempo transcurrido y al GPS que tenía al cerrar/abrir.
 *
 * NO requiere mantener la app corriendo en segundo plano: se apoya en
 * GameSaveData.lastSaveTimestamp, que YA se actualiza en cada SaveGame()
 * (incluido OnApplicationQuit y OnApplicationPause(true) en GameSaveManager).
 * Es decir: "lastSaveTimestamp" == el momento más reciente en el que sabemos
 * con certeza que el juego estaba abierto.
 *
 * REGLA DE NEGOCIO:
 * - Oro potencial = segundos transcurridos (con tope) * GPS actual.
 * - Reclamo base:   30% de ese potencial, sin costo.
 * - Reclamo con ad: 60% de ese potencial, mirando un rewarded.
 * - No se ofrece nada si el GPS es 0 (jugador nuevo / sin upgrades de GPS aún)
 *   o si el tiempo afuera fue insignificante.
 *
 * FLUJO:
 * - GoldManager.Start() llama a CheckForOfflineEarnings() justo después de
 *   aplicar el estado cargado (para que el GPS ya esté seteado).
 * - Si corresponde, dispara OnOfflineEarningsAvailable -> lo escucha
 *   OfflineEarningsPanel para abrirse solo.
 *
 * ESCENA:
 * - No hace falta agregarlo a mano: ProgressionBootstrap lo instancia solo.
 */

using System;
using UnityEngine;

public class OfflineEarningsManager : MonoBehaviour
{
    public static OfflineEarningsManager Instance { get; private set; }

    [Header("Porcentajes de recompensa")]
    [Range(0f, 1f)][SerializeField] private double baseRewardPercent = 0.30;
    [Range(0f, 1f)][SerializeField] private double adRewardPercent = 0.60;

    [Header("Límites")]
    [Tooltip("Tope de horas offline que se toman en cuenta (evita cálculos absurdos si pasó mucho tiempo)")]
    [SerializeField] private double maxOfflineHours = 8;
    [Tooltip("Si estuviste afuera menos que esto, directamente no se ofrece nada")]
    [SerializeField] private double minOfflineSecondsToShow = 60; // 30 minutos
    [Tooltip("Oro mínimo (versión base) para que valga la pena mostrar el panel")]
    [SerializeField] private double minGoldToShow = 1;

    /// <summary>(baseReward, adReward, elapsedSecondsCapped)</summary>
    public event Action<double, double, double> OnOfflineEarningsAvailable;

    public bool HasPendingOffer { get; private set; }
    public double PendingBaseReward { get; private set; }
    public double PendingAdReward { get; private set; }
    public double PendingElapsedSeconds { get; private set; }

    private bool checkedThisSession;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>Llamado una vez por sesión desde GoldManager.Start(), después de aplicar el save.</summary>
    public void CheckForOfflineEarnings()
    {
        if (checkedThisSession)
        {
            Debug.Log("[OfflineEarnings] Corte: ya se chequeó esta sesión (checkedThisSession=true).");
            return;
        }
        checkedThisSession = true;

        var data = GameSaveManager.Instance != null ? GameSaveManager.Instance.LoadedData : null;
        if (data == null)
        {
            Debug.Log("[OfflineEarnings] Corte: GameSaveManager.Instance.LoadedData es null.");
            return;
        }
        if (data.lastSaveTimestamp == 0)
        {
            Debug.Log("[OfflineEarnings] Corte: lastSaveTimestamp == 0 (nunca se guardó antes, o es la primera vez).");
            return;
        }
        if (GoldManager.Instance == null)
        {
            Debug.Log("[OfflineEarnings] Corte: GoldManager.Instance es null.");
            return;
        }

        double gps = GoldManager.Instance.CurrentGoldPerSecond;
        Debug.Log($"[OfflineEarnings] CurrentGoldPerSecond = {gps}");
        if (gps <= 0)
        {
            Debug.Log("[OfflineEarnings] Corte: GPS actual es 0 (sin mejoras de GPS compradas todavía).");
            return;
        }

        DateTime lastSave;
        try { lastSave = DateTime.FromBinary(data.lastSaveTimestamp); }
        catch (Exception e)
        {
            Debug.Log($"[OfflineEarnings] Corte: excepción al parsear lastSaveTimestamp: {e.Message}");
            return;
        }

        double elapsedSeconds = (DateTime.Now - lastSave).TotalSeconds;
        Debug.Log($"[OfflineEarnings] lastSave={lastSave}, ahora={DateTime.Now}, elapsedSeconds={elapsedSeconds:F1}, minRequerido={minOfflineSecondsToShow}");
        if (elapsedSeconds < minOfflineSecondsToShow)
        {
            Debug.Log("[OfflineEarnings] Corte: elapsedSeconds < minOfflineSecondsToShow.");
            return;
        }

        double cappedSeconds = Math.Min(elapsedSeconds, maxOfflineHours * 3600.0);
        double fullPotential = cappedSeconds * gps;

        double baseReward = fullPotential * baseRewardPercent;
        double adReward = fullPotential * adRewardPercent;

        Debug.Log($"[OfflineEarnings] fullPotential={fullPotential:F1}, baseReward={baseReward:F1}, minGoldToShow={minGoldToShow}");
        if (baseReward < minGoldToShow)
        {
            Debug.Log("[OfflineEarnings] Corte: baseReward < minGoldToShow.");
            return;
        }

        PendingBaseReward = baseReward;
        PendingAdReward = adReward;
        PendingElapsedSeconds = cappedSeconds;
        HasPendingOffer = true;

        Debug.Log($"[OfflineEarnings] {cappedSeconds:F0}s afuera -> base {baseReward:F0} / ad {adReward:F0}");
        OnOfflineEarningsAvailable?.Invoke(baseReward, adReward, cappedSeconds);
    }

    /// <summary>Reclama el 30% sin anuncio.</summary>
    public void ClaimBase()
    {
        if (!HasPendingOffer) return;
        GoldManager.Instance?.AddGold(PendingBaseReward);
        ClearOffer();
    }

    /// <summary>Reclama el 60% mostrando un rewarded ad primero. Si el ad falla, no se otorga nada extra.</summary>
    public void ClaimWithAd(Action<bool> onResult = null)
    {
        if (!HasPendingOffer)
        {
            onResult?.Invoke(false);
            return;
        }

        if (AdsManager.Instance == null)
        {
            onResult?.Invoke(false);
            return;
        }

        double adReward = PendingAdReward; // capturar antes de limpiar la oferta
        StartCoroutine(AdsManager.Instance.ShowRewardedAdCoroutine(granted =>
        {
            if (granted)
                GoldManager.Instance?.AddGold(adReward);

            ClearOffer();
            onResult?.Invoke(granted);
        }));
    }

    /// <summary>Descarta la oferta (ej. si el jugador cierra el panel sin elegir con anuncio).</summary>
    public void Dismiss()
    {
        ClearOffer();
    }

    private void ClearOffer()
    {
        HasPendingOffer = false;
        PendingBaseReward = 0;
        PendingAdReward = 0;
        PendingElapsedSeconds = 0;
    }
}
