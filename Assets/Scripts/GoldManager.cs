using TMPro;
using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance
    {
        get; private set;
    }

    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI goldPerSecondText;

    [SerializeField] private double gold;
    [SerializeField] private double goldPerSecond;
    private float timer;

    public double CurrentGold => gold;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            gold += goldPerSecond;
            timer = 0f;
            UpdateGoldUI();
        }
    }

    public void AddGold(double amount)
    {
        gold += amount;
        UpdateGoldUI();
    }

    public void AddGoldPerSecond(double amount)
    {
        goldPerSecond += amount;
        UpdateGoldUI();
    }

    private void UpdateGoldUI()
    {
        goldText.text = FormatNumber(gold);
        goldPerSecondText.text = FormatNumber(goldPerSecond) + "/s";
    }

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
}
