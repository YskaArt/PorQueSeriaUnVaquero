using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Idle/Upgrade")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;
    public double baseCost = 10;
    public double costMultiplier = 1.75;
    public double goldPerSecondPerLevel = 0.1f;
    public int currentLevel = 0;

    public double GetCost()
    {
        return baseCost * Mathf.Pow((float)costMultiplier, currentLevel);
    }

    public double GetGoldPerSecondGain()
    {
        return goldPerSecondPerLevel * currentLevel;
    }

    public void LevelUp()
    {
        currentLevel++;
    }
}
