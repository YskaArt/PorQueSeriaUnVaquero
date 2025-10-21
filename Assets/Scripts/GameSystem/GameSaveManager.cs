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

        // Cargar los datos en Awake para que estén disponibles durante Start de otros managers
        LoadGame();
    }

    private void Start()
    {
        // Si hay datos cargados pendientes, aplicar a los managers ahora
        ApplyLoadedDataToManagers();
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
            horseCooldownRemaining = HorseCooldownManager.Instance != null ? HorseCooldownManager.Instance.GetRemainingCooldown() : 0f,
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

        // Restaurar oro si GoldManager ya está disponible
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

        // Restaurar cooldown del caballo si existe la instancia
        if (HorseCooldownManager.Instance != null)
        {
            HorseCooldownManager.Instance.SetRemainingCooldown(loadedData.horseCooldownRemaining);
        }

        Debug.Log("✅ Juego cargado: " + json);

        // Si GoldManager no estaba disponible en el momento de la carga, apply later when it becomes available
        if (GoldManager.Instance == null)
        {
            Debug.Log("GameSaveManager: GoldManager no disponible, se aplicarán los datos cuando esté listo.");
        }
    }

    // Public helper: aplica los datos cargados al GoldManager si están pendientes
    public void ApplyLoadedDataToManagers()
    {
        if (loadedData == null)
            return;

        // Aplicar oro
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(loadedData.gold - GoldManager.Instance.CurrentGold);
        }

        // Aplicar OPS calculado a partir de upgrades
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

        // Aplicar cooldown del caballo si existe la instancia
        if (HorseCooldownManager.Instance != null)
        {
            HorseCooldownManager.Instance.SetRemainingCooldown(loadedData.horseCooldownRemaining);
        }

        Debug.Log("GameSaveManager: Datos aplicados a los managers desde loadedData.");
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
    public float horseCooldownRemaining;
    public long lastSaveTimestamp;
}

[Serializable]
public class UpgradeSaveData
{
    public string upgradeName;
    public int currentLevel;
}
