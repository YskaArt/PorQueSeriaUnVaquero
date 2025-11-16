/*
 * GPSUpgradeData (ScriptableObject)
 * ---------------------------------
 * Representa un upgrade que incrementa el oro por segundo (GPS) del jugador.
 * Forma parte del sistema de mejoras Idle y hereda de UpgradeBase.
 *
 * FUNCIONAMIENTO:
 * - Cada nivel comprado añade gpsPerLevel al GPS total.
 * - GetBaseGPS() devuelve el GPS sin modificar (niveles * gpsPorNivel).
 * - GetEffectiveGPS() devuelve el GPS final considerando si existe un bonus.
 *
 * BONUS:
 * - Opcional, configurable desde el inspector.
 * - Se desbloquea al alcanzar un nivel mínimo (bonusUnlockLevel).
 * - Tiene un costo bonusCost y al comprarlo aplica bonusMultiplier al GPS base.
 * - BuyBonus() verifica oro suficiente y utiliza GoldManager.SpendGold().
 * - Usa RaiseBonusPurchased() (protegido en UpgradeBase) para disparar eventos.
 *
 * PERSISTENCIA:
 * - ApplyLoadedState() delega en UpgradeBase para restaurar nivel y despachar
 *   eventos de actualización correctamente.
 *
 * RESPONSABILIDAD:
 * - Solo almacena datos y reglas de cálculo del upgrade.
 * - No actualiza el GPS directamente; otro manager (probablemente UpgradeManager
 *   o un sistema de aplicación de upgrades) debe sumar el GPS resultante al
 *   GoldManager al comprar niveles.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewGPSUpgrade", menuName = "Idle/Upgrades/GPSUpgrade")]
public class GPSUpgradeData : UpgradeBase
{
    [Header("GPS")]
    [SerializeField] public double gpsPerLevel;

    [Header("Bonus (optional)")]
    public bool hasBonus = false;
    public int bonusUnlockLevel = 25;
    public double bonusCost = 10000;
    public double bonusMultiplier = 2.0;
    [HideInInspector] public bool bonusPurchased = false;

    public override bool HasBonus() => hasBonus;
    public override bool IsBonusAvailable() => hasBonus && !bonusPurchased && currentLevel >= bonusUnlockLevel;

    public override bool BuyBonus()
    {
        if (!IsBonusAvailable()) return false;
        if (GoldManager.Instance == null) return false;

        if (GoldManager.Instance.SpendGold(bonusCost))
        {
            bonusPurchased = true;
            RaiseBonusPurchased();
            return true;
        }
        return false;
    }

    public double GetBaseGPS() => gpsPerLevel * currentLevel;

    public double GetEffectiveGPS()
    {
        double baseOps = GetBaseGPS();
        return bonusPurchased ? baseOps * bonusMultiplier : baseOps;
    }

    public override void ApplyLoadedState(int loadedLevel)
    {
        base.ApplyLoadedState(loadedLevel);
    }
}
