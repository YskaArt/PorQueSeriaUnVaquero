using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Idle/Upgrade")]
public class UpgradeData : ScriptableObject
{
    // ==========================
    // PROPIEDADES BÁSICAS
    // ==========================
    public string upgradeName;                 // Nombre del upgrade
    public double baseCost ;               // Costo base del primer nivel
    public double costMultiplier ;       // Multiplicador de costo por nivel
    public double goldPerSecondPerLevel = 0.1f;// Oro pasivo adicional por nivel
    public int currentLevel = 0;               // Nivel actual del upgrade

    // ==========================
    // DATOS DEL BONO (opcional, comprable)
    // ==========================
    [Header("Bonus (comprable)")]
    public bool hasBonus = false;              // Si esta mejora tiene bono disponible
    public int bonusUnlockLevel = 25;          // Nivel mínimo para poder comprar el bono
    public double bonusCost = 10000;           // Costo en oro para comprar el bono
    public double bonusMultiplier = 2.0;       // Multiplicador que aplica al OPS de este upgrade al comprar bono
    [HideInInspector] public bool bonusPurchased = false; // Si el bono ya fue comprado

    // ==========================
    // EVENTOS
    // ==========================
    public event Action OnLevelChanged;
    public event Action OnBonusPurchased; // Se dispara cuando se compra el bono

    // ==========================
    // MÉTODO: GetCost()
    // Coste del siguiente nivel
    // ==========================
    public double GetCost()
    {
        return baseCost * Mathf.Pow((float)costMultiplier, currentLevel);
    }

    // ==========================
    // MÉTODO: GetGoldPerSecondGain()
    // Ganancia pasiva actual sin contar bono
    // ==========================
    public double GetGoldPerSecondGain()
    {
        return goldPerSecondPerLevel * currentLevel;
    }

    // ==========================
    // MÉTODO: LevelUp()
    // Incrementa un nivel y dispara evento
    // ==========================
    public void LevelUp()
    {
        currentLevel++;
        OnLevelChanged?.Invoke();
    }

    // ==========================
    // MÉTODO: IsBonusAvailable()
    // Indica si el bono puede comprarse (cumple nivel y no comprado)
    // ==========================
    public bool IsBonusAvailable()
    {
        return hasBonus && !bonusPurchased && currentLevel >= bonusUnlockLevel;
    }

    // ==========================
    // MÉTODO: BuyBonus()
    // Intenta comprar el bono (usa GoldManager.SpendGold).
    // Devuelve true si la compra se realizó.
    // ==========================
    public bool BuyBonus()
    {
        if (!IsBonusAvailable()) return false;
        if (GoldManager.Instance == null) return false;

        if (GoldManager.Instance.SpendGold(bonusCost))
        {
            bonusPurchased = true;
            OnBonusPurchased?.Invoke();
            return true;
        }
        return false;
    }

    // ==========================
    // MÉTODO: GetEffectiveOPS()
    // Devuelve el OPS que aporta la mejora teniendo en cuenta si el bono
    // está comprado. No lo uses automáticamente hasta que decidas aplicarlo.
    // ==========================
    public double GetEffectiveOPS()
    {
        double baseOps = goldPerSecondPerLevel * currentLevel;
        return bonusPurchased ? baseOps * bonusMultiplier : baseOps;
    }
}
