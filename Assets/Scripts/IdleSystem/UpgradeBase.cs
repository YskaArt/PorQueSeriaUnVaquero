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
 *   inesperados, notificando a la UI después.
 *
 * EVENTOS:
 * - OnLevelChanged: Notifica cuando el nivel aumenta o se restaura desde carga.
 * - OnBonusPurchased: Lo mismo, pero cuando se compra un bonus opcional.
 *
 * NOTA:
 * - Se usan métodos protected (RaiseLevelChanged y RaiseBonusPurchased)
 *   para evitar que la UI o sistemas externos invoquen eventos accidentalmente.
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

    /// <summary>
    /// Calcula el costo actual según nivel y multiplicador.
    /// </summary>
    public virtual double GetCost()
    {
        return baseCost * Math.Pow(costMultiplier, currentLevel);
    }

    /// <summary>
    /// Aumenta el nivel del upgrade y notifica a los suscriptores.
    /// </summary>
    public virtual void LevelUp()
    {
        currentLevel++;
        RaiseLevelChanged();
    }

    public virtual bool HasBonus() => false;
    public virtual bool IsBonusAvailable() => false;

    /// <summary>
    /// Subclases deben implementar compra de bonus si corresponde.
    /// </summary>
    public virtual bool BuyBonus() { return false; }

    /// <summary>
    /// Se usa al cargar datos guardados sin ejecutar lógica adicional.
    /// </summary>
    public virtual void ApplyLoadedState(int loadedLevel)
    {
        currentLevel = loadedLevel;
        RaiseLevelChanged();
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
