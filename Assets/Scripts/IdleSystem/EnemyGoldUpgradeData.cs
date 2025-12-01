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
 * - Soporta un sistema de bonus multi-etapa (cada N niveles) definido en UpgradeBase:
 *      * hasBonus, bonusInterval, bonusCost, bonusMultiplierPerBonus, bonusCount
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

    // (Nota) Parámetros de bonus reubicados en UpgradeBase: hasBonus, bonusInterval, bonusCost, bonusMultiplierPerBonus, bonusCount

    public double CalculateEnemyReward(double currentGPS)
    {
        int lvl = currentLevel;
        double reward;
        if (lvl <= 0) reward = baseEnemyGold;
        else if (lvl == 1) reward = baseEnemyGold * 2.0;
        else if (lvl == 2) reward = baseEnemyGold * 4.0;
        else
        {
            int extraLevels = lvl - 2;
            double added = currentGPS * enemyPercentOfGPSPerLevel * extraLevels;
            reward = baseEnemyGold * 4.0 + added;
        }

        // Aplicar multiplicador total por bonuses (si los hay)
        double totalMultiplier = GetTotalBonusMultiplier();
        return reward * totalMultiplier;
    }

    // Use base.ApplyLoadedState to set level + raise event safely
    public override void ApplyLoadedState(int loadedLevel)
    {
        base.ApplyLoadedState(loadedLevel);
    }
}
