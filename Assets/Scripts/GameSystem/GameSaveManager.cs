using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona el guardado y carga de progreso del jugador:
/// - Niveles de upgrades (UpgradeData)
/// - Cantidad de oro (GoldManager)
/// Persistencia mediante PlayerPrefs + JSON
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Upgrades del Juego")]
    [SerializeField] private UpgradeData[] allUpgrades; // Todas las mejoras definidas en el juego

    private const string SaveKey = "IdleGameSave"; // Clave en PlayerPrefs

    // ==========================
    // MÉTODO: Awake()
    // Configura Singleton y asegura persistencia entre escenas
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
    // MÉTODO: Start()
    // Carga el progreso al inicio del juego
    // ==========================
    private void Start()
    {
        LoadGame();
    }

    // ==========================
    // MÉTODO: SaveGame()
    // Guarda oro y niveles de todas las mejoras en JSON
    // ==========================
    public void SaveGame()
    {
        if (GoldManager.Instance == null) return;

        GameSaveData saveData = new GameSaveData
        {
            gold = GoldManager.Instance.CurrentGold,
            upgrades = new List<UpgradeSaveData>()
        };

        foreach (var upgrade in allUpgrades)
        {
            saveData.upgrades.Add(new UpgradeSaveData
            {
                upgradeName = upgrade.upgradeName,
                currentLevel = upgrade.currentLevel
            });
        }

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        Debug.Log("✅ Juego guardado: " + json);
    }

    // ==========================
    // MÉTODO: LoadGame()
    // Carga oro y niveles desde PlayerPrefs si existe guardado
    // ==========================
    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("ℹ No hay datos guardados.");
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        if (saveData == null)
        {
            Debug.LogWarning("⚠ Error al cargar datos.");
            return;
        }

        // Restaurar oro
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(saveData.gold - GoldManager.Instance.CurrentGold);
        }

        // Restaurar niveles y recalcular OPS total
        double totalOPS = 0;

        foreach (var savedUpgrade in saveData.upgrades)
        {
            foreach (var upgrade in allUpgrades)
            {
                if (upgrade.upgradeName == savedUpgrade.upgradeName)
                {
                    upgrade.currentLevel = savedUpgrade.currentLevel;

                    // Sumar OPS total según el nivel actual
                    totalOPS += upgrade.goldPerSecondPerLevel * upgrade.currentLevel;
                }
            }
        }

        // Establecer OPS correcto (reseteando antes)
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.SetGoldPerSecond(totalOPS);
        }

        Debug.Log("✅ Juego cargado: " + json);
    }


    // ==========================
    // MÉTODO: ResetGame()
    // Limpia el progreso (para debug o reinicio manual)
    // ==========================
    public void ResetGame()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("🗑 Progreso borrado.");
    }

    // ==========================
    // EVENTO: OnApplicationQuit()
    // Guarda automáticamente al cerrar el juego
    // ==========================
    private void OnApplicationQuit()
    {
        SaveGame();
    }
}

/// <summary>
/// Clase raíz del guardado (JSON)
/// </summary>
[Serializable]
public class GameSaveData
{
    public double gold;
    public List<UpgradeSaveData> upgrades;
}

/// <summary>
/// Datos individuales de cada upgrade
/// </summary>
[Serializable]
public class UpgradeSaveData
{
    public string upgradeName;
    public int currentLevel;
}
