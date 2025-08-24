using UnityEngine;
using UnityEngine.UI;

public class UpgradeMenuController : MonoBehaviour
{
    // ==========================
    // PANEL PRINCIPAL
    // ==========================
    [Header("Panel Principal")]
    [SerializeField] private GameObject upgradeMenuPanel; // Panel raíz del menú de mejoras

    // ==========================
    // BOTONES DE NAVEGACIÓN
    // ==========================
    [Header("Botones de Navegación")]
    [SerializeField] private Button opsButton;      // Ir a "Mejoras OPS"
    [SerializeField] private Button generalButton;  // Ir a "Mejoras Generales"
    [SerializeField] private Button settingsButton; // Ir a "Opciones"
    [SerializeField] private Button closeButton;    // Cerrar menú

    // ==========================
    // SCROLLS (UNO POR SECCIÓN)
    // ==========================
    [Header("Scrolls por Sección")]
    [SerializeField] private GameObject opsPanel;       // Panel con Scroll OPS
    [SerializeField] private GameObject generalPanel;   // Panel con Scroll General
    [SerializeField] private GameObject settingsPanel;  // Panel con Scroll Settings

    [SerializeField] private ScrollRect opsScroll;      // ScrollRect OPS
    [SerializeField] private ScrollRect generalScroll;  // ScrollRect General
    [SerializeField] private ScrollRect settingsScroll; // ScrollRect Settings

    // ==========================
    // BOTONES DE CANTIDAD (SOLO OPS)
    // ==========================
    [Header("Botones de Cantidad (solo OPS)")]
    [SerializeField] private GameObject quantityButtonsPanel; // Panel con 1x, 10x, 50x, MAX
    [SerializeField] private Button btnBuy1;
    [SerializeField] private Button btnBuy10;
    [SerializeField] private Button btnBuy50;
    [SerializeField] private Button btnBuyMax;
    // LISTAS DE UPGRADES
    // ==========================
    [Header("Upgrades")]
    [SerializeField] private Upgrade[] allUpgrades; // Para refrescar UI global
    [SerializeField] private Upgrade[] opsUpgrades; // Solo los de OPS (para cambiar cantidad)

    // ==========================
    // MÉTODO: Start()
    // Configura listeners y muestra la sección inicial
    // ==========================
    private void Awake()
    {
      
        // Botones de cantidad de compra Ops
        btnBuy1.onClick.AddListener(() => SetPurchaseQuantity(1));
        btnBuy10.onClick.AddListener(() => SetPurchaseQuantity(10));
        btnBuy50.onClick.AddListener(() => SetPurchaseQuantity(50));
        btnBuyMax.onClick.AddListener(() => SetPurchaseQuantity(-1)); // -1 = MAX
    }
    private void Start()
    {
        if (opsButton != null) opsButton.onClick.AddListener(() => ShowPanel(opsPanel, opsScroll, true));
        if (generalButton != null) generalButton.onClick.AddListener(() => ShowPanel(generalPanel, generalScroll, false));
        if (settingsButton != null) settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel, settingsScroll, false));
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenu);

        // Panel inicial: OPS
        ShowPanel(opsPanel, opsScroll, true);

        // Menú oculto al inicio (opcional)
        if (upgradeMenuPanel != null)
            upgradeMenuPanel.SetActive(false);
    }

    // ==========================
    // MÉTODO: OpenMenu()
    // Activa el panel principal y refresca la UI
    // ==========================
    public void OpenMenu()
    {
        if (upgradeMenuPanel != null)
            upgradeMenuPanel.SetActive(true);

        RefreshAllUpgrades();
    }

    // ==========================
    // MÉTODO: CloseMenu()
    // Oculta el panel principal
    // ==========================
    private void CloseMenu()
    {
        if (upgradeMenuPanel != null)
            upgradeMenuPanel.SetActive(false);
    }

    // ==========================
    // MÉTODO: ShowPanel()
    // Activa solo el panel indicado y resetea su scroll
    // ==========================
    private void ShowPanel(GameObject panelToShow, ScrollRect scrollToReset, bool showQuantityButtons)
    {
        // Ocultar todos
        if (opsPanel != null) opsPanel.SetActive(false);
        if (generalPanel != null) generalPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Mostrar el seleccionado
        if (panelToShow != null) panelToShow.SetActive(true);

        // Reset scroll
        if (scrollToReset != null)
            scrollToReset.verticalNormalizedPosition = 1f;

        // Botones de cantidad
        if (quantityButtonsPanel != null)
            quantityButtonsPanel.SetActive(showQuantityButtons);

        // Si es OPS, reseteamos a 1x para evitar confusión
        if (showQuantityButtons)
            SetPurchaseQuantity(1);

        RefreshAllUpgrades();
    }

    // ==========================
    // MÉTODO: RefreshAllUpgrades()
    // Refresca todos los upgrades visibles
    // ==========================
    private void RefreshAllUpgrades()
    {
        if (allUpgrades == null) return;

        foreach (var upg in allUpgrades)
            if (upg != null) upg.ForceUpdateUI();
    }

    // ==========================
    // MÉTODO: SetPurchaseQuantity()
    // Ajusta la cantidad para TODAS las mejoras OPS
    // Vincular con los botones 1x, 10x, 50x, MAX
    // ==========================
    public void SetPurchaseQuantity(int quantity)
    {
        if (opsUpgrades == null) return;

        foreach (var upg in opsUpgrades)
            if (upg != null) upg.SetQuantityToBuy(quantity);
    }
}
