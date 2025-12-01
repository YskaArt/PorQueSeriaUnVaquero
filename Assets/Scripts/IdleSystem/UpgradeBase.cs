/*
 * UpgradeBase
 * -----------
 * Clase base para todos los upgrades del juego (ScriptableObject).
 *
 * PROPÓSITO:
 * - Centralizar lógica común de mejoras: nombre, costo, multiplicador, nivel actual.
 * - Proveer un sistema seguro de eventos donde las subclases y la UI
 *   pueden suscribirse, pero solo la clase (o subclases) puede invocar.
 * - Establecer funciones virtuales para extensiones como bonus, GPS, OPS, etc.
 *
 * FUNCIONAMIENTO GENERAL:
 * - Cada upgrade mantiene su nivel actual y puede calcular su costo incremental.
 * - Cuando se compra un nivel (LevelUp), se dispara un evento para que la UI se actualice.
 * - Los bonus son opcionales y se manejan en subclases sobrescribiendo HasBonus(), 
 *   IsBonusAvailable() y BuyBonus().
 * - ApplyLoadedState permite cargar un nivel desde guardado sin efectos colaterales 
 *   inesperados.
 *
 * EVENTOS:
 * - OnLevelChanged: Notifica cuando el nivel aumenta o se restaura desde carga.
 * - OnBonusPurchased: Lo mismo, pero cuando se compra un bonus opcional.
 *
 * NOTA:
 * - Se usan métodos protected (RaiseLevelChanged y RaiseBonusPurchased)
 *   para evitar que la UI o sistemas externos invoquen eventos accidentalmente.
 *
 * Cambios importantes en esta versión:
 * - Se añadió soporte para múltiples bonuses por upgrade (ej. cada 50 niveles).
 * - El UpgradeBase contiene ahora campos por defecto relacionados con bonus:
 *   hasBonus, bonusInterval, bonusCost, bonusMultiplierPerBonus y bonusCount.
 * - Las subclases pueden seguir sobrescribiendo la lógica si necesitan comportamiento distinto.
 */


using System;
using UnityEngine;

public abstract class UpgradeBase : ScriptableObject
{
    public string upgradeName;
    public double baseCost = 10.0;
    public double costMultiplier = 1.15;
    public int currentLevel = 0;

    // Eventos a los que otros sistemas pueden suscribirse, pero no invocar.
    public event Action OnLevelChanged;
    public event Action OnBonusPurchased;

    [Header("Bonus (base settings - multi-bonus support)")]
    public bool hasBonus = false;
    public int bonusInterval = 50;
    public double bonusCost = 10000;
    public double bonusMultiplierPerBonus = 2.0;
    public int bonusCount = 0;

    // COSTO Y NIVEL
    public virtual double GetCost()
    {
        return baseCost * Math.Pow(costMultiplier, currentLevel);
    }

    public virtual void LevelUp()
    {
        currentLevel++;
        RaiseLevelChanged();
    }

    // BONUS API
    // Consider a SO to 'support' bonuses either when hasBonus==true OR when the bonusInterval/cost are configured.
    public bool HasBonus()
    {
        return hasBonus || (bonusInterval > 0 && bonusCost > 0.0);
    }

    public virtual bool IsBonusAvailable()
    {
        if (!HasBonus()) return false;

        // A new bonus becomes available cada "bonusInterval" niveles.
        // Si bonusCount==0, el primer bonus está disponible cuando currentLevel >= bonusInterval.
        // En general, el (bonusCount+1)-ésimo bonus requiere levels >= (bonusCount+1) * bonusInterval.
        int requiredLevelForNextBonus = (bonusCount + 1) * Math.Max(1, bonusInterval);
        return currentLevel >= requiredLevelForNextBonus;
    }

    public virtual bool BuyBonus()
    {
        if (!HasBonus()) return false;

        // Verificar que el bonus esté disponible por nivel
        if (!IsBonusAvailable()) return false;

        double cost = GetBonusCostFor(bonusCount + 1);
        if (GoldManager.Instance == null) return false;

        if (!GoldManager.Instance.SpendGold(cost))
            return false;

        bonusCount++;
        RaiseBonusPurchased();
        return true;
    }

    public double GetBonusCostFor(int nextCount)
    {
        if (!HasBonus()) return double.MaxValue;
        // Escala exponencial por cada bonus adicional comprado
        return bonusCost * Math.Pow(bonusMultiplierPerBonus, Math.Max(0, nextCount - 1));
    }

    public double GetTotalBonusMultiplier()
    {
        if (!HasBonus()) return 1.0;
        return Math.Pow(bonusMultiplierPerBonus, bonusCount);
    }

    /// <summary>
    /// Se usa al cargar datos guardados sin ejecutar lógica adicional.
    /// </summary>
    public virtual void ApplyLoadedState(int loadedLevel)
    {
        currentLevel = loadedLevel;
        RaiseLevelChanged();
    }

    /// <summary>
    /// Método público seguro para aplicar la cantidad de bonuses cargada desde el save.
    /// Esto actualizará internalmente bonusCount y disparará OnBonusPurchased (una sola vez)
    /// para notificar a sistemas dependientes que el estado de bonus cambió.
    /// </summary>
    public virtual void ApplyLoadedBonusCount(int loadedBonusCount)
    {
        // Si no soporta bonus, ignorar
        if (!HasBonus())
        {
            bonusCount = 0;
            return;
        }

        // Sólo aplicar si es distinto para evitar notificaciones innecesarias
        if (bonusCount != loadedBonusCount)
        {
            bonusCount = Mathf.Max(0, loadedBonusCount);
            // Notificar que el estado de bonus cambió (al menos una vez).
            // Los subscribers deben recalcular con GetTotalBonusMultiplier()/GetEffectiveGPS() según corresponda.
            RaiseBonusPurchased();
        }
    }

    // Invocaciones protegidas de eventos (solo clase/subclases)
    protected void RaiseLevelChanged()
    {
        OnLevelChanged?.Invoke();
    }

    protected void RaiseBonusPurchased()
    {
        OnBonusPurchased?.Invoke();
    }
}