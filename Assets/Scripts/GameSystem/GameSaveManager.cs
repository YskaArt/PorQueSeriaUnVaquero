using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona el guardado y carga de progreso del jugador:
/// - Niveles de upgrades
/// - Cantidad de oro
/// - Última escena jugada
/// - Tiempo restante para minijuego
/// Persistencia mediante PlayerPrefs + JSON
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Upgrades del Juego")]
    [SerializeField] private UpgradeData[] allUpgrades;

    private const string SaveKey = "IdleGameSave";

    private GameSaveData loadedData;

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

    private void Start()
    {
        LoadGame();
    }

    public void SaveGame()
    {
        if (GoldManager.Instance == null) return;

        GameSaveData saveData = new GameSaveData
        {
            gold = GoldManager.Instance.CurrentGold,
            upgrades = new List<UpgradeSaveData>(),
            lastScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            timeBeforeMiniGame = GameStartManager.Instance != null ? GameStartManager.Instance.GetRemainingTime() : 0,
            lastSaveTimestamp = DateTime.Now.ToBinary()
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

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("ℹ No hay datos guardados.");
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        loadedData = JsonUtility.FromJson<GameSaveData>(json);

        if (loadedData == null)
        {
            Debug.LogWarning("⚠ Error al cargar datos.");
            return;
        }

        // Restaurar oro
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(loadedData.gold - GoldManager.Instance.CurrentGold);
        }

        // Restaurar upgrades
        double totalOPS = 0;
        foreach (var savedUpgrade in loadedData.upgrades)
        {
            foreach (var upgrade in allUpgrades)
            {
                if (upgrade.upgradeName == savedUpgrade.upgradeName)
                {
                    upgrade.currentLevel = savedUpgrade.currentLevel;
                    totalOPS += upgrade.goldPerSecondPerLevel * upgrade.currentLevel;
                }
            }
        }

        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.SetGoldPerSecond(totalOPS);
        }

        Debug.Log("✅ Juego cargado: " + json);
    }

    public string GetLastScene()
    {
        return loadedData != null && !string.IsNullOrEmpty(loadedData.lastScene)
            ? loadedData.lastScene
            : null;
    }

    public float GetSavedTimer()
    {
        if (loadedData == null) return 0;

        if (loadedData.timeBeforeMiniGame > 0 && loadedData.lastSaveTimestamp > 0)
        {
            DateTime lastSaveTime = DateTime.FromBinary(loadedData.lastSaveTimestamp);
            double secondsPassed = (DateTime.Now - lastSaveTime).TotalSeconds;

            float remaining = loadedData.timeBeforeMiniGame - (float)secondsPassed;
            return Mathf.Max(remaining, 0);
        }

        return 0;
    }

    public void ResetGame()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("🗑 Progreso borrado.");
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}

[Serializable]
public class GameSaveData
{
    public double gold;
    public List<UpgradeSaveData> upgrades;
    public string lastScene;
    public float timeBeforeMiniGame;
    public long lastSaveTimestamp;
}

[Serializable]
public class UpgradeSaveData
{
    public string upgradeName;
    public int currentLevel;
}
