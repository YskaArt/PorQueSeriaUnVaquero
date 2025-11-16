
/*
 * EnemyGoldUpgradeData
 * --------------------
 * ScriptableObject que define la progresión del upgrade que aumenta el oro otorgado por los enemigos.
 *
 * FUNCIONAMIENTO:
 * - Hereda de UpgradeBase y utiliza su sistema de niveles, costos y eventos.
 * - Define cómo escalan las recompensas de oro por enemigo según el nivel actual:
 *      * Nivel 0: usa el valor base.
 *      * Nivel 1: recompensa = baseEnemyGold * 2
 *      * Nivel 2: recompensa = baseEnemyGold * 4
 *      * Nivel >=3: mantiene el *4 y suma un porcentaje del GPS por cada nivel extra.
 *
 * - También soporta un bonus opcional (similar a otras mejoras del proyecto):
 *      * Se habilita al alcanzar cierto nivel.
 *      * Tiene su propio costo y multiplica la recompensa final.
 *      * Usa BuyBonus() para descontar oro y disparar el evento de compra.
 *
 * RESPONSABILIDAD:
 * - Solo almacena la configuración y calcula el reward del enemigo.
 * - NO maneja oro directamente, solo consulta GoldManager al comprar el bonus.
 * - EnemyGoldManager usa este ScriptableObject para determinar la recompensa final.
 */


using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyGoldUpgrade", menuName = "Idle/Upgrades/EnemyGoldUpgrade")]
public class EnemyGoldUpgradeData : UpgradeBase
{
    [Header("Enemy gold behaviour")]
    public double baseEnemyGold = 1.0;
    [Tooltip("For levels >=3: percent of current GPS added per extra level (0.02 == 2% per level)")]
    public double enemyPercentOfGPSPerLevel = 0.02;

    [Header("Optional bonus (same pattern)")]
    public bool hasBonus = false;
    public int bonusUnlockLevel = 25;
    public double bonusCost = 10000;
    public double bonusMultiplier = 2.0;
    [HideInInspector] public bool bonusPurchased = false;

    public override bool HasBonus() => hasBonus;
    public override bool IsBonusAvailable() => hasBonus && !bonusPurchased && currentLevel >= bonusUnlockLevel;

    // Use RaiseBonusPurchased() instead of invoking the event directly.
    public override bool BuyBonus()
    {
        if (!IsBonusAvailable()) return false;
        if (GoldManager.Instance == null) return false;

        if (GoldManager.Instance.SpendGold(bonusCost))
        {
            bonusPurchased = true;
            // invoke the event safely from the base class helper
            RaiseBonusPurchased();
            return true;
        }
        return false;
    }

    public double CalculateEnemyReward(double currentGPS)
    {
        int lvl = currentLevel;
        if (lvl <= 0) return baseEnemyGold;
        if (lvl == 1) return baseEnemyGold * 2.0;
        if (lvl == 2) return baseEnemyGold * 4.0;

        int extraLevels = lvl - 2;
        double added = currentGPS * enemyPercentOfGPSPerLevel * extraLevels;
        return baseEnemyGold * 4.0 + added;
    }

    // Use base.ApplyLoadedState to set level + raise event safely
    public override void ApplyLoadedState(int loadedLevel)
    {
        base.ApplyLoadedState(loadedLevel);
    }
}
