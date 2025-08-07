using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance
    {
        get; private set;
    }
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI goldPerSecondText;
    [SerializeField] private UpgradeMenuToggle upgradeMenuToggle;

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

    private void OnEnable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged += OnGoldChanged;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= OnGoldChanged;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnGoldChanged()
    {
        upgradeMenuToggle?.RefreshUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        if (GoldManager.Instance != null)
            GoldManager.Instance.SetTextReferences(goldText, goldPerSecondText);

        upgradeMenuToggle?.RefreshUI();
    }
}
