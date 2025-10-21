using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoldManager : MonoBehaviour
{
    // ==========================
    // SINGLETON
    // ==========================
    public static GoldManager Instance { get; private set; }

    // ==========================
    // EVENTOS
    // ==========================
    public event Action OnGoldChanged;

    // ==========================
    // REFERENCIAS DE UI
    // ==========================
    [SerializeField] private TextMeshProUGUI goldText;          // Texto que muestra el oro total
    [SerializeField] private TextMeshProUGUI goldPerSecondText; // Texto que muestra oro por segundo

    // ==========================
    // VARIABLES PRINCIPALES
    // ==========================
    [SerializeField] private double gold;          // Oro actual
    [SerializeField] private double goldPerSecond; // Oro generado automáticamente por segundo

    // Control para actualizar la UI a una cadencia razonable
    [SerializeField] private float uiUpdateInterval = 0.1f; // intervalo en segundos para refrescar UI durante progresión
    private float uiUpdateTimer = 0f;

    // Propiedad de solo lectura (acceso público al oro actual)
    public double CurrentGold => gold;

    // ==========================
    // MÉTODO: Awake()
    // Configura el singleton y asegura que no se duplique
    // ==========================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Actualiza la UI en caso de que los valores se asignaran antes de Awake
        UpdateGoldUI();
    }

    // ==========================
    // MÉTODO: Start()
    // Aplica los datos guardados una vez que los managers existen
    // ==========================
    private void Start()
    {
        // Si hay datos guardados que se cargaron antes de que existiera GoldManager, aplícalos ahora
        GameSaveManager.Instance?.ApplyLoadedDataToManagers();

        // Asegura que la UI refleje los valores aplicados
        UpdateGoldUI();
    }

    // ==========================
    // MÉTODO: OnDestroy()
    // Limpia las suscripciones al cambiar de escena
    // ==========================
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Cuando carga una escena, busca y asigna los textos automáticamente.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignUIReferences();
        UpdateGoldUI();
    }

    // ==========================
    // MÉTODO: Update()
    // Incrementa oro de forma progresiva cada frame según goldPerSecond
    // ==========================
    private void Update()
    {
        // Sumar progresivamente según el tiempo transcurrido
        if (Math.Abs(goldPerSecond) > double.Epsilon)
        {
            gold += goldPerSecond * Time.deltaTime;
        }

        // Actualizar la UI a una cadencia controlada para evitar llamadas excesivas
        uiUpdateTimer += Time.deltaTime;
        if (uiUpdateTimer >= uiUpdateInterval)
        {
            uiUpdateTimer = 0f;
            UpdateGoldUI();
            OnGoldChanged?.Invoke();
        }
    }

    // ==========================
    // MÉTODO: AddGold()
    // Suma oro inmediato (por ejemplo al matar enemigos)
    // ==========================
    public void AddGold(double amount)
    {
        gold += amount;
        UpdateGoldUI();
        OnGoldChanged?.Invoke();
    }

    // ==========================
    // MÉTODO: AddGoldPerSecond()
    // Aumenta el oro pasivo generado por segundo
    // ==========================
    public void AddGoldPerSecond(double amount)
    {
        goldPerSecond += amount;
        UpdateGoldUI();
    }

    // ==========================
    // MÉTODO: SpendGold()
    // Resta oro si hay suficiente y devuelve true, si no devuelve false
    // ==========================
    public bool SpendGold(double amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            UpdateGoldUI();
            OnGoldChanged?.Invoke();
            return true;
        }
        return false; // No hay suficiente oro
    }

    // ==========================
    // MÉTODO: UpdateGoldUI()
    // Actualiza los textos de oro y oro por segundo en pantalla
    // ==========================
    private void UpdateGoldUI()
    {
        if (goldText == null || goldPerSecondText == null)
        {
            // Buscar dinámicamente si faltan referencias
            var uiTexts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var text in uiTexts)
            {
                if (text.name == "GoldText") goldText = text;
                if (text.name == "GoldPerSecondText") goldPerSecondText = text;
            }
        }

        if (goldText != null)
            goldText.text = FormatNumber(gold);

        if (goldPerSecondText != null)
            goldPerSecondText.text = FormatNumber(goldPerSecond) + " Ops";
    }



    // ==========================
    // MÉTODO: FormatNumber()
    // Convierte números grandes en formato compacto (K, M, B, etc.)
    // ==========================
    public static string FormatNumber(double number)
    {
        string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "Sin", "De", "Ud", "Dd", "Td", "Qt", "Qd", "Sd", "St", "Od", "Nd", "Vg", "Uv", "Dv", "Tv" };
        int index = 0;
        while (number >= 1000 && index < suffixes.Length - 1)
        {
            number /= 1000;
            index++;
        }
        return number.ToString("0.#") + suffixes[index];
    }

    // ==========================
    // MÉTODO: SetTextReferences()
    // Permite reasignar textos desde otro script de UI (ej: UIManager)
    // ==========================
    private void AssignUIReferences()
    {
        if (goldText == null)
        {
            GameObject goldObj = GameObject.FindWithTag("GoldText");
            if (goldObj != null)
                goldText = goldObj.GetComponent<TextMeshProUGUI>();
        }

        if (goldPerSecondText == null)
        {
            GameObject opsObj = GameObject.FindWithTag("GoldPerSecondText");
            if (opsObj != null)
                goldPerSecondText = opsObj.GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetTextReferences(TextMeshProUGUI gold, TextMeshProUGUI goldPerSecond)
    {
        goldText = gold;
        goldPerSecondText = goldPerSecond;
        UpdateGoldUI();
    }

    public void SetGoldPerSecond(double value)
    {
        goldPerSecond = value;
        UpdateGoldUI();
    }
}

