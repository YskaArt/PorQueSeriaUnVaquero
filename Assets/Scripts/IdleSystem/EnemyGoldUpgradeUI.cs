/*
 * EnemyGoldUpgradeUI
 * -------------------
 * UI específica del upgrade que aumenta el oro otorgado por enemigos.
 *
 * FUNCIONAMIENTO:
 * - Hereda de UpgradeUIBase<EnemyGoldUpgradeData>, por lo que utiliza su sistema
 *   de selección de cantidad, cálculo de costos y actualización visual.
 *
 * - OnLevelBought():
 *      * Se llama cuando el jugador compra niveles del upgrade.
 *      * Notifica a EnemyGoldManager para que recalcule inmediatamente la
 *        recompensa de oro por enemigo.
 *
 * - BuildDisplayStrings():
 *      * Construye los textos dinámicos del panel:
 *          - levelStr: muestra el nivel actual y una estimación del reward por enemigo.
 *          - priceStr: muestra cuántos niveles se comprarán y el costo total.
 *      * La recompensa estimada se toma desde EnemyGoldManager para coincidir
 *        con la lógica real que usa todo el sistema.
 *
 * RESPONSABILIDAD:
 * - Solo controla la visualización y actualización UI del upgrade.
 * - Añade soporte de visualización para el bonus (comprar / ocultar).
 */


using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyGoldUpgradeUI : UpgradeUIBase<EnemyGoldUpgradeData>
{
    [Header("Bonus UI (optional)")]
    [SerializeField] private GameObject bonusContainer;
    [SerializeField] private Button buyBonusButton;
    [SerializeField] private TextMeshProUGUI bonusPriceText;
    [SerializeField] private TextMeshProUGUI bonusLabel; // <-- NUEVO

    protected override void Awake()
    {
        base.Awake();
        if (buyBonusButton != null) buyBonusButton.onClick.AddListener(OnBonusBuy);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (upgradeData != null) upgradeData.OnBonusPurchased += UpdateUI;
        UpdateUI();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (upgradeData != null) upgradeData.OnBonusPurchased -= UpdateUI;
    }

    private void OnBonusBuy()
    {
        if (upgradeData == null) return;
        if (upgradeData.BuyBonus())
        {
            GameSaveManager.Instance?.SaveGame();
            EnemyGoldManager.Instance?.OnEnemyUpgradeChanged();
            UpdateUI();
        }
    }

    protected override void OnLevelBought()
    {
        EnemyGoldManager.Instance?.OnEnemyUpgradeChanged();
    }

    protected override void BuildDisplayStrings(out string levelStr, out string priceStr)
    {
        double sampleReward = EnemyGoldManager.Instance != null ?
                              EnemyGoldManager.Instance.GetEnemyGoldReward() : upgradeData.CalculateEnemyReward(0.0);
        levelStr = $"Lv. {upgradeData.currentLevel}\n<color=#888>Reward ~ {GoldManager.FormatNumber(sampleReward)}</color>";

        int displayQty = (selectedQuantity < 0) ? GetMaxAffordableLevels() : selectedQuantity;
        double total = GetTotalCostForQuantity(displayQty);
        priceStr = $"Buy {displayQty}\n{GoldManager.FormatNumber(total)}";
    }

    public override void UpdateUI()
    {
        base.UpdateUI();

        if (upgradeData == null) return;

        // --- Bonus UI ---
        if (bonusContainer != null && buyBonusButton != null && bonusPriceText != null)
        {
            bool available = upgradeData.IsBonusAvailable();
            bonusContainer.SetActive(available);

            if (available)
            {
                double cost = upgradeData.GetBonusCostFor(upgradeData.bonusCount + 1);
                bonusPriceText.text = $"{GoldManager.FormatNumber(cost)}";
                buyBonusButton.interactable = GoldManager.Instance != null && GoldManager.Instance.CurrentGold >= cost;

                if (bonusLabel != null)
                    bonusLabel.text = "BONUS";
            }
        }
    }
}
