/*
    EnemyGoldManager
    ----------------
    Sistema central encargado de determinar cuánta MONEDA suelta cada enemigo,
    basado principalmente en el GPS (Gold Per Second) del jugador y las mejoras
    del ScriptableObject EnemyGoldUpgradeData.

    FUNCIONALIDAD GENERAL:
    • Calcula y mantiene en caché ("cachedReward") el oro que debe otorgar cada
      enemigo al morir.
    • Se subscribe a:
        - Cambios en el nivel del upgrade EnemyGoldUpgradeData.
        - Cambios en el GPS del GoldManager.
      Cada cambio relevante fuerza una recalculación del valor de recompensa.

    • "cachedReward" se usa para evitar cálculos costosos por enemigo derrotado.
      Cada enemigo muerto solo llama a GetEnemyGoldReward().

    • El sistema se mantiene entre escenas (DontDestroyOnLoad).

    FLUJO:
    1. Al iniciar, busca el ScriptableObject si no fue asignado.
    2. Se suscribe a los eventos de upgrade y gold.
    3. Calcula la recompensa inicial.
    4. Cada cambio significativo de GPS vuelve a calcular la recompensa.
    5. Los enemigos consultan GetEnemyGoldReward() al morir.

    Este sistema asegura que el oro de enemigos escale correctamente con el
    progreso idle, evitando que los enemigos se vuelvan irrelevantes.
*/

using UnityEngine;

public class EnemyGoldManager : MonoBehaviour
{
    public static EnemyGoldManager Instance { get; private set; }

    [SerializeField] private EnemyGoldUpgradeData enemyGoldUpgrade;

    private double cachedReward = 1.0;
    private double lastKnownGPS = 0.0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (enemyGoldUpgrade == null)
        {
            var list = Resources.FindObjectsOfTypeAll<EnemyGoldUpgradeData>();
            if (list != null && list.Length > 0)
                enemyGoldUpgrade = list[0];
        }

        if (enemyGoldUpgrade != null)
            enemyGoldUpgrade.OnLevelChanged += OnEnemyUpgradeChanged;

        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged += OnGoldOrGPSChanged;

        RecalculateCachedReward();
    }

    private void OnDestroy()
    {
        if (enemyGoldUpgrade != null)
            enemyGoldUpgrade.OnLevelChanged -= OnEnemyUpgradeChanged;

        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= OnGoldOrGPSChanged;
    }

    public void OnEnemyUpgradeChanged()
    {
        RecalculateCachedReward();
        Debug.Log("[EnemyGoldManager] Enemy gold upgrade changed -> level: " +
            (enemyGoldUpgrade != null ? enemyGoldUpgrade.currentLevel.ToString() : "n/a"));
    }

    private void OnGoldOrGPSChanged()
    {
        double gps = GoldManager.Instance != null ? GoldManager.Instance.CurrentGoldPerSecond : 0.0;

        if (!Mathf.Approximately((float)gps, (float)lastKnownGPS))
            RecalculateCachedReward();
    }

    private void RecalculateCachedReward()
    {
        double gps = GoldManager.Instance != null ? GoldManager.Instance.CurrentGoldPerSecond : 0.0;
        lastKnownGPS = gps;

        if (enemyGoldUpgrade == null)
        {
            cachedReward = 1.0;
            return;
        }

        cachedReward = enemyGoldUpgrade.CalculateEnemyReward(gps);
    }

    public double GetEnemyGoldReward()
    {
        double baseReward = cachedReward;
        double multiplier = BonusManager.Instance != null ? BonusManager.Instance.GetEnemyRewardMultiplier() : 1.0;
        return baseReward * multiplier;
    }
}
