/*
 * UpgradeUIManager
 * ----------------
 * Manager responsable de:
 *
 *   ✔ Mostrar / ocultar cada panel de upgrade según el progreso del jugador.
 *   ✔ Actualizar dinámicamente la UI cuando cambian el oro o los niveles de cualquier upgrade.
 *   ✔ Suscribirse y desuscribirse automáticamente a los eventos de cada UpgradeBase.
 *
 * Funcionamiento:
 * ---------------
 * - En el inspector se asignan:
 *       upgradeUIComponents → scripts concretos de cada upgrade (UI lógica)
 *       upgradeVisuals      → los GameObjects visuales (paneles)
 *
 * - En OnEnable se suscribe a:
 *       GoldManager.OnGoldChanged  → refresca todos los upgrades
 *       UpgradeBase.OnLevelChanged → refresca cuando un upgrade sube de nivel
 *
 * - RefreshUI controla dos cosas:
 *       1. Actualiza la visibilidad del panel (reveal/hide).
 *       2. Llama a ForceUpdateUI() para refrescar datos internos.
 *
 * Reglas de visibilidad:
 * ----------------------
 *  - El primer upgrade siempre se muestra.
 *  - Los siguientes aparecen si:
 *         • La mejora anterior tiene nivel >= 1
 *         • O el jugador tiene suficiente oro (>= 50% del costo del upgrade actual)
 *
 * Esto permite un sistema escalable donde las mejoras se desbloquean gradualmente
 * sin necesidad de lógica especial por upgrade.
 */

using UnityEngine;
using System.Collections.Generic;

public class UpgradeUIManager : MonoBehaviour
{
    [Tooltip("Assign the UI components (scripts) here: GPSUpgradeUI, EnemyGoldUpgradeUI, etc.")]
    [SerializeField] private UpgradeUIBaseCommon[] upgradeUIComponents;

    [SerializeField] private GameObject[] upgradeVisuals;

    private bool[] hasBeenRevealed;

    // Para limpiar eventos fácilmente
    private readonly List<UpgradeBase> subscribedUpgrades = new List<UpgradeBase>();

    private void Awake()
    {
        int len = upgradeUIComponents != null ? upgradeUIComponents.Length : 0;
        hasBeenRevealed = new bool[len];
    }

    private void OnEnable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged += RefreshUI;

        SubscribeToUpgradeEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= RefreshUI;

        UnsubscribeFromUpgradeEvents();
    }

    private void SubscribeToUpgradeEvents()
    {
        UnsubscribeFromUpgradeEvents();
        subscribedUpgrades.Clear();

        if (upgradeUIComponents == null) return;

        foreach (var uiComp in upgradeUIComponents)
        {
            if (uiComp == null) continue;

            var data = uiComp.GetUpgradeData();
            if (data == null) continue;

            data.OnLevelChanged += RefreshUI;
            subscribedUpgrades.Add(data);
        }
    }

    private void UnsubscribeFromUpgradeEvents()
    {
        foreach (var up in subscribedUpgrades)
        {
            if (up != null)
                up.OnLevelChanged -= RefreshUI;
        }

        subscribedUpgrades.Clear();
    }

    public void RefreshUI()
    {
        if (GoldManager.Instance == null ||
            upgradeUIComponents == null ||
            upgradeVisuals == null) return;

        double gold = GoldManager.Instance.CurrentGold;

        for (int i = 0; i < upgradeUIComponents.Length; i++)
        {
            UpgradeUIBaseCommon ui = upgradeUIComponents[i];
            GameObject visual = upgradeVisuals[i];

            if (ui == null || visual == null) continue;

            UpgradeBase data = ui.GetUpgradeData();
            if (data == null)
            {
                visual.SetActive(false);
                continue;
            }

            bool show = false;

            // Primera mejora → siempre visible
            if (i == 0)
            {
                show = true;
            }
            else
            {
                UpgradeBase prevUpgrade = upgradeUIComponents[i - 1].GetUpgradeData();

                if (prevUpgrade.currentLevel >= 1)
                {
                    show = true;
                }
                else if (gold >= data.GetCost() * 0.5)
                {
                    show = true;
                }
            }

            visual.SetActive(show);

            if (show)
                ui.ForceUpdateUI();
        }
    }
}
