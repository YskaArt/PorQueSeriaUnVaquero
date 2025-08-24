using UnityEngine;
using UnityEngine.UI;

public class UpgradeMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject upgradeMenuPanel; // Panel que contiene todos los upgrades
    [SerializeField] private Button toggleButton;         // Botón para abrir/cerrar el panel
    [SerializeField] private Upgrade[] upgrades;          // Lista de upgrades a mostrar en la UI

    // MÉTODO: Start()
    // Inicializa el botón para abrir/cerrar el panel y lo oculta al inicio
    private void Start()
    {
        toggleButton.onClick.AddListener(ToggleMenu);
        upgradeMenuPanel.SetActive(false);
    }

    // MÉTODO: ToggleMenu()
    // Alterna la visibilidad del panel de upgrades
    private void ToggleMenu()
    {
        upgradeMenuPanel.SetActive(!upgradeMenuPanel.activeSelf);
    }

    // MÉTODO: RefreshUI()
    // Actualiza todos los elementos del panel de upgrades
    // Llama a ForceUpdateUI() de cada Upgrade para reflejar cambios en nivel o costo
    public void RefreshUI()
    {
        foreach (var upgrade in upgrades)
        {
            if (upgrade != null)
                upgrade.ForceUpdateUI();
        }
    }
}
