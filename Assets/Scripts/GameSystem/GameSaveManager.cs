using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
/// - Persiste en un archivo JSON en Application.persistentDataPath (savegame.json).
///   Si existe un save viejo en PlayerPrefs, se migra automáticamente la primera vez.
/// - Garantiza que la carga se aplique antes de que otros managers empiecen a funcionar.
///

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Inspector fallback (opcional)")]
    [SerializeField] private UpgradeBase[] allUpgrades;

    // Clave del save viejo en PlayerPrefs; solo se usa para migrar y limpiar.
    private const string LegacySaveKey = "IdleGameSave";
    private const string SaveFileName = "savegame.json";

    private GameSaveData loadedData;
    private UpgradeBase[] allUpgradesCache;

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // Estado cargado del save. Los managers (Maestría, Misiones, Tienda) lo leen
    // en su Start para restaurar su estado. Puede ser null si aún no se cargó.
    public GameSaveData LoadedData => loadedData;

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

    // ================== GUARDADO DIFERIDO (evita hitches por I/O sincrónico) ==================
    [Header("Guardado diferido")]
    [Tooltip("Cuánto esperar antes de escribir a disco después de un RequestSave(). " +
             "Si llegan varios pedidos seguidos (ej: varios kills consecutivos), se juntan en un solo guardado real.")]
    [SerializeField] private float saveDebounceSeconds = 0.5f;

    private Coroutine pendingSaveRoutine;

    /// <summary>
    /// Pide un guardado "para dentro de un rato" en vez de escribir a disco ahora mismo.
    /// Usar esto para eventos frecuentes/automáticos (progreso de misiones, generación de
    /// sets, etc.) en vez de SaveGame() directo, para no bloquear el frame con I/O sincrónico.
    /// Para cierres de la app (OnApplicationQuit/Pause) seguimos usando SaveGame() directo,
    /// ahí sí necesitamos la garantía de que se escribió antes de que el proceso muera.
    /// </summary>
    public void RequestSave()
    {
        if (pendingSaveRoutine != null) return; // ya hay uno programado, no acumular más
        pendingSaveRoutine = StartCoroutine(DelayedSaveRoutine());
    }

    private IEnumerator DelayedSaveRoutine()
    {
        yield return new WaitForSecondsRealtime(saveDebounceSeconds);
        pendingSaveRoutine = null;
        SaveGame();
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
            lastSaveTimestamp = DateTime.Now.ToBinary(),

            // Progresión (si el manager todavía no existe, conservar lo cargado)
            lifetimeGoldThisRun = GoldManager.Instance.LifetimeGoldThisRun,
            masteryPoints = MasteryManager.Instance != null
                          ? MasteryManager.Instance.MasteryPoints
                          : (loadedData != null ? loadedData.masteryPoints : 0),
            prestigeCount = MasteryManager.Instance != null
                          ? MasteryManager.Instance.PrestigeCount
                          : (loadedData != null ? loadedData.prestigeCount : 0),
            dailyMissions = DailyMissionManager.Instance != null
                          ? DailyMissionManager.Instance.GetSaveData()
                          : (loadedData != null ? loadedData.dailyMissions : null),
            zoneMissions = ZoneMissionManager.Instance != null
                          ? ZoneMissionManager.Instance.GetSaveData()
                          : (loadedData != null ? loadedData.zoneMissions : null),
            activeBoost = ShopManager.Instance != null
                        ? ShopManager.Instance.GetBoostSaveData()
                        : (loadedData != null ? loadedData.activeBoost : null)
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
        WriteSaveFile(json);

        // Mantener loadedData en sync con lo último guardado
        loadedData = saveData;

        Debug.Log("[GameSaveManager] Juego guardado: " + json);
    }

    // Escritura "segura": primero a un .tmp y después se reemplaza el archivo real,
    // para no corromper el save si la app muere a mitad de la escritura.
    private void WriteSaveFile(string json)
    {
        try
        {
            string tmpPath = SaveFilePath + ".tmp";
            File.WriteAllText(tmpPath, json);

            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
            File.Move(tmpPath, SaveFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[GameSaveManager] Error escribiendo el save: " + ex);
        }
    }

    // Lee el JSON del save. Prioridad: archivo nuevo -> PlayerPrefs viejo (migración) -> null.
    private string ReadSaveJson()
    {
        try
        {
            if (File.Exists(SaveFilePath))
                return File.ReadAllText(SaveFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[GameSaveManager] Error leyendo el save: " + ex);
        }

        // Migración desde el save viejo en PlayerPrefs (versiones <= 1.0)
        if (PlayerPrefs.HasKey(LegacySaveKey))
        {
            string legacyJson = PlayerPrefs.GetString(LegacySaveKey);
            if (!string.IsNullOrEmpty(legacyJson))
            {
                Debug.Log("[GameSaveManager] Migrando save viejo de PlayerPrefs a archivo.");
                WriteSaveFile(legacyJson);
                PlayerPrefs.DeleteKey(LegacySaveKey);
                PlayerPrefs.Save();
                return legacyJson;
            }
        }

        return null;
    }

    // ================== LOAD ==================
    public void LoadGame()
    {
        string json = ReadSaveJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[GameSaveManager] No hay datos guardados.");
            loadedData = new GameSaveData(); // inicializar vacío para evitar null checks
            return;
        }

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
        {
            GoldManager.Instance.SetGold(loadedData.gold);
            GoldManager.Instance.SetLifetimeGoldThisRun(loadedData.lifetimeGoldThisRun);
        }

        if (HorseCooldownManager.Instance != null)
            HorseCooldownManager.Instance.SetRemainingCooldown(loadedData.horseCooldownRemaining);

        Debug.Log("[GameSaveManager] Juego cargado: " + json);
    }

    // Aplicar valores post-load: GPS, cooldowns y oro
    public void ApplyLoadedDataToManagers()
    {
        if (loadedData == null) return;

        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.SetGold(loadedData.gold);
            GoldManager.Instance.SetLifetimeGoldThisRun(loadedData.lifetimeGoldThisRun);
        }

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

    // Reinicia todo el progreso de la run actual: upgrades, oro, GPS y cooldowns.
    // Es la parte compartida entre el reset total y el prestigio.
    private void ResetRunState()
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

        // 2) Resetear oro, acumulado de la run y GPS en GoldManager
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.SetGold(0);
            GoldManager.Instance.SetLifetimeGoldThisRun(0);
            GoldManager.Instance.SetGoldPerSecond(0.0);
        }

        // 3) Resetear cooldown del caballo
        if (HorseCooldownManager.Instance != null)
            HorseCooldownManager.Instance.SetRemainingCooldown(0f);
    }

    /// <summary>
    /// Reset por PRESTIGIO (Maestría): reinicia la run (upgrades, oro, nivel)
    /// pero conserva puntos de maestría, misiones diarias y tutorial.
    /// MasteryManager es quien suma los puntos ANTES de llamar a esto.
    /// </summary>
    public void ResetRunProgress()
    {
        ResetRunState();

        if (loadedData == null) loadedData = new GameSaveData();
        loadedData.gold = 0;
        loadedData.lifetimeGoldThisRun = 0;
        loadedData.upgrades = new List<UpgradeSaveData>();
        loadedData.currentLevelIndex = 0;
        loadedData.timeBeforeMiniGame = 0f;
        loadedData.horseCooldownRemaining = 0f;

        SaveGame();

        Debug.Log("[GameSaveManager] Run reiniciada por prestigio (maestría conservada).");
    }

    // ================== RESET ==================
    public void ResetGame()
    {
        ResetRunState();

        // El reset total también borra la maestría, misiones y boosts en memoria.
        // (Va antes de reinicializar loadedData: ResetAll puede disparar un SaveGame interno.)
        MasteryManager.Instance?.ResetAll();
        DailyMissionManager.Instance?.ResetAll();
        ZoneMissionManager.Instance?.ResetAll();
        ShopManager.Instance?.ResetAll();

        // Reinicializar loadedData en memoria para que GetSavedLevelIndex() devuelva 0
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

        // 5) Borrar el archivo de save (y la clave legacy de PlayerPrefs por las dudas)
        try
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[GameSaveManager] Error borrando el save: " + ex);
        }

        PlayerPrefs.DeleteKey(LegacySaveKey);
        PlayerPrefs.DeleteKey("TutorialSeen");
        PlayerPrefs.Save();

        Debug.Log("[GameSaveManager] Save cleared and upgrades reset to level 0.");
    }


    private void OnApplicationQuit()
    {
        FlushPendingSave();
        SaveGame();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            FlushPendingSave();
            SaveGame();
        }
    }

    /// <summary>Cancela cualquier guardado diferido pendiente (ya se va a guardar YA, sin esperar el debounce).</summary>
    private void FlushPendingSave()
    {
        if (pendingSaveRoutine != null)
        {
            StopCoroutine(pendingSaveRoutine);
            pendingSaveRoutine = null;
        }
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

    // --- Progresión (maestría / misiones / tienda) ---
    public double lifetimeGoldThisRun; // oro total ganado en la run actual
    public int masteryPoints;
    public int prestigeCount;
    public DailyMissionsSaveData dailyMissions;
    public ZoneMissionsSaveData zoneMissions;
    public BoostSaveData activeBoost;
}

[Serializable]
public class DailyMissionsSaveData
{
    public string dateKey; // "yyyyMMdd" del día al que pertenecen las misiones
    public List<MissionProgressData> missions = new List<MissionProgressData>();
}

[Serializable]
public class ZoneMissionsSaveData
{
    public int zoneIndex = -1; // índice de LevelData al que pertenecen estas misiones
    public List<MissionProgressData> missions = new List<MissionProgressData>();
}

[Serializable]
public class MissionProgressData
{
    public string missionId;
    public double progress;
    public double resolvedTarget; // objetivo resuelto al asignar la misión (para targets que escalan con GPS)
    public bool claimed;
}

[Serializable]
public class BoostSaveData
{
    public double multiplier = 1.0;
    public float remainingSeconds;
}

[Serializable]
public class UpgradeSaveData
{
    public string upgradeName;
    public int currentLevel;
    public int bonusCount; // <--- NUEVO: cantidad de bonuses comprados para este upgrade
}
