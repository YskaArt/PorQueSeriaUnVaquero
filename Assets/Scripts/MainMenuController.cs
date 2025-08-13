using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Desert"; // Nombre de la escena a cargar

    private void Update()
    {
        // Detecta clic del mouse (botón izquierdo) o toque en pantalla
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            LoadGame();
        }
    }

    private void LoadGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
