/// <summary>
/// Controlador base para menús UI simples.
/// Se encarga de activar/desactivar un panel al presionar un botón asociado.
/// Ideal para menús desplegables como inventarios, upgrades, ajustes, etc.
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class UIControllerBase : MonoBehaviour
{
    [SerializeField] private GameObject MenuPanel;
    [SerializeField] private Button toggleButton;

    private void Start()
    {
        toggleButton.onClick.AddListener(ToggleMenu);
        MenuPanel.SetActive(false);
    }

    public void ToggleMenu()
    {
        MenuPanel.SetActive(!MenuPanel.activeSelf);
    }
}
