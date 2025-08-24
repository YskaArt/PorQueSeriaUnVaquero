using System;
using TMPro;
using UnityEngine;

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
    private float timer;                           // Contador para sumar oro por segundo

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
    }

    // ==========================
    // MÉTODO: Update()
    // Incrementa oro automáticamente cada segundo
    // ==========================
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            gold += goldPerSecond;
            timer = 0f;
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
        string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
        int index = 0;
        while (number >= 1000 && index < suffixes.Length - 1)
        {
            number /= 1000;
            index++;
        }
        return number.ToString("0.##") + suffixes[index];
    }

    // ==========================
    // MÉTODO: SetTextReferences()
    // Permite reasignar textos desde otro script de UI (ej: UIManager)
    // ==========================
    public void SetTextReferences(TextMeshProUGUI gold, TextMeshProUGUI goldPerSecond)
    {
        goldText = gold;
        goldPerSecondText = goldPerSecond;
        UpdateGoldUI();
    }
}
