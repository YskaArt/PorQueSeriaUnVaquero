using UnityEngine;
using UnityEngine.UI;

public class UpgradeMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject upgradeMenuPanel;
    [SerializeField] private Button toggleButton;

    private void Start()
    {
        toggleButton.onClick.AddListener(ToggleMenu);
        upgradeMenuPanel.SetActive(false);
    }

    private void ToggleMenu()
    {
        upgradeMenuPanel.SetActive(!upgradeMenuPanel.activeSelf);
    }
}
