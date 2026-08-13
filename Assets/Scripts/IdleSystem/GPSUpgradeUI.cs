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
 * - Ahora añade soporte visual para mostrar/ocultar un botón de BONUS que se
 *   habilita cuando IsBonusAvailable() está a true en el UpgradeBase.
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
 * - No calcula el GPS total acumulado ni maneja bonus: eso lo define UpgradeBase/GPSUpgradeData.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GPSUpgradeUI : UpgradeUIBase<GPSUpgradeData>
{
    [Header("Bonus UI (optional)")]
    [SerializeField] private GameObject bonusContainer;   // panel que contiene el botón de bonus
    [SerializeField] private Button buyBonusButton;
    [SerializeField] private TextMeshProUGUI bonusPriceText;
    [SerializeField] private TextMeshProUGUI bonusLabel; // <-- NUEVO: referencia al texto que indica "BONUS"
    [SerializeField] private Image bonusFillImage;        // <-- NUEVO: barra de progreso hacia poder comprarlo (Image Type = Filled)

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

        // Calculate previous multiplier and base GPS so we can apply the delta after buying
        double prevMultiplier = upgradeData.GetTotalBonusMultiplier();
        double baseGPS = upgradeData.GetBaseGPS();

        if (upgradeData.BuyBonus())
        {
            // After purchase, compute new multiplier and apply delta to GoldManager
            double newMultiplier = upgradeData.GetTotalBonusMultiplier();
            double deltaGPS = baseGPS * (newMultiplier - prevMultiplier);

            if (deltaGPS != 0 && GoldManager.Instance != null)
                GoldManager.Instance.AddGoldPerSecond(deltaGPS);

            GameSaveManager.Instance?.SaveGame();
            UpdateUI();
        }
    }

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

    public override void UpdateUI()
    {
        base.UpdateUI();

        if (upgradeData == null) return;

        // --- Bonus UI ---
        if (bonusContainer != null && buyBonusButton != null && bonusPriceText != null)
        {
            bool supportsBonus = upgradeData.HasBonus() || (upgradeData.bonusInterval > 0 && upgradeData.bonusCost > 0);

            if (!supportsBonus)
            {
                bonusContainer.SetActive(false);
                return;
            }

            bool available = upgradeData.IsBonusAvailable();
            double cost = upgradeData.GetBonusCostFor(upgradeData.bonusCount + 1);
            bool affordable = GoldManager.Instance != null && GoldManager.Instance.CurrentGold >= cost;

            // Mostrar el UI de bonus apenas esté DESBLOQUEADO por nivel (10, 50, 100...),
            // sin importar si ya lo podés pagar. La asequibilidad solo decide si el botón
            // está habilitado o no -- antes se ocultaba el panel entero hasta poder pagarlo,
            // lo cual hacía parecer que el desbloqueo por nivel no funcionaba (aparecía
            // recién cuando juntabas oro suficiente, coincidiendo o no con el nivel real).
            bonusContainer.SetActive(available);

            if (available)
            {
                bonusPriceText.text = $"Buy: {GoldManager.FormatNumber(cost)}";
                buyBonusButton.interactable = affordable;
                if (bonusLabel != null)
                {
                    // Mostrar el nombre de la mejora seguido de " Bonus X<multiplier>"
                    string multStr = upgradeData.bonusMultiplierPerBonus % 1 == 0 ?
                        ((int)upgradeData.bonusMultiplierPerBonus).ToString() :
                        upgradeData.bonusMultiplierPerBonus.ToString("0.#");

                    bonusLabel.text = $"{upgradeData.upgradeName} Bonus X{multStr}";
                }

                // Igual que en las mejoras normales (UpgradeUIBase.UpdateButtonVisuals):
                // la barra se llena según cuánto oro tenés respecto al costo del bonus.
                if (bonusFillImage != null && GoldManager.Instance != null)
                {
                    float progress = (cost <= 0) ? 1f : Mathf.Clamp01((float)(GoldManager.Instance.CurrentGold / cost));
                    bonusFillImage.fillAmount = progress;
                    bonusFillImage.color = affordable ? fillColorEnough : fillColorNotEnough;
                }
            }
            else
            {
                // mantener el botón desactivado cuando está oculto o no se puede comprar
                buyBonusButton.interactable = false;

                if (bonusFillImage != null)
                {
                    bonusFillImage.fillAmount = 0f;
                    bonusFillImage.color = fillColorNotEnough;
                }
            }
        }
    }
}