using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    // Singleton para acceder fácilmente desde otros scripts
    public static UIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI goldText;          // Referencia al texto de oro total
    [SerializeField] private TextMeshProUGUI goldPerSecondText; // Referencia al texto de oro por segundo
    [SerializeField] private UpgradeMenuToggle upgradeMenuToggle; // Referencia al panel de mejoras

    // MÉTODO: Awake()
    // Configura el singleton y asegura que persista entre escenas.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // MÉTODO: OnEnable()
    // Se suscribe a eventos: 
    // - Cambio de oro (para actualizar UI de mejoras)
    // - Carga de escenas (para volver a referenciar los textos)
    private void OnEnable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged += OnGoldChanged;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // MÉTODO: OnDisable()
    // Desuscribe los eventos para evitar memory leaks
    private void OnDisable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= OnGoldChanged;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // MÉTODO: OnGoldChanged()
    // Se ejecuta cada vez que cambia el oro. Actualiza la UI de mejoras.
    private void OnGoldChanged()
    {
        upgradeMenuToggle?.RefreshUI();
    }

    // MÉTODO: OnSceneLoaded()
    // Se ejecuta al cargar una escena:
    // - Vuelve a asignar referencias de TextMeshPro al GoldManager
    // - Refresca la UI del panel de mejoras
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.SetTextReferences(goldText, goldPerSecondText);

        upgradeMenuToggle?.RefreshUI();
    }
}
