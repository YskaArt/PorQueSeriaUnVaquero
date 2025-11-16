/*
 * UpgradeMenuToggle
 * ------------------
 * Componente simple que permite refrescar un grupo específico de UIs de mejoras.
 *
 * FUNCIONAMIENTO:
 * - Este script hereda de UIControllerBase (clase común para controladores UI).
 * - Guarda una lista de UpgradeUIBaseCommon[] llamada "upgrades".
 * - Cuando se llama a RefreshUI(), recorre todas las UIs y fuerza que cada una
 *   actualice sus textos, costos y estado.
 *
 * USO TÍPICO:
 * - Vinculado a un panel que no controla el menú entero, pero sí un subconjunto
 *   de mejoras que deben refrescarse al abrirse o al recibir algún evento.
 */

using UnityEngine;

public class UpgradeMenuToggle : UIControllerBase
{
    [SerializeField] private UpgradeUIBaseCommon[] upgrades;

    public void RefreshUI()
    {
        if (upgrades == null) return;
        foreach (var u in upgrades)
            u?.ForceUpdateUI();
    }
}
