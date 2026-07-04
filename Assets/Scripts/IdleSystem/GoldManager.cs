/*
 * GoldManager
 * -----------
 * Sistema central de administraci�n del oro en el juego.
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
 *   autom�ticamente mediante b�squeda por tag o por nombre.
 *
 * M�TODOS PRINCIPALES:
 * - AddGold(amount): suma oro instant�neo (ej: enemigos muertos).
 * - SpendGold(amount): resta oro si hay suficiente; devuelve true/false.
 * - AddGoldPerSecond(amount): incrementa la ganancia pasiva.
 * - SetGoldPerSecond(value): asigna nuevo GPS directo.
 * - FormatNumber(number): compacta n�meros grandes (K, M, B, etc.).
 *
 * PERSISTENCIA Y ESCENAS:
 * - Usa SceneManager.sceneLoaded para restaurar referencias al cambiar de escena.
 * - Colabora con GameSaveManager para aplicar datos previamente cargados.
 *
 * RESPONSABILIDAD �NICA:
 * - Este manager es la autoridad del oro y del GPS en todo el juego.
 * - Ning�n otro sistema deber�a modificar el oro directamente.
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

    // Oro total ganado en la run actual (se reinicia con el prestigio).
    // Lo usa MasteryManager para calcular los puntos de maestría a otorgar.
    private double lifetimeGoldThisRun;

    // Multiplicadores externos sobre TODO el oro ganado (no afectan gastos ni cargas de save).
    // masteryMultiplier: bonificación permanente por puntos de maestría.
    // boostMultiplier: boosts temporales de la tienda.
    private double masteryMultiplier = 1.0;
    private double boostMultiplier = 1.0;

    public double CurrentGold => gold;
    public double CurrentGoldPerSecond => goldPerSecond;
    public double LifetimeGoldThisRun => lifetimeGoldThisRun;
    public double TotalEarningsMultiplier => masteryMultiplier * boostMultiplier;

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
        {
            double earned = goldPerSecond * TotalEarningsMultiplier * Time.deltaTime;
            gold += earned;
            if (earned > 0)
                RegisterEarnings(earned);
        }

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
        if (amount > 0)
        {
            amount *= TotalEarningsMultiplier;
            RegisterEarnings(amount);
        }

        gold += amount;
        UpdateGoldUI();
        OnGoldChanged?.Invoke();
    }

    // Registra oro ganado para maestría y misiones. Solo ganancias reales,
    // nunca cargas de save (esas entran por SetGold).
    private void RegisterEarnings(double earned)
    {
        lifetimeGoldThisRun += earned;
        DailyMissionManager.Instance?.ReportProgress(MissionType.EarnGold, earned);
    }

    /// <summary>
    /// Asigna el oro directamente, sin multiplicadores ni registro de ganancias.
    /// Uso exclusivo de GameSaveManager (carga de save y resets).
    /// </summary>
    public void SetGold(double value)
    {
        gold = value;
        UpdateGoldUI();
        OnGoldChanged?.Invoke();
    }

    /// <summary>Restaura el acumulado de la run desde el save (o 0 en resets).</summary>
    public void SetLifetimeGoldThisRun(double value)
    {
        lifetimeGoldThisRun = Math.Max(0, value);
    }

    public void SetMasteryMultiplier(double multiplier)
    {
        masteryMultiplier = Math.Max(1.0, multiplier);
        UpdateGoldUI();
    }

    public void SetBoostMultiplier(double multiplier)
    {
        boostMultiplier = Math.Max(1.0, multiplier);
        UpdateGoldUI();
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
            var uiTexts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                 FindObjectsSortMode.None
                    );

            foreach (var text in uiTexts)
            {
                if (text.name == "GoldText") goldText = text;
                if (text.name == "GoldPerSecondText") goldPerSecondText = text;
            }
        }

        if (goldText != null)
            goldText.text = FormatNumber(gold);

        if (goldPerSecondText != null)
        {
            // Mostrar el texto del GPS s�lo si es mayor que 0
            bool shouldShow = Math.Abs(goldPerSecond) > double.Epsilon && goldPerSecond > 0.0;
            if (goldPerSecondText.gameObject.activeSelf != shouldShow)
                goldPerSecondText.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                // Mostrar el GPS efectivo (con maestría y boosts aplicados)
                goldPerSecondText.text = FormatNumber(goldPerSecond * TotalEarningsMultiplier) + " Gps";
            }
        }
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
