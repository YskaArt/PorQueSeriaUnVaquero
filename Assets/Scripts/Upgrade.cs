using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Upgrade : MonoBehaviour
{
    [SerializeField] private UpgradeData upgradeData;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button upgradeButton;

    private void Start()
    {
        UpdateUI();
        upgradeButton.onClick.AddListener(BuyUpgrade);
    }

    private void BuyUpgrade()
    {
        double cost = upgradeData.GetCost();
        if (GoldManager.Instance.CurrentGold >= cost)
        {
            GoldManager.Instance.AddGold(-cost);
            upgradeData.LevelUp();
            GoldManager.Instance.AddGoldPerSecond(upgradeData.goldPerSecondPerLevel);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        levelText.text = $"Lv. {upgradeData.currentLevel}\n<color=#888>+{upgradeData.goldPerSecondPerLevel} gold/s</color>";
        costText.text = GoldManager.FormatNumber(upgradeData.GetCost()) + " G";
    }
}
