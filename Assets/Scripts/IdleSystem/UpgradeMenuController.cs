/*
 * UpgradeMenuController
 * ---------------------
 * Controla todo el menú de mejoras del juego, incluyendo:
 * - Cambio entre las pestañas OPS / General / Settings.
 * - Activar o desactivar el panel principal.
 * - Controlar los botones de compra múltiple (1, 10, 50, Max).
 * - Actualizar todas las UIs de upgrades cuando se abre el menú
 *   o cuando se cambia de pestaña.
 *
 * FUNCIONAMIENTO GENERAL:
 * - Awake() conecta los botones de cantidad de compra con SetPurchaseQuantity().
 * - Start() conecta las pestañas del menú, selecciona por defecto la pestaña OPS
 *   y esconde el menú principal (hasta que se abra manualmente).
 * - OpenMenu() muestra el menú y refresca todas las UIs de mejoras.
 * - CloseMenu() simplemente oculta el menú.
 * - ShowPanel() gestiona el cambio entre pestañas, reinicia el scroll,
 *   muestra/oculta los botones de cantidad y refresca todas las UIs.
 * - SetPurchaseQuantity() envía la cantidad elegida a todas las UIs OPS
 *   para que calculen sus costos y textos.
 *
 * NOTA:
 * - allUpgradeUIs se usa para refrescar todo el menú.
 * - opsUpgradeUIs solo se usa para actualizar la cantidad de compra
 *   en los upgrades relacionados a OPS.
 */

using UnityEngine;
using UnityEngine.UI;

public class UpgradeMenuController : MonoBehaviour
{
    [Header("Panel Principal")]
    [SerializeField] private GameObject upgradeMenuPanel;

    [Header("Buttons")]
    [SerializeField] private Button opsButton;
    [SerializeField] private Button generalButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private GameObject opsPanel;
    [SerializeField] private GameObject generalPanel;
    [SerializeField] private GameObject settingsPanel;

    [SerializeField] private ScrollRect opsScroll;
    [SerializeField] private ScrollRect generalScroll;
    [SerializeField] private ScrollRect settingsScroll;

    [Header("Quantity buttons (OPS)")]
    [SerializeField] private GameObject quantityButtonsPanel;
    [SerializeField] private Button btnBuy1;
    [SerializeField] private Button btnBuy10;
    [SerializeField] private Button btnBuy50;
    [SerializeField] private Button btnBuyMax;

    [Header("Upgrade UIs")]
    [SerializeField] private UpgradeUIBaseCommon[] allUpgradeUIs;
    [SerializeField] private UpgradeUIBaseCommon[] opsUpgradeUIs;

    private void Awake()
    {
        btnBuy1.onClick.AddListener(() => SetPurchaseQuantity(1));
        btnBuy10.onClick.AddListener(() => SetPurchaseQuantity(10));
        btnBuy50.onClick.AddListener(() => SetPurchaseQuantity(50));
        btnBuyMax.onClick.AddListener(() => SetPurchaseQuantity(-1));
    }

    private void Start()
    {
        if (opsButton != null) opsButton.onClick.AddListener(() => ShowPanel(opsPanel, opsScroll, true));
        if (generalButton != null) generalButton.onClick.AddListener(() => ShowPanel(generalPanel, generalScroll, false));
        if (settingsButton != null) settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel, settingsScroll, false));
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenu);

        ShowPanel(opsPanel, opsScroll, true);

        if (upgradeMenuPanel != null)
            upgradeMenuPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        if (upgradeMenuPanel != null)
            upgradeMenuPanel.SetActive(true);

        RefreshAllUpgrades();
    }

    private void CloseMenu()
    {
        if (upgradeMenuPanel != null)
            upgradeMenuPanel.SetActive(false);
    }

    private void ShowPanel(GameObject panelToShow, ScrollRect scrollToReset, bool showQuantityButtons)
    {
        if (opsPanel != null) opsPanel.SetActive(false);
        if (generalPanel != null) generalPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (panelToShow != null) panelToShow.SetActive(true);
        if (scrollToReset != null) scrollToReset.verticalNormalizedPosition = 1f;

        if (quantityButtonsPanel != null)
            quantityButtonsPanel.SetActive(showQuantityButtons);

        if (showQuantityButtons)
            SetPurchaseQuantity(1);

        RefreshAllUpgrades();
    }

    public void RefreshAllUpgrades()
    {
        if (allUpgradeUIs == null) return;
        foreach (var u in allUpgradeUIs)
            if (u != null)
                u.ForceUpdateUI();
    }

    public void SetPurchaseQuantity(int quantity)
    {
        if (opsUpgradeUIs == null) return;
        foreach (var u in opsUpgradeUIs)
            if (u != null)
                u.SetQuantityToBuy(quantity);
    }
}
