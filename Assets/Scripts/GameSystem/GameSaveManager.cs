using System;
using System.Collections.Generic;
using UnityEngine;

///
/// GameSaveManager
/// ----------------
/// Sistema central de guardado/carga del juego.
/// Funciona así:
/// - Detecta automáticamente todos los UpgradeBase (por Resources, FindAll o fallback del inspector).
/// - Guarda oro, niveles de upgrades, cooldowns y escena actual.
/// - Carga los datos y aplica los niveles directamente a cada UpgradeBase mediante ApplyLoadedState().
/// - Recalcula GPS después de cargar.
/// - Mantiene persistencia usando PlayerPrefs y JSON.
/// - Garantiza que la carga se aplique antes de que otros managers empiecen a funcionar.
///

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Inspector fallback (opcional)")]
    [SerializeField] private UpgradeBase[] allUpgrades;

    private const string SaveKey = "IdleGameSave";
    private GameSaveData loadedData;
    private UpgradeBase[] allUpgradesCache;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadGame();
    }

    private void Start()
    {
        ApplyLoadedDataToManagers();
    }

    // Obtiene todos los upgrades disponibles usando varios métodos de detección
    private UpgradeBase[] GetAllUpgrades()
    {
        if (allUpgradesCache != null) return allUpgradesCache;

        var fromResources = Resources.LoadAll<UpgradeBase>("Upgrades");
        if (fromResources != null && fromResources.Length > 0)
        {
            allUpgradesCache = fromResources;
            Debug.Log($"[GameSaveManager] Loaded {allUpgradesCache.Length} upgrades from Resources/Upgrades.");
            return allUpgradesCache;
        }

        var fromFindAll = Resources.FindObjectsOfTypeAll<UpgradeBase>();
        if (fromFindAll != null && fromFindAll.Length > 0)
        {
            allUpgradesCache = fromFindAll;
            Debug.Log($"[GameSaveManager] Loaded {allUpgradesCache.Length} upgrades via FindObjectsOfTypeAll.");
            return allUpgradesCache;
        }

        if (allUpgrades != null && allUpgrades.Length > 0)
        {
            allUpgradesCache = allUpgrades;
            Debug.Log($"[GameSaveManager] Loaded {allUpgradesCache.Length} upgrades from Inspector fallback.");
            return allUpgradesCache;
        }

        allUpgradesCache = Array.Empty<UpgradeBase>();
        Debug.LogWarning("[GameSaveManager] No upgrades found by any method!");
        return allUpgradesCache;
    }

    // ================== SAVE ==================
    public void SaveGame()
    {
        if (GoldManager.Instance == null) return;

        GameSaveData saveData = new GameSaveData
        {
            gold = GoldManager.Instance.CurrentGold,
            upgrades = new List<UpgradeSaveData>(),
            currentLevelIndex = GameManager.Instance != null ? GameManager.Instance.GetCurrentLevelIndex() : 0,
            lastScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            horseCooldownRemaining = HorseCooldownManager.Instance != null
                                   ? HorseCooldownManager.Instance.GetRemainingCooldown()
                                   : 0f,
            lastSaveTimestamp = DateTime.Now.ToBinary()
        };

        foreach (var upgrade in GetAllUpgrades())
        {
            if (upgrade == null) continue;
            saveData.upgrades.Add(new UpgradeSaveData
            {
                upgradeName = upgrade.upgradeName,
                currentLevel = upgrade.currentLevel,
                bonusCount = upgrade.HasBonus() ? upgrade.bonusCount : 0
            });
        }

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        Debug.Log("[GameSaveManager] Juego guardado: " + json);
    }

    // ================== LOAD ==================
    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("[GameSaveManager] No hay datos guardados.");
            loadedData = new GameSaveData(); // inicializar vacío para evitar null checks
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        loadedData = JsonUtility.FromJson<GameSaveData>(json);

        if (loadedData == null)
        {
            Debug.LogWarning("[GameSaveManager] Error al cargar datos.");
            loadedData = new GameSaveData();
            return;
        }

        // Aplicar niveles guardados a los ScriptableObjects detectados
        var all = GetAllUpgrades();
        if (loadedData.upgrades != null && all != null)
        {
            var map = new Dictionary<string, UpgradeSaveData>();
            foreach (var s in loadedData.upgrades)
                map[s.upgradeName] = s;

            foreach (var up in all)
            {
                if (up == null) continue;
                if (map.TryGetValue(up.upgradeName, out UpgradeSaveData saved))
                {
                    // Aplicar nivel y bonusCount de forma segura
                    up.ApplyLoadedState(saved.currentLevel);
                    // Si el upgrade soporta bonus, aplicar también el bonusCount con el método seguro
                    if (up.HasBonus())
                    {
                        up.ApplyLoadedBonusCount(saved.bonusCount);
                    }
                }
                else
                {
                    up.ApplyLoadedState(0);
                    if (up.HasBonus())
                        up.ApplyLoadedBonusCount(0);
                }
            }
        }

        // Aplicar oro y cooldown (si managers ya existen)
        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(loadedData.gold - GoldManager.Instance.CurrentGold);

        if (HorseCooldownManager.Instance != null)
            HorseCooldownManager.Instance.SetRemainingCooldown(loadedData.horseCooldownRemaining);

        Debug.Log("[GameSaveManager] Juego cargado: " + json);
    }

    // Aplicar valores post-load: GPS, cooldowns y oro
    public void ApplyLoadedDataToManagers()
    {
        if (loadedData == null) return;

        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(loadedData.gold - GoldManager.Instance.CurrentGold);

        double totalGPS = 0;
        foreach (var u in GetAllUpgrades())
        {
            if (u is GPSUpgradeData gps)
                totalGPS += gps.GetEffectiveGPS();
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.SetGoldPerSecond(totalGPS);

        if (HorseCooldownManager.Instance != null)
            HorseCooldownManager.Instance.SetRemainingCooldown(loadedData.horseCooldownRemaining);

        Debug.Log("[GameSaveManager] Loaded data applied to managers.");
    }

    // ================== GETTERS ==================
    public string GetLastScene() => loadedData != null ? loadedData.lastScene : null;

    public int GetSavedLevelIndex()
    {
        return loadedData != null ? loadedData.currentLevelIndex : 0;
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

    // ================== RESET ==================
    public void ResetGame()
    {
        // 1) Resetear todos los upgrades a 0 (esto también disparará OnLevelChanged en los SOs)
        var upgrades = GetAllUpgrades();
        if (upgrades != null)
        {
            foreach (var up in upgrades)
            {
                if (up == null) continue;
                up.ApplyLoadedState(0); // aplica nivel 0 y notifica
                if (up.HasBonus())
                    up.ApplyLoadedBonusCount(0);
            }
        }

        // 2) Resetear oro y GPS en GoldManager
        if (GoldManager.Instance != null)
        {
            // Restar todo el oro actual para dejar en 0
            double currentGold = GoldManager.Instance.CurrentGold;
            if (currentGold != 0)
                GoldManager.Instance.AddGold(-currentGold);

            // Poner GPS a 0
            GoldManager.Instance.SetGoldPerSecond(0.0);
        }

        // 3) Resetear cooldown del caballo
        if (HorseCooldownManager.Instance != null)
            HorseCooldownManager.Instance.SetRemainingCooldown(0f);

        // 4) Reinicializar loadedData en memoria para que GetSavedLevelIndex() devuelva 0
        loadedData = new GameSaveData
        {
            gold = 0,
            upgrades = new List<UpgradeSaveData>(),
            lastScene = null,
            currentLevelIndex = 0,
            timeBeforeMiniGame = 0f,
            horseCooldownRemaining = 0f,
            lastSaveTimestamp = 0
        };

        // 5) Borrar el save en PlayerPrefs (y escribir el estado limpio opcionalmente)
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey("TutorialSeen");
        PlayerPrefs.Save();

        Debug.Log("[GameSaveManager] Save cleared and upgrades reset to level 0.");
    }


    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveGame();
    }
}

[Serializable]
public class GameSaveData
{
    public double gold;
    public List<UpgradeSaveData> upgrades;
    public string lastScene;
    public int currentLevelIndex;
    public float timeBeforeMiniGame;
    public float horseCooldownRemaining;
    public long lastSaveTimestamp;
}

[Serializable]
public class UpgradeSaveData
{
    public string upgradeName;
    public int currentLevel;
    public int bonusCount; // <--- NUEVO: cantidad de bonuses comprados para este upgrade
}
