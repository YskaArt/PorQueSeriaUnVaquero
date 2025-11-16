/*
 * UpgradeUIBase<T>
 * ----------------
 * Clase base genérica para todas las UIs de mejoras.
 *
 * FUNCIÓN GENERAL:
 * - Conecta un ScriptableObject (UpgradeBase o derivado) con su representación visual en UI.
 * - Se encarga de mostrar nombre, nivel, precio y estado del botón de compra.
 * - Gestiona las compras individuales o múltiples (1,10,50 o MAX).
 * - Escucha cambios de oro y eventos del upgrade para refrescar la visual.
 *
 * FUNCIONAMIENTO INTERNO:
 * -----------------------
 * 1) EVENTOS Y SUSCRIPCIONES:
 *    - Al activarse, se suscribe a:
 *        upgradeData.OnLevelChanged  → refresca la UI cuando cambia el nivel.
 *        GoldManager.OnGoldChanged   → refresca la UI cuando cambia el oro.
 *    - Al desactivarse, se desuscribe.
 *
 * 2) BOTÓN DE COMPRA:
 *    - OnBuyClicked() calcula la cantidad a comprar:
 *         selectedQuantity >0 → compra fija
 *         selectedQuantity <0 → compra máxima posible
 *    - Calcula el costo total progresivo.
 *    - Si hay oro suficiente, hace SpendGold() y luego llama LevelUp() la cantidad necesaria.
 *    - Llama OnLevelBought() (implementado por subclases) para aplicar efectos (ejemplo: añadir GPS).
 *    - Guarda la partida luego de la compra.
 *
 * 3) RENDER DE UI:
 *    - UpdateUI() llama a BuildDisplayStrings() para que cada subclase genere su texto.
 *    - Ajusta nombre, nivel, precio y habilita/deshabilita botón.
 *    - Llama a UpdateButtonVisuals() para actualizar barras/color de suficiente-oro.
 *
 * 4) COSTOS:
 *    - Los costos se calculan progresivamente (cada nivel cuesta más).
 *    - GetTotalCostForQuantity() suma todos los costos futuros desde el nivel actual.
 *    - GetMaxAffordableLevels() determina cuántos niveles se pueden pagar con el oro actual.
 *
 * 5) SUBCLASES:
 *    - Las subclases deben implementar:
 *          OnLevelBought()
 *          BuildDisplayStrings(out string levelStr, out string priceStr)
 *
 * NOTA:
 * - Esta clase sirve como núcleo unificado para cualquier upgrade UI (GPS, Enemigos, General, etc.).
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class UpgradeUIBase<T> : UpgradeUIBaseCommon where T : UpgradeBase
{
    [Header("Upgrade data")]
    [SerializeField] protected T upgradeData;

    [Header("UI refs")]
    [SerializeField] protected TextMeshProUGUI upgradeNameText;
    [SerializeField] protected TextMeshProUGUI levelText;
    [SerializeField] protected TextMeshProUGUI priceText;
    [SerializeField] protected Button buyButton;
    [SerializeField] protected Image fillImage;
    [SerializeField] protected Color fillColorEnough = Color.green;
    [SerializeField] protected Color fillColorNotEnough = Color.red;

    protected int selectedQuantity = 1; // 1,10,50,-1 = MAX

    protected virtual void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
    }

    protected virtual void OnEnable()
    {
        if (upgradeData != null) upgradeData.OnLevelChanged += UpdateUI;
        if (GoldManager.Instance != null) GoldManager.Instance.OnGoldChanged += UpdateUI;
        UpdateUI();
    }

    protected virtual void OnDisable()
    {
        if (upgradeData != null) upgradeData.OnLevelChanged -= UpdateUI;
        if (GoldManager.Instance != null) GoldManager.Instance.OnGoldChanged -= UpdateUI;
    }

    protected virtual void OnBuyClicked()
    {
        if (upgradeData == null || GoldManager.Instance == null) return;

        int quantity = (selectedQuantity < 0) ? GetMaxAffordableLevels() : selectedQuantity;
        if (quantity <= 0) return;

        double totalCost = GetTotalCostForQuantity(quantity);
        if (!GoldManager.Instance.SpendGold(totalCost)) return;

        for (int i = 0; i < quantity; i++)
        {
            upgradeData.LevelUp();
            OnLevelBought();
        }

        GameSaveManager.Instance?.SaveGame();
        UpdateUI();
    }

    protected virtual double GetTotalCostForQuantity(int quantity)
    {
        if (upgradeData == null || quantity <= 0) return 0;
        double total = 0;
        int lvl = upgradeData.currentLevel;
        for (int i = 0; i < quantity; i++)
        {
            total += upgradeData.baseCost * Math.Pow(upgradeData.costMultiplier, lvl);
            lvl++;
        }
        return total;
    }

    public override int GetMaxAffordableLevels()
    {
        if (upgradeData == null || GoldManager.Instance == null) return 0;
        double gold = GoldManager.Instance.CurrentGold;
        int count = 0;
        int lvl = upgradeData.currentLevel;
        while (true)
        {
            double c = upgradeData.baseCost * Math.Pow(upgradeData.costMultiplier, lvl);
            if (gold < c) break;
            gold -= c; lvl++; count++;
        }
        return count;
    }

    protected abstract void OnLevelBought();
    protected abstract void BuildDisplayStrings(out string levelStr, out string priceStr);

    public override void UpdateUI()
    {
        if (upgradeData == null) return;

        if (upgradeNameText != null)
            upgradeNameText.text = upgradeData.upgradeName;

        BuildDisplayStrings(out string levelStr, out string priceStr);
        if (levelText != null)
            levelText.text = levelStr;

        int displayQty = (selectedQuantity < 0) ? GetMaxAffordableLevels() : selectedQuantity;

        if (priceText != null)
            priceText.text = displayQty <= 0 ? "Not enough gold" : priceStr;

        UpdateButtonVisuals(displayQty);
    }

    public override void ForceUpdateUI() => UpdateUI();

    public override void SetQuantityToBuy(int q)
    {
        selectedQuantity = q;
        UpdateUI();
    }

    public override UpgradeBase GetUpgradeData() => upgradeData;

    protected virtual void UpdateButtonVisuals(int displayQty)
    {
        if (buyButton == null || GoldManager.Instance == null) return;

        if (displayQty <= 0)
        {
            buyButton.interactable = false;

            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
                fillImage.color = fillColorNotEnough;
            }
            return;
        }

        double totalCost = GetTotalCostForQuantity(displayQty);
        double gold = GoldManager.Instance.CurrentGold;
        bool canBuy = gold >= totalCost;
        buyButton.interactable = canBuy;

        if (fillImage != null)
        {
            float progress = (totalCost <= 0) ? 1f : Mathf.Clamp01((float)(gold / totalCost));
            fillImage.fillAmount = progress;
            fillImage.color = canBuy ? fillColorEnough : fillColorNotEnough;
        }
    }
}
