/// <summary>
/// Controlador base para menús UI simples.
/// Se encarga de activar/desactivar un panel al presionar un botón asociado.
/// Ideal para menús desplegables como inventarios, upgrades, ajustes, etc.
///
/// FIX: el listener del botón se registra en Awake() en vez de Start().
/// Motivo: si se registra en Start(), cualquier toque que llegue ANTES de que
/// este componente en particular ejecute su Start() (por ejemplo, mientras la
/// escena todavía está terminando de inicializar todos sus objetos) no hace
/// nada -- no hay nada bloqueando el toque, simplemente todavía no existe el
/// listener que lo atienda. Awake() se ejecuta antes y de forma más confiable
/// (todos los Awake() de la escena terminan antes de que arranque cualquier
/// Start()), así que el botón queda funcional mucho antes.
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class UIControllerBase : MonoBehaviour
{
    [SerializeField] private GameObject MenuPanel;
    [SerializeField] private Button toggleButton;

    protected virtual void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleMenu);

        if (MenuPanel != null)
            MenuPanel.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (MenuPanel != null) 
        {
            MenuPanel.SetActive(!MenuPanel.activeSelf);

            Debug.Log("Se pulso el boton");
                };
                
    }
}