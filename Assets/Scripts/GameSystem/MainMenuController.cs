using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string defaultScene = "Desert";

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            LoadGame();
        }
    }

    private void LoadGame()
    {
        string lastScene = null;

        if (GameSaveManager.Instance != null)
        {
            lastScene = GameSaveManager.Instance.GetLastScene();
        }

        string sceneToLoad = !string.IsNullOrEmpty(lastScene) ? lastScene : defaultScene;
        SceneManager.LoadScene(sceneToLoad);
    }
}
