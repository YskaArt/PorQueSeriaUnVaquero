/*
 * UpgradeBase
 * -----------
 * Clase base para todos los upgrades del juego (ScriptableObject).
 *
 * PROP�SITO:
 * - Centralizar l�gica com�n de mejoras: nombre, costo, multiplicador, nivel actual.
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
 * - Se usan m�todos protected (RaiseLevelChanged y RaiseBonusPurchased)
 *   para evitar que la UI o sistemas externos invoquen eventos accidentalmente.
 *
 * Cambios importantes en esta versi�n:
 * - Se a�adi� soporte para m�ltiples bonuses por upgrade (ej. cada 50 niveles).
 * - El UpgradeBase contiene ahora campos por defecto relacionados con bonus:
 *   hasBonus, bonusInterval, bonusCost, bonusMultiplierPerBonus y bonusCount.
 * - Las subclases pueden seguir sobrescribiendo la l�gica si necesitan comportamiento distinto.
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

    // Evento global: se dispara una vez por cada nivel COMPRADO (no al cargar el save).
    // Lo usan sistemas transversales como las misiones diarias.
    public static event Action<UpgradeBase> OnAnyLevelPurchased;

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
        OnAnyLevelPurchased?.Invoke(this);
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
        // Si bonusCount==0, el primer bonus est� disponible cuando currentLevel >= bonusInterval.
        // En general, el (bonusCount+1)-�simo bonus requiere levels >= (bonusCount+1) * bonusInterval.
        int requiredLevelForNextBonus = (bonusCount + 1) * Math.Max(1, bonusInterval);
        return currentLevel >= requiredLevelForNextBonus;
    }

    public virtual bool BuyBonus()
    {
        if (!HasBonus()) return false;

        // Verificar que el bonus est� disponible por nivel
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
    /// Se usa al cargar datos guardados sin ejecutar l�gica adicional.
    /// </summary>
    public virtual void ApplyLoadedState(int loadedLevel)
    {
        currentLevel = loadedLevel;
        RaiseLevelChanged();
    }

    /// <summary>
    /// M�todo p�blico seguro para aplicar la cantidad de bonuses cargada desde el save.
    /// Esto actualizar� internalmente bonusCount y disparar� OnBonusPurchased (una sola vez)
    /// para notificar a sistemas dependientes que el estado de bonus cambi�.
    /// </summary>
    public virtual void ApplyLoadedBonusCount(int loadedBonusCount)
    {
        // Si no soporta bonus, ignorar
        if (!HasBonus())
        {
            bonusCount = 0;
            return;
        }

        // S�lo aplicar si es distinto para evitar notificaciones innecesarias
        if (bonusCount != loadedBonusCount)
        {
            bonusCount = Mathf.Max(0, loadedBonusCount);
            // Notificar que el estado de bonus cambi� (al menos una vez).
            // Los subscribers deben recalcular con GetTotalBonusMultiplier()/GetEffectiveGPS() seg�n corresponda.
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