using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private const string TutorialSeenKey = "TutorialSeen";

    [Header("Páginas del Tutorial")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;

    [Header("Contenedor general del tutorial")]
    [SerializeField] private GameObject tutorialPanel;

    private void Start()
    {
        // Mostrar tutorial solo la primera vez
        if (!HasSeenTutorial())
        {
            ShowTutorial();
        }
        else
        {
            tutorialPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra el panel de tutorial.
    /// </summary>
    public void ShowTutorial()
    {
        tutorialPanel.SetActive(true);
        ShowPage1();

       
    
    }

    private bool HasSeenTutorial()
    {
        return PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;
    }

    /// <summary>
    /// Botón: Ir a la página 1
  
    public void ShowPage1()
    {
        page1.SetActive(true);
        page2.SetActive(false);
    }

    /// <summary>
    /// Botón: Ir a la página 2
    /// </summary>
    public void ShowPage2()
    {
        page1.SetActive(false);
        page2.SetActive(true);
    }

    /// <summary>
    /// Botón: Cerrar tutorial
    /// </summary>
    public void CloseTutorial()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
        tutorialPanel.SetActive(false);
    }
}
