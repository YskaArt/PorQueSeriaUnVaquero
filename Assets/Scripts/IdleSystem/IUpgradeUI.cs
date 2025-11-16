/*
 * IUpgradeUI
 * ----------
 * Interfaz base para cualquier elemento de UI que represente una mejora (upgrade).
 *
 * RESPONSABILIDAD:
 * - Proveer acceso a los datos de mejora asociados (UpgradeBase).
 * - Permitir que un gestor externo (UpgradeUIManager) fuerce la actualización visual
 *   de este elemento de UI.
 *
 * IMPLEMENTACIÓN:
 * - Cualquier clase que represente un slot, botón o panel de mejora en la UI
 *   debe implementar esta interfaz.
 * - El UpgradeUIManager utilizará ForceUpdateUI() cuando haya cambios globales,
 *   como variaciones de oro o compra masiva de niveles.
 */

public interface IUpgradeUI
{
    /// <summary>
    /// Devuelve el UpgradeBase asociado a esta UI.
    /// Permite al sistema acceder a sus valores como nivel, precio, etc.
    /// </summary>
    UpgradeBase GetUpgradeData();

    /// <summary>
    /// Fuerza un refresco manual de la UI.
    /// Usado por UpgradeUIManager cuando los datos cambian.
    /// </summary>
    void ForceUpdateUI();
}
