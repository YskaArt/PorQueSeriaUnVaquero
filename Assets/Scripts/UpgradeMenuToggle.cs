using UnityEngine;
using UnityEngine.UI;

public class UpgradeMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject upgradeMenuPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Upgrade[] upgrades;

    private void Start()
    {
        toggleButton.onClick.AddListener(ToggleMenu);
        upgradeMenuPanel.SetActive(false);
    }

    private void ToggleMenu()
    {
        upgradeMenuPanel.SetActive(!upgradeMenuPanel.activeSelf);
    }

    public void RefreshUI()
    {
        foreach (var upgrade in upgrades)
        {
            if (upgrade != null)
                upgrade.ForceUpdateUI();
        }
    }
}
