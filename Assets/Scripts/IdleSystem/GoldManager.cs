/*
 * GoldManager
 * -----------
 * Sistema central de administración del oro en el juego.
 *
 * FUNCIONAMIENTO GENERAL:
 * - Singleton persistente entre escenas (DontDestroyOnLoad).
 * - Maneja el oro total (gold) y el oro pasivo por segundo (goldPerSecond).
 * - Incrementa el oro progresivamente usando Time.deltaTime.
 * - Notifica cambios mediante el evento OnGoldChanged, para que otros sistemas
 *   (upgrades, recompensas, managers) reaccionen cuando el oro o el GPS cambian.
 *
 * UI:
 * - Actualiza el texto de oro y GPS con un intervalo fijo (uiUpdateInterval)
 *   para evitar refrescar la UI cada frame.
 * - Si las referencias de UI no existen al cargar una escena, intenta reasignarlas
 *   automáticamente mediante búsqueda por tag o por nombre.
 *
 * MÉTODOS PRINCIPALES:
 * - AddGold(amount): suma oro instantáneo (ej: enemigos muertos).
 * - SpendGold(amount): resta oro si hay suficiente; devuelve true/false.
 * - AddGoldPerSecond(amount): incrementa la ganancia pasiva.
 * - SetGoldPerSecond(value): asigna nuevo GPS directo.
 * - FormatNumber(number): compacta números grandes (K, M, B, etc.).
 *
 * PERSISTENCIA Y ESCENAS:
 * - Usa SceneManager.sceneLoaded para restaurar referencias al cambiar de escena.
 * - Colabora con GameSaveManager para aplicar datos previamente cargados.
 *
 * RESPONSABILIDAD ÚNICA:
 * - Este manager es la autoridad del oro y del GPS en todo el juego.
 * - Ningún otro sistema debería modificar el oro directamente.
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    public event Action OnGoldChanged;

    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI goldPerSecondText;

    [SerializeField] private double gold;
    [SerializeField] private double goldPerSecond;

    [SerializeField] private float uiUpdateInterval = 0.1f;
    private float uiUpdateTimer = 0f;

    public double CurrentGold => gold;
    public double CurrentGoldPerSecond => goldPerSecond;

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

        UpdateGoldUI();
    }

    private void Start()
    {
        GameSaveManager.Instance?.ApplyLoadedDataToManagers();
        UpdateGoldUI();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignUIReferences();
        UpdateGoldUI();
    }

    private void Update()
    {
        if (Math.Abs(goldPerSecond) > double.Epsilon)
            gold += goldPerSecond * Time.deltaTime;

        uiUpdateTimer += Time.deltaTime;
        if (uiUpdateTimer >= uiUpdateInterval)
        {
            uiUpdateTimer = 0f;
            UpdateGoldUI();
            OnGoldChanged?.Invoke();
        }
    }

    public void AddGold(double amount)
    {
        gold += amount;
        UpdateGoldUI();
        OnGoldChanged?.Invoke();
    }

    public void AddGoldPerSecond(double amount)
    {
        goldPerSecond += amount;
        UpdateGoldUI();
    }

    public bool SpendGold(double amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            UpdateGoldUI();
            OnGoldChanged?.Invoke();
            return true;
        }
        return false;
    }

    private void UpdateGoldUI()
    {
        if (goldText == null || goldPerSecondText == null)
        {
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
            goldPerSecondText.text = FormatNumber(goldPerSecond) + " Gps";
    }

    public static string FormatNumber(double number)
    {
        string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "Sin", "De",
                              "Ud", "Dd", "Td", "Qt", "Qd", "Sd", "St", "Od", "Nd", "Vg", "Uv", "Dv", "Tv" };
        int index = 0;

        while (number >= 1000 && index < suffixes.Length - 1)
        {
            number /= 1000;
            index++;
        }

        return number.ToString("0.#") + suffixes[index];
    }

    private void AssignUIReferences()
    {
        if (goldText == null)
        {
            var obj = GameObject.FindWithTag("GoldText");
            if (obj != null) goldText = obj.GetComponent<TextMeshProUGUI>();
        }

        if (goldPerSecondText == null)
        {
            var obj = GameObject.FindWithTag("GoldPerSecondText");
            if (obj != null) goldPerSecondText = obj.GetComponent<TextMeshProUGUI>();
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
