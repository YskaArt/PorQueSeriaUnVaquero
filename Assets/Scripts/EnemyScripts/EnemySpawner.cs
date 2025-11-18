/// <summary>
/// Sistema completo de generación de enemigos y bonus.
/// 
/// Funcionalidades principales:
/// - Usa object pooling para todos los enemigos y bonus.
/// - Genera enemigos mediante patrones y también por intervalos.
/// - Incluye un "Horse Mode" (frenzy) que acelera el spawn y la velocidad.
/// - Permite activar/desactivar el spawner externamente y cambiar el pool de enemigos.
/// - Genera bonus cada cierto intervalo independiente del spawn normal.
/// - Mantiene coroutines controladas para evitar solapamientos o fugas.
/// 
/// Este spawner está preparado para niveles con diferentes pools de enemigos,
/// modos temporales de alta intensidad y optimización mediante pools.
/// </summary>
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

    public float NormalEnemySpeed { get; set; } = 5f;
    public float HorseSkillEnemySpeed { get; set; } = 10f;

    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
    private bool isSpawning = false;
    private float bonusTimer = 0f;
    private Coroutine spawnCoroutine;
    private Coroutine patternCoroutine;
    private bool isSpawningPattern = false;

    private bool frenzyMode = false;
    private float frenzySpawnDelay = 0.12f;

    public bool IsHorseSkillActive => frenzyMode;
    public bool IsSpawning => isSpawning;
    public float MinSpawnTime { get => minSpawnTime; set => minSpawnTime = value; }
    public float MaxSpawnTime { get => maxSpawnTime; set => maxSpawnTime = value; }

    private void Awake()
    {
        if (worldContainer == null) worldContainer = transform;

        foreach (var prefab in bonusPrefabs)
            EnsurePoolFor(prefab);

        foreach (var prefab in enemyPrefabs)
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

    // POOL
    private void EnsurePoolFor(GameObject prefab)
    {
        if (prefab == null || pools.ContainsKey(prefab)) return;

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
        if (!pools.ContainsKey(prefab))
            EnsurePoolFor(prefab);

        foreach (var obj in pools[prefab])
            if (!obj.activeInHierarchy)
                return obj;

        var extra = Instantiate(prefab, worldContainer);
        extra.SetActive(false);
        pools[prefab].Add(extra);
        return extra;
    }

    // CONTROL EXTERNO
    public void StopSpawning()
    {
        isSpawning = false;

        if (patternCoroutine != null)
        {
            try { StopCoroutine(patternCoroutine); }
            catch { }
            patternCoroutine = null;
            isSpawningPattern = false;
        }

        if (spawnCoroutine != null)
        {
            try { StopCoroutine(spawnCoroutine); }
            catch { }
            spawnCoroutine = null;
        }
    }

    public void StartSpawning()
    {
        if (isSpawning) return;

        isSpawning = true;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void RestartSpawning()
    {
        StopSpawning();
        bonusTimer = 0f;
        StartSpawning();
    }

    public void SetEnemyPool(List<GameObject> newEnemies)
    {
        var toRemove = new List<GameObject>();

        foreach (var p in pools)
            if (!bonusPrefabs.Contains(p.Key) && (newEnemies == null || !newEnemies.Contains(p.Key)))
                toRemove.Add(p.Key);

        foreach (var k in toRemove)
            pools.Remove(k);

        enemyPrefabs.Clear();

        if (newEnemies != null)
        {
            enemyPrefabs.AddRange(newEnemies);
            foreach (var p in enemyPrefabs)
                EnsurePoolFor(p);
        }

        Debug.Log($"[EnemySpawner] Enemy pool set: {enemyPrefabs.Count} prefabs.");
    }

    // HORSE MODE
    public void ActivateHorseMode(float speedMultiplier = 2f, float delay = 0.12f)
    {
        if (frenzyMode) return;

        frenzyMode = true;
        HorseSkillEnemySpeed = NormalEnemySpeed * Mathf.Max(1f, speedMultiplier);
        frenzySpawnDelay = Mathf.Max(0.02f, delay);

        if (patternCoroutine != null)
        {
            StopCoroutine(patternCoroutine);
            patternCoroutine = null;
            isSpawningPattern = false;
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        spawnCoroutine = StartCoroutine(FrenzySpawnRoutine());
        UpdateActiveEnemiesSpeed(HorseSkillEnemySpeed);
    }

    public void DeactivateHorseMode()
    {
        if (!frenzyMode) return;

        frenzyMode = false;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnRoutine());
        UpdateActiveEnemiesSpeed(NormalEnemySpeed);
    }

    private IEnumerator FrenzySpawnRoutine()
    {
        SpawnEnemyFromPoolAtRandomPoint(HorseSkillEnemySpeed);

        while (frenzyMode && isSpawning)
        {
            yield return new WaitForSeconds(frenzySpawnDelay);
            SpawnEnemyFromPoolAtRandomPoint(HorseSkillEnemySpeed);
        }
    }

    // LOOP PRINCIPAL
    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

        while (isSpawning)
        {
            if (frenzyMode)
            {
                SpawnEnemyFromPoolAtRandomPoint(HorseSkillEnemySpeed);
                yield return new WaitForSeconds(frenzySpawnDelay);
                continue;
            }

            patternCoroutine = StartCoroutine(SpawnPattern());
            yield return patternCoroutine;
            patternCoroutine = null;

            float elapsed = 0f;

            while (elapsed < patternInterval)
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

        int point = Random.Range(0, spawnPoints.Length);
        Transform p = spawnPoints[point];

        int count = Random.Range(2, 5);
        float stagger = 0.9f;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        for (int i = 0; i < count; i++)
        {
            GameObject go = SpawnFromPool(prefab, p.position, Quaternion.identity);

            var runner = go.GetComponent<RunnerEnemy>();
            if (runner != null)
            {
                float speed = frenzyMode ? HorseSkillEnemySpeed : NormalEnemySpeed;
                runner.SetFallSpeed(speed);
            }

            yield return new WaitForSeconds(stagger);
        }

        isSpawningPattern = false;
    }

    // SPAWN HELPERS
    public GameObject SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject go = GetFromPool(prefab);
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.SetParent(worldContainer);
        go.SetActive(true);

        var r = go.GetComponent<IPoolResettable>();
        r?.OnSpawn();

        return go;
    }

    private void SpawnEnemyFromPoolAtRandomPoint(float speed)
    {
        if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject go = SpawnFromPool(prefab, sp.position, Quaternion.identity);

        var runner = go.GetComponent<RunnerEnemy>();
        runner?.SetFallSpeed(speed > 0f ? speed : NormalEnemySpeed);
    }

    private void SpawnBonus()
    {
        if (bonusPrefabs.Count == 0 || spawnPoints.Length == 0) return;

        GameObject prefab = bonusPrefabs[Random.Range(0, bonusPrefabs.Count)];
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

        SpawnFromPool(prefab, sp.position, Quaternion.identity);
    }

    // UTILS
    private void UpdateActiveEnemiesSpeed(float speed)
    {
        var enemies = FindObjectsByType<RunnerEnemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
            if (e.gameObject.activeInHierarchy)
                e.SetFallSpeed(speed);
    }

    public interface IPoolResettable
    {
        void OnSpawn();
    }
}
