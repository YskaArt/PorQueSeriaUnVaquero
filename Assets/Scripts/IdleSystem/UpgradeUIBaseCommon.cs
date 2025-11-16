/*
 * UpgradeUIBaseCommon
 * -------------------
 * Clase base abstracta que define el **contrato mínimo** que cualquier UI de upgrade
 * debe implementar para poder ser controlada por los sistemas del menú (UpgradeMenuController,
 * toggles, refrescos globales, etc.).
 *
 * No contiene lógica propia. Solo asegura que cualquier UI concreta:
 *
 *   ✔ Pueda actualizar su interfaz (UpdateUI / ForceUpdateUI).
 *   ✔ Permita cambiar la cantidad de compra (1,10,50,MAX).
 *   ✔ Exponga el UpgradeBase asociado (GetUpgradeData).
 *   ✔ Pueda calcular cuántos niveles puede comprar el jugador (GetMaxAffordableLevels).
 *
 * De esta forma, los controladores del menú pueden interactuar con cualquier tipo de
 * upgrade sin conocer su lógica interna ni su tipo específico.
 */

using UnityEngine;

public abstract class UpgradeUIBaseCommon : MonoBehaviour
{
    /// <summary>
    /// Actualiza la UI (niveles, precio, botones, etc.).
    /// </summary>
    public abstract void UpdateUI();

    /// <summary>
    /// Fuerza actualización incluso si no hubo eventos.
    /// </summary>
    public abstract void ForceUpdateUI();

    /// <summary>
    /// Cambia la cantidad seleccionada para comprar (1,10,50 o -1 = MAX).
    /// </summary>
    public abstract void SetQuantityToBuy(int q);

    /// <summary>
    /// Devuelve el ScriptableObject que controla esta mejora.
    /// </summary>
    public abstract UpgradeBase GetUpgradeData();

    /// <summary>
    /// Calcula cuántos niveles puede comprar el jugador con su oro actual.
    /// </summary>
    public abstract int GetMaxAffordableLevels();
}
