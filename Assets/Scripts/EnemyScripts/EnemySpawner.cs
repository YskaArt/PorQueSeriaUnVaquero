using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> bonusPrefabs = new List<GameObject>();

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Timing")]
    [SerializeField] private float minSpawnTime = 2f;
    [SerializeField] private float maxSpawnTime = 7f;
    [SerializeField] private float patternInterval = 5f;
    [SerializeField] private float bonusInterval = 20f;

    [Header("Pool")]
    [SerializeField] private Transform worldContainer;
    [SerializeField] private int initialPoolPerPrefab = 10;

    // Velocidades públicas
    public float NormalEnemySpeed { get; set; } = 5f;
    public float HorseSkillEnemySpeed { get; set; } = 10f;

    // Estado interno
    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
    private bool isSpawning = false;
    private float bonusTimer = 0f;
    private Coroutine spawnCoroutine;
    private bool isSpawningPattern = false;

    // Frenzy / HorseMode
    private bool frenzyMode = false;
    private float frenzySpawnDelay = 0.12f; // default rapid fire

    // Propiedades públicas para compatibilidad
    public bool IsHorseSkillActive => frenzyMode;
    public float MinSpawnTime { get => minSpawnTime; set => minSpawnTime = value; }
    public float MaxSpawnTime { get => maxSpawnTime; set => maxSpawnTime = value; }

    private void Awake()
    {
        if (worldContainer == null) worldContainer = this.transform;
        
        foreach (var prefab in bonusPrefabs)
            EnsurePoolFor(prefab);
    }

    private void Start()
    {
        StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    // -----------------------
    // Pool helpers
    // -----------------------
    private void EnsurePoolFor(GameObject prefab)
    {
        if (prefab == null) return;
        if (pools.ContainsKey(prefab)) return;
        pools[prefab] = new List<GameObject>();
        for (int i = 0; i < initialPoolPerPrefab; i++)
        {
            var go = Instantiate(prefab, worldContainer);
            go.SetActive(false);
            pools[prefab].Add(go);
        }
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (prefab == null) return null;
        if (!pools.ContainsKey(prefab))
            EnsurePoolFor(prefab);
        var list = pools[prefab];
        for (int i = 0; i < list.Count; i++)
            if (!list[i].activeInHierarchy)
                return list[i];
        var go = Instantiate(prefab, worldContainer);
        go.SetActive(false);
        list.Add(go);
        return go;
    }

    // -----------------------
    // Control externo
    // -----------------------
    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        StopAllCoroutines();
    }

    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void RestartSpawning()
    {
        StopSpawning();
        bonusTimer = 0f;
        StartSpawning();
    }

    /// <summary>
    /// Reemplaza la lista de enemyPrefabs por la nueva (no toca bonus).
    /// </summary>
    public void SetEnemyPool(List<GameObject> newEnemies)
    {
        // Mantener pools de bonus, eliminar pools de enemigos previos
        var toRemove = new List<GameObject>();
        foreach (var kv in pools)
        {
            if (!bonusPrefabs.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }
        foreach (var k in toRemove)
            pools.Remove(k);

        enemyPrefabs.Clear();
        if (newEnemies != null && newEnemies.Count > 0)
        {
            enemyPrefabs.AddRange(newEnemies);
            foreach (var p in enemyPrefabs)
                EnsurePoolFor(p);
        }
        Debug.Log($"[EnemySpawner] Enemy pool set: {enemyPrefabs.Count} prefabs.");
    }

    // -----------------------
    // Horse / Frenzy mode
    // -----------------------
    /// <summary>
    /// Entra en modo frenesí: spawn continuo SOLO enemigos y a la velocidad indicada.
    /// </summary>
    /// <param name="enemySpeedMultiplier">multiplica NormalEnemySpeed para new enemies</param>
    /// <param name="spawnDelay">delay entre spawns en frenzy (ej: 0.12f)</param>
    public void ActivateHorseMode(float enemySpeedMultiplier = 2f, float spawnDelay = 0.12f)
    {
        frenzyMode = true;
        HorseSkillEnemySpeed = NormalEnemySpeed * Mathf.Max(1f, enemySpeedMultiplier);
        frenzySpawnDelay = Mathf.Max(0.02f, spawnDelay);
        Debug.Log("[EnemySpawner] HorseMode ACTIVATED");
    }

    public void DeactivateHorseMode()
    {
        frenzyMode = false;
        Debug.Log("[EnemySpawner] HorseMode DEACTIVATED");
    }

    // -----------------------
    // Spawn loop
    // -----------------------
    private IEnumerator SpawnRoutine()
    {
        // espera inicial aleatoria
        yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

        while (isSpawning)
        {
            if (frenzyMode)
            {
                // spawn continuo solo enemigos (usa pool)
                SpawnEnemyFromPoolAtRandomPoint(HorseSkillEnemySpeed);
                yield return new WaitForSeconds(frenzySpawnDelay);
                continue;
            }

            // modo normal: patrones
            yield return StartCoroutine(SpawnPattern());

            float wait = patternInterval;
            float elapsed = 0f;
            while (elapsed < wait)
            {
                bonusTimer += Time.deltaTime;
                if (bonusTimer >= bonusInterval)
                {
                    SpawnBonus();
                    bonusTimer = 0f;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator SpawnPattern()
    {
        if (isSpawningPattern) yield break;
        isSpawningPattern = true;

        if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0)
        {
            isSpawningPattern = false;
            yield break;
        }

        int lineIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[lineIndex];
        int count = Random.Range(2, 5);
        int prefabIndex = Random.Range(0, enemyPrefabs.Count);
        GameObject prefab = enemyPrefabs[prefabIndex];
        float stagger = 0.9f;

        for (int i = 0; i < count; i++)
        {
            var go = SpawnFromPool(prefab, spawnPoint.position, Quaternion.identity);
            var runner = go != null ? go.GetComponent<RunnerEnemy>() : null;
            if (runner != null)
            {
                runner.SetFallSpeed(NormalEnemySpeed);
            }
            yield return new WaitForSeconds(stagger);
        }

        isSpawningPattern = false;
    }

    // -----------------------
    // Spawn helpers (pool)
    // -----------------------
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var go = GetFromPool(prefab);
        if (go == null) return null;
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.SetParent(worldContainer);
        go.SetActive(true);
        var poolable = go.GetComponent<IPoolResettable>();
        poolable?.OnSpawn();
        return go;
    }

    private void SpawnEnemyFromPoolAtRandomPoint(float speed)
    {
        if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0) return;
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 pos = sp.position;
        var go = SpawnFromPool(prefab, pos, Quaternion.identity);
        var runner = go != null ? go.GetComponent<RunnerEnemy>() : null;
        if (runner != null)
            runner.SetFallSpeed(speed > 0f ? speed : NormalEnemySpeed);
    }

    private void SpawnBonus()
    {
        if (bonusPrefabs.Count == 0 || spawnPoints.Length == 0) return;
        int prefabIndex = Random.Range(0, bonusPrefabs.Count);
        int pointIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[pointIndex];
        SpawnFromPool(bonusPrefabs[prefabIndex], spawnPoint.position, Quaternion.identity);
    }

    public interface IPoolResettable
    {
        void OnSpawn();
    }

}
