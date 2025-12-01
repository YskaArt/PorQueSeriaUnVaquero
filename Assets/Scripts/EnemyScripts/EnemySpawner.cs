/// <summary>
/// Sistema completo de generación de enemigos y bonus.
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
    [Tooltip("Minimum time (seconds) between spawned Bonus items")]
    [SerializeField] private float minBonusInterval = 120f;

    [Header("Pool")]
    [SerializeField] private Transform worldContainer;
    [SerializeField] private int initialPoolPerPrefab = 10;

    [Header("Bonus spawn checks")]
    [Tooltip("Radius to consider a spawn point occupied by an enemy or bonus")]
    [SerializeField] private float spawnPointBlockRadius = 0.6f;
    [Tooltip("Time (seconds) to consider a spawn point recently used and avoid reusing it")]
    [SerializeField] private float spawnPointCooldown = 0.75f;

    [Header("Horde settings")]
    [Tooltip("Cooldown after a horde ends before normal spawns can resume (s)")]
    [SerializeField] private float hordeEndCooldown = 0.75f;

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

    private float lastBonusSpawnTime = -Mathf.Infinity;

    // Bonus horde control
    private bool bonusHordeActive = false;
    private Coroutine bonusHordeCoroutine;
    private float lastHordeEndTime = -Mathf.Infinity;

    // Track last used time per spawn point to avoid immediate reuse
    private Dictionary<Transform, float> spawnPointLastUsed = new Dictionary<Transform, float>();

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

        // init spawnPointLastUsed
        if (spawnPoints != null)
        {
            foreach (var sp in spawnPoints)
                if (sp != null && !spawnPointLastUsed.ContainsKey(sp))
                    spawnPointLastUsed[sp] = -Mathf.Infinity;
        }
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

    // New API to start a bonus-controlled horde spawn on this spawner
    public void StartBonusHorde(float duration, float spawnInterval, float enemySpeed)
    {
        if (bonusHordeActive) return;
        bonusHordeActive = true;

        // Stop any pattern running
        if (patternCoroutine != null)
        {
            try { StopCoroutine(patternCoroutine); } catch { }
            patternCoroutine = null;
            isSpawningPattern = false;
        }

        // Stop normal spawn loop so it respects bonusHordeActive
        if (spawnCoroutine != null)
        {
            try { StopCoroutine(spawnCoroutine); } catch { }
            spawnCoroutine = null;
        }

        if (bonusHordeCoroutine != null)
        {
            try { StopCoroutine(bonusHordeCoroutine); } catch { }
            bonusHordeCoroutine = null;
        }

        bonusHordeCoroutine = StartCoroutine(BonusHordeSpawnRoutine(duration, spawnInterval, enemySpeed));
    }

    public void StopBonusHordeImmediate()
    {
        if (!bonusHordeActive) return;
        bonusHordeActive = false;
        if (bonusHordeCoroutine != null)
        {
            try { StopCoroutine(bonusHordeCoroutine); } catch { }
            bonusHordeCoroutine = null;
        }
        lastHordeEndTime = Time.time;

        // Restart main spawn loop
        if (spawnCoroutine != null) { StopCoroutine(spawnCoroutine); spawnCoroutine = null; }
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator BonusHordeSpawnRoutine(float duration, float spawnInterval, float enemySpeed)
    {
        float timer = 0f;
        System.Random rnd = new System.Random();

        while (timer < duration && isSpawning)
        {
            // Spawn at a free point if possible
            Transform free = GetRandomFreeSpawnPoint();
            if (free != null && enemyPrefabs.Count > 0)
            {
                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
                GameObject go = SpawnFromPool(prefab, free.position, Quaternion.identity);
                var runner = go.GetComponent<RunnerEnemy>();
                runner?.SetFallSpeed(enemySpeed > 0f ? enemySpeed : NormalEnemySpeed);

                // record usage to avoid immediate reuse
                RecordSpawnAtPoint(free);
            }

            float jitter = (float)(rnd.NextDouble() * (spawnInterval * 0.5));
            yield return new WaitForSeconds(Mathf.Max(0.02f, spawnInterval + jitter));
            timer += spawnInterval;
        }

        // Horde finished
        bonusHordeActive = false;
        bonusHordeCoroutine = null;
        lastHordeEndTime = Time.time;

        // Restart main spawn loop
        if (spawnCoroutine != null) { StopCoroutine(spawnCoroutine); spawnCoroutine = null; }
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    // LOOP PRINCIPAL
    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

        while (isSpawning)
        {
            // If any bonus object is present in the world, pause normal spawning/patterns
            while (HasAnyActiveBonusObject() || bonusHordeActive)
            {
                yield return null; // wait until bonuses cleared / horde finished
            }

            // Respect a cooldown after a horde ended to avoid immediate spawn too near
            if (Time.time - lastHordeEndTime < hordeEndCooldown)
            {
                yield return null;
                continue;
            }

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

        // find a free spawn point for the whole pattern; if none, skip this pattern
        Transform p = GetRandomFreeSpawnPoint();
        if (p == null)
        {
            isSpawningPattern = false;
            yield break;
        }

        int count = Random.Range(2, 5);
        float stagger = 0.9f;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        for (int i = 0; i < count; i++)
        {
            // if a bonus appears meanwhile, abort pattern
            if (HasAnyActiveBonusObject() || bonusHordeActive) break;

            // Before spawning, ensure the point is still free (not taken by another spawn)
            if (!IsSpawnPointFree(p))
            {
                // try to find another free point for the remainder of the pattern
                Transform alt = GetRandomFreeSpawnPoint();
                if (alt == null)
                {
                    // no free point available - abort pattern
                    break;
                }
                p = alt;
            }

            GameObject go = SpawnFromPool(prefab, p.position, Quaternion.identity);

            var runner = go.GetComponent<RunnerEnemy>();
            if (runner != null)
            {
                float speed = frenzyMode ? HorseSkillEnemySpeed : NormalEnemySpeed;
                runner.SetFallSpeed(speed);
            }

            // record usage to avoid immediate reuse
            RecordSpawnAtPoint(p);

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

        Transform free = GetRandomFreeSpawnPoint();
        if (free == null) return; // no free spot, skip

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        GameObject go = SpawnFromPool(prefab, free.position, Quaternion.identity);

        var runner = go.GetComponent<RunnerEnemy>();
        runner?.SetFallSpeed(speed > 0f ? speed : NormalEnemySpeed);

        RecordSpawnAtPoint(free);
    }

    private void SpawnBonus()
    {
        // Check min interval
        if (Time.time - lastBonusSpawnTime < minBonusInterval) return;

        // Do not spawn if a Bonus is already active (effect) or a bonus object exists
        if (BonusManager.Instance != null && BonusManager.Instance.IsBonusActive()) return;
        if (HasAnyActiveBonusObject()) return;

        if (bonusPrefabs.Count == 0 || spawnPoints.Length == 0) return;

        // Find a free spawn point (not occupied by active enemy/bonus)
        Transform free = GetRandomFreeSpawnPoint();
        if (free == null)
        {
            // No free spawn point available, skip this spawn
            return;
        }

        GameObject prefab = bonusPrefabs[Random.Range(0, bonusPrefabs.Count)];

        SpawnFromPool(prefab, free.position, Quaternion.identity);

        // record usage
        RecordSpawnAtPoint(free);

        lastBonusSpawnTime = Time.time;
    }

    // New helper: find a random spawn point that is free (no active pooled object within radius and not recently used)
    private Transform GetRandomFreeSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        int[] indices = new int[spawnPoints.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        // Fisher-Yates shuffle
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
        }

        foreach (int idx in indices)
        {
            Transform sp = spawnPoints[idx];
            if (sp == null) continue;
            if (IsSpawnPointFree(sp)) return sp;
        }

        return null;
    }

    // Returns true if there is any active bonus prefab instance in the world (pool)
    private bool HasAnyActiveBonusObject()
    {
        if (bonusPrefabs == null || bonusPrefabs.Count == 0) return false;

        foreach (var prefab in bonusPrefabs)
        {
            if (prefab == null) continue;
            if (!pools.ContainsKey(prefab)) continue;
            var list = pools[prefab];
            if (list == null) continue;
            foreach (var go in list)
            {
                if (go == null) continue;
                if (go.activeInHierarchy) return true;
            }
        }

        return false;
    }

    private bool IsSpawnPointFree(Transform sp)
    {
        Vector3 pos = sp.position;

        // Check cooldown
        if (spawnPointLastUsed.TryGetValue(sp, out float last))
        {
            if (Time.time - last < spawnPointCooldown)
                return false;
        }

        // Check all pooled objects to see if any active one is near this position
        foreach (var kv in pools)
        {
            var list = kv.Value;
            if (list == null) continue;
            foreach (var go in list)
            {
                if (go == null) continue;
                if (!go.activeInHierarchy) continue;

                // If the active object is close to spawn point, consider it occupied
                if (Vector3.Distance(go.transform.position, pos) <= spawnPointBlockRadius)
                    return false;
            }
        }

        return true;
    }

    private void RecordSpawnAtPoint(Transform sp)
    {
        if (sp == null) return;
        if (!spawnPointLastUsed.ContainsKey(sp)) spawnPointLastUsed[sp] = -Mathf.Infinity;
        spawnPointLastUsed[sp] = Time.time;
    }

    // New API: spawn a single random enemy immediately with specific speed
    public void SpawnRandomEnemyImmediate(float speed = -1f)
    {
        if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0) return;

        Transform free = GetRandomFreeSpawnPoint();
        if (free == null) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        GameObject go = SpawnFromPool(prefab, free.position, Quaternion.identity);
        var runner = go.GetComponent<RunnerEnemy>();
        runner?.SetFallSpeed(speed > 0f ? speed : NormalEnemySpeed);

        RecordSpawnAtPoint(free);
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
