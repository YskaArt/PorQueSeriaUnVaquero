using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Idle/Upgrade")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;                 // Nombre del upgrade
    public double baseCost = 10;               // Costo base del primer nivel
    public double costMultiplier = 1.75;       // Multiplicador de costo por nivel
    public double goldPerSecondPerLevel = 0.1f; // Oro pasivo adicional por nivel
    public int currentLevel = 0;               // Nivel actual del upgrade

    // Evento que notifica a otros scripts (ej. UI) cuando sube de nivel
    public event Action OnLevelChanged;

    // MÉTODO: GetCost()
    // Calcula el costo actual del upgrade según su nivel
    public double GetCost()
    {
        return baseCost * Mathf.Pow((float)costMultiplier, currentLevel);
    }

    // MÉTODO: GetGoldPerSecondGain()
    // Calcula cuánto oro por segundo aporta este upgrade según su nivel
    public double GetGoldPerSecondGain()
    {
        return goldPerSecondPerLevel * currentLevel;
    }

    // MÉTODO: LevelUp()
    // Incrementa el nivel del upgrade y dispara el evento OnLevelChanged
    public void LevelUp()
    {
        currentLevel++;
        OnLevelChanged?.Invoke();
    }
}
