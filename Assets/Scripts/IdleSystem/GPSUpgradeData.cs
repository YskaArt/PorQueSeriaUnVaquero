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
 * - Ahora el sistema de bonuses está gestionado desde UpgradeBase (multi-bonus).
 * - UpgradeBase contiene los parámetros hasBonus, bonusInterval, bonusCost, bonusMultiplierPerBonus y bonusCount.
 * - GPSUpgradeData usa GetTotalBonusMultiplier() para calcular el efecto.
 *
 * PERSISTENCIA:
 * - ApplyLoadedState() delega en UpgradeBase para restaurar nivel y despachar
 *   eventos de actualización correctamente.
 *
 * RESPONSABILIDAD:
 * - Solo almacena datos y reglas de cálculo del upgrade.
 * - No actualiza el GPS directamente; otro manager (probablemente UpgradeManager
 *   o un sistema de aplicación de upgrades) debe sumar el GPS resultante al
 *   GoldManager al comprar niveles o bonuses.
 */

using UnityEngine;

[CreateAssetMenu(fileName = "NewGPSUpgrade", menuName = "Idle/Upgrades/GPSUpgrade")]
public class GPSUpgradeData : UpgradeBase
{
    [Header("GPS")]
    [SerializeField] public double gpsPerLevel;

    // (Nota) Los parámetros relacionados con bonus se leen desde la clase base (UpgradeBase):
    // hasBonus, bonusInterval, bonusCost, bonusMultiplierPerBonus, bonusCount

    public double GetBaseGPS() => gpsPerLevel * currentLevel;

    public double GetEffectiveGPS()
    {
        double baseGPS = GetBaseGPS();
        double totalMultiplier = GetTotalBonusMultiplier(); // del UpgradeBase
        return baseGPS * totalMultiplier;
    }

    public override void ApplyLoadedState(int loadedLevel)
    {
        base.ApplyLoadedState(loadedLevel);
    }
}
