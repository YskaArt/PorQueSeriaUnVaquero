using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Upgrade : MonoBehaviour
{
    // ==========================
    // DATOS (SCRIPTABLE OBJECT)
    // ==========================
    [Header("Datos de la Mejora (ScriptableObject)")]
    [SerializeField] private UpgradeData upgradeData;
    // Contiene: upgradeName, baseCost, costMultiplier, goldPerSecondPerLevel, currentLevel

    // ==========================
    // REFERENCIAS DE UI (TMP + BOTÓN)
    // ==========================
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI upgradeNameText;   // Nombre visible
    [SerializeField] private TextMeshProUGUI levelText;         // Nivel actual + hint de OPS
    [SerializeField] private TextMeshProUGUI priceText;         // Costo actual / acumulado según cantidad
    [SerializeField] private Button upgradeButton;              // Botón para comprar
    [SerializeField] private Image fillImage;                   // Imagen tipo Filled (progreso hacia el costo)

    [Header("Colores de la barra de progreso")]
    [SerializeField] private Color fillColorNotEnough = Color.red;
    [SerializeField] private Color fillColorEnough = Color.green;

    // ==========================
    // ESTADO INTERNO
    // ==========================
    private int selectedQuantity = 1; // Cantidad a comprar (1, 10, 50 o "MAX" -> se calcula)

    // ==========================
    // MÉTODO: Awake()
    // Suscribe eventos necesarios cuando el objeto se active
    // ==========================
    private void Awake()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(TryBuySelectedQuantity);
    }

    // ==========================
    // MÉTODO: OnEnable() / OnDisable()
    // Maneja suscripciones a eventos (oro y nivel cambiado)
    // ==========================
    private void OnEnable()
    {
        if (upgradeData != null)
        {
            upgradeData.OnLevelChanged += UpdateUI;
            upgradeData.OnBonusPurchased += OnBonusPurchased;
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged += UpdateUI;

        UpdateUI();
    }

    private void OnDisable()
    {
        if (upgradeData != null)
        {
            upgradeData.OnLevelChanged -= UpdateUI;
            upgradeData.OnBonusPurchased -= OnBonusPurchased;
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= UpdateUI;
    }

    // ==========================
    // MÉTODO: TryBuySelectedQuantity()
    // Intenta comprar la cantidad seleccionada (1, 10, 50, MAX)
    // ==========================
    private void TryBuySelectedQuantity()
    {
        if (upgradeData == null || GoldManager.Instance == null)
            return;

        int quantity = (selectedQuantity < 0) ? GetMaxAffordableLevels() : selectedQuantity;
        if (quantity <= 0) return;

        double totalCost = GetTotalCost(quantity);
        if (GoldManager.Instance.SpendGold(totalCost))
        {
            // Sube niveles y aplica OPS por cada nivel comprado
            for (int i = 0; i < quantity; i++)
            {
                upgradeData.LevelUp();
                GoldManager.Instance.AddGoldPerSecond(upgradeData.goldPerSecondPerLevel);
                GameSaveManager.Instance.SaveGame();
            }
            UpdateUI();
        }
    }

    private void OnBonusPurchased()
    {
        double baseOps = upgradeData.goldPerSecondPerLevel * upgradeData.currentLevel;
        double effectiveOps = upgradeData.GetEffectiveOPS();
        double delta = effectiveOps - baseOps;
        if (delta > 0)
            GoldManager.Instance.AddGoldPerSecond(delta);


        UpdateUI();
    }

    // ==========================
    // MÉTODO: SetQuantityToBuy()
    // Cambia la cantidad deseada de compra: 1, 10, 50 o -1 (MAX)
    // ==========================
    public void SetQuantityToBuy(int quantity)
    {
        selectedQuantity = quantity; // -1 = MAX, se resuelve al vuelo
        UpdateUI();
    }

    // ==========================
    // MÉTODO: GetTotalCost()
    // Calcula el costo acumulado para N niveles, desde el nivel actual del SO
    // ==========================
    public double GetTotalCost(int quantity)
    {
        if (upgradeData == null || quantity <= 0) return 0;

        double total = 0;
        int tempLevel = upgradeData.currentLevel;

        for (int i = 0; i < quantity; i++)
        {
            double nextCost = upgradeData.baseCost * Mathf.Pow((float)upgradeData.costMultiplier, tempLevel);
            total += nextCost;
            tempLevel++;
        }

        return total;
    }

    // ==========================
    // MÉTODO: GetMaxAffordableLevels()
    // Devuelve cuántos niveles se pueden comprar con el oro actual
    // ==========================
    public int GetMaxAffordableLevels()
    {
        if (upgradeData == null || GoldManager.Instance == null) return 0;

        double gold = GoldManager.Instance.CurrentGold;
        int count = 0;
        int tempLevel = upgradeData.currentLevel;

        while (true)
        {
            double nextCost = upgradeData.baseCost * Mathf.Pow((float)upgradeData.costMultiplier, tempLevel);
            if (gold < nextCost) break;
            gold -= nextCost;
            tempLevel++;
            count++;
        }
        return count;
    }

    // ==========================
    // MÉTODO: UpdateUI()
    // Actualiza textos, estado del botón y barra de progreso
    // ==========================
    public void UpdateUI()
    {
        if (upgradeData == null) return;

        // Nombre y nivel
        if (upgradeNameText != null)
            upgradeNameText.text = upgradeData.upgradeName;

        if (levelText != null)
        {
            string opsFormatted = GoldManager.FormatNumber(upgradeData.goldPerSecondPerLevel);
            levelText.text = $"Lv. {upgradeData.currentLevel}\n<color=#888>+{opsFormatted} OPS</color>";
        }

        // Cantidad efectiva a mostrar (si es MAX, calcular en vivo)
        int displayQty = (selectedQuantity < 0) ? GetMaxAffordableLevels() : selectedQuantity;

        // Texto de precio / preview de compra
        if (priceText != null)
        {
            if (displayQty <= 0)
            {
                priceText.text = "No alcanzas niveles";
            }
            else
            {
                double totalCost = GetTotalCost(displayQty);
                string costFormatted = GoldManager.FormatNumber(totalCost);
                priceText.text = $"Buy {displayQty}\n {costFormatted} ";
            }
        }

        // Estado del botón + barra de progreso
        UpdateButtonVisuals(displayQty);
    }

    // ==========================
    // MÉTODO: UpdateButtonVisuals()
    // Controla interactuable y fill del botón según oro/costo
    // ==========================
    private void UpdateButtonVisuals(int displayQty)
    {
        if (upgradeButton == null || GoldManager.Instance == null) return;

        if (displayQty <= 0)
        {
            upgradeButton.interactable = false;
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
                fillImage.color = fillColorNotEnough;
            }
            return;
        }

        double totalCost = GetTotalCost(displayQty);
        double gold = GoldManager.Instance.CurrentGold;

        bool canBuy = gold >= totalCost;
        upgradeButton.interactable = canBuy;

        if (fillImage != null)
        {
            float progress = (totalCost <= 0) ? 1f : Mathf.Clamp01((float)(gold / totalCost));
            fillImage.fillAmount = progress;
            fillImage.color = canBuy ? fillColorEnough : fillColorNotEnough;
        }
    }

    // ==========================
    // MÉTODO: ForceUpdateUI()
    // Permite a otros scripts refrescar a demanda
    // ==========================
    public void ForceUpdateUI()
    {
        UpdateUI();
    }

    // New: Expose UpgradeData so other managers can read properties
    public UpgradeData GetUpgradeData()
    {
        return upgradeData;
    }
}
