/*
 * GPSUpgradeUI
 * ------------
 * Controla la interfaz de usuario para la mejora de GPS (oro por segundo).
 * Hereda de UpgradeUIBase<T>, usando GPSUpgradeData como fuente de datos.
 *
 * FUNCIONAMIENTO:
 * - Muestra nivel actual, costo y cantidad a comprar según la selección del usuario.
 * - Cuando el jugador compra niveles, OnLevelBought() añade el GPS correspondiente
 *   al GoldManager (gpsPerLevel por nivel comprado).
 *
 * MÉTODOS CLAVE:
 *   • OnLevelBought()
 *       - Suma al GPS global la ganancia por nivel comprada.
 *
 *   • BuildDisplayStrings(out levelStr, out priceStr)
 *       - Construye los textos de UI:
 *           - levelStr: nivel actual + GPS ganado por nivel.
 *           - priceStr: cantidad a comprar y costo total formateado.
 *       - Considera compra individual, múltiple o compra máxima (MAX).
 *
 * RESPONSABILIDAD:
 * - Únicamente trabaja con la presentación en pantalla y comunica al GoldManager
 *   la ganancia de GPS cuando se adquiere un nivel.
 * - No calcula el GPS total acumulado ni maneja bonus: eso lo define GPSUpgradeData.
 */

using TMPro;
using UnityEngine;

public class GPSUpgradeUI : UpgradeUIBase<GPSUpgradeData>
{
    protected override void OnLevelBought()
    {
        if (upgradeData != null)
            GoldManager.Instance?.AddGoldPerSecond(upgradeData.gpsPerLevel);
    }

    protected override void BuildDisplayStrings(out string levelStr, out string priceStr)
    {
        string opsFormatted = GoldManager.FormatNumber(upgradeData.gpsPerLevel);
        levelStr = $"Lv. {upgradeData.currentLevel}\n<color=#888>+{opsFormatted} GPS</color>";

        int displayQty = (selectedQuantity < 0) ? GetMaxAffordableLevels() : selectedQuantity;
        double total = GetTotalCostForQuantity(displayQty);
        priceStr = $"Buy {displayQty}\n{GoldManager.FormatNumber(total)}";
    }
}
