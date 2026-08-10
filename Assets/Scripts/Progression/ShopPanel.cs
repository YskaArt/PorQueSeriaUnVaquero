/*
 * ShopPanel
 * ---------
 * UI de la tienda. Tres items fijos (ver ShopManager):
 * - Gold Rush (rewarded ad -> oro instantáneo)
 * - Frenzy (rewarded ad -> x2 oro temporal)
 * - Lucky Horseshoe (oro -> x1.5 oro temporal)
 *
 * WIRING EN EL EDITOR:
 * - panelRoot + closeButton.
 * - Por item: botón + texto de valor/costo (opcional).
 * - boostStatusText (opcional): "x2 Gold - 08:15 left" mientras haya boost activo.
 *
 * Conectar OpenPanel() al botón de Tienda del HUD.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Gold Rush")]
    [SerializeField] private Button goldRushButton;
    [SerializeField] private TextMeshProUGUI goldRushValueText;

    [Header("Frenzy")]
    [SerializeField] private Button frenzyButton;
    [SerializeField] private TextMeshProUGUI frenzyValueText;

    [Header("Lucky Horseshoe")]
    [SerializeField] private Button horseshoeButton;
    [SerializeField] private TextMeshProUGUI horseshoeCostText;

    [Header("Estado del boost")]
    [SerializeField] private TextMeshProUGUI boostStatusText;

    [Header("Refresco")]
    [SerializeField] private float refreshInterval = 0.5f;
    private float refreshTimer;
    private bool purchaseInProgress;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (goldRushButton != null) goldRushButton.onClick.AddListener(OnGoldRushClicked);
        if (frenzyButton != null) frenzyButton.onClick.AddListener(OnFrenzyClicked);
        if (horseshoeButton != null) horseshoeButton.onClick.AddListener(OnHorseshoeClicked);
    }

    private void Start()
    {
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnShopStateChanged += RefreshUI;

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnShopStateChanged -= RefreshUI;
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshUI();
        }
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ================== CLICKS ==================

    private void OnGoldRushClicked()
    {
        if (purchaseInProgress || ShopManager.Instance == null) return;
        purchaseInProgress = true;
        ShopManager.Instance.BuyGoldRushWithAd(_ => { purchaseInProgress = false; RefreshUI(); });
    }

    private void OnFrenzyClicked()
    {
        if (purchaseInProgress || ShopManager.Instance == null) return;
        purchaseInProgress = true;
        ShopManager.Instance.BuyFrenzyWithAd(_ => { purchaseInProgress = false; RefreshUI(); });
    }

    private void OnHorseshoeClicked()
    {
        ShopManager.Instance?.BuyHorseshoe();
        RefreshUI();
    }

    // ================== RENDER ==================

    private void RefreshUI()
    {
        var shop = ShopManager.Instance;
        if (shop == null) return;

        if (goldRushValueText != null)
            goldRushValueText.text = $"+{GoldManager.FormatNumber(shop.GetGoldRushAmount())} Gold";

        if (frenzyValueText != null)
            frenzyValueText.text = $"x{shop.FrenzyMultiplier:0.#} Gold / {FormatMinutes(shop.FrenzyDurationSeconds)}";

        if (horseshoeCostText != null)
        {
            double cost = shop.GetHorseshoeCost();
            horseshoeCostText.text =
                $"x{shop.HorseshoeMultiplier:0.#} Gold / {FormatMinutes(shop.HorseshoeDurationSeconds)}\n" +
                $"Cost: {GoldManager.FormatNumber(cost)}";

            if (horseshoeButton != null)
                horseshoeButton.interactable = GoldManager.Instance != null &&
                                               GoldManager.Instance.CurrentGold >= cost;
        }

        bool adsBusy = purchaseInProgress;
        if (goldRushButton != null) goldRushButton.interactable = !adsBusy;
        if (frenzyButton != null) frenzyButton.interactable = !adsBusy;

        if (boostStatusText != null)
        {
            if (shop.BoostActive)
            {
                float s = shop.BoostRemainingSeconds;
                boostStatusText.text = $"x{shop.ActiveBoostMultiplier:0.#} Gold active - {(int)(s / 60):00}:{(int)(s % 60):00} left";
                boostStatusText.gameObject.SetActive(true);
            }
            else
            {
                boostStatusText.gameObject.SetActive(false);
            }
        }
    }

    private static string FormatMinutes(float seconds) => $"{Mathf.RoundToInt(seconds / 60f)} min";
}
