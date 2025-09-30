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
    [Tooltip("Tiempo entre patrones (líneas de enemigos)")]
    [SerializeField] private float patternInterval = 5f;
    [Tooltip("Intervalo entre aparición de objetos bonus")]
    [SerializeField] private float bonusInterval = 20f;

    [Header("Pool")]
    [SerializeField] private Transform worldContainer;
    [SerializeField] private int initialPoolPerPrefab = 5;

    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
    private bool isSpawning;
    private float bonusTimer;
    private Coroutine spawnCoroutine;

    public bool IsHorseSkillActive { get; set; } = false;
    public float HorseSkillEnemySpeed { get; set; } = 1f;
    public float NormalEnemySpeed { get; set; } = 5f;

    public float MinSpawnTime { get => minSpawnTime; set => minSpawnTime = value; }
    public float MaxSpawnTime { get => maxSpawnTime; set => maxSpawnTime = value; }

    private void Awake()
    {
        if (worldContainer == null) worldContainer = this.transform;
        foreach (var prefab in enemyPrefabs)
            EnsurePoolFor(prefab);
        foreach (var prefab in bonusPrefabs)
            EnsurePoolFor(prefab);
    }

    private void Start()
    {
        isSpawning = true;
        bonusTimer = 0f;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        StopSpawning();
    }

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
        {
            if (!list[i].activeInHierarchy)
                return list[i];
        }
        var go = Instantiate(prefab, worldContainer);
        go.SetActive(false);
        list.Add(go);
        return go;
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));
        while (isSpawning)
        {
            // Si la habilidad está activa, spawnea patrones de enemigos sin esperar entre ellos
            if (IsHorseSkillActive)
            {
                yield return StartCoroutine(SpawnPattern());
                // No espera entre patrones, solo el stagger interno de cada patrón
            }
            else
            {
                // Comportamiento normal: espera entre patrones
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
    }

    private IEnumerator SpawnPattern()
    {
        if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0)
            yield break;
        int lineIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[lineIndex];
        int count = IsHorseSkillActive ? 3 : Random.Range(2, 5); // 3 si habilidad activa, 2-4 normal
        int prefabIndex = Random.Range(0, enemyPrefabs.Count);
        GameObject prefab = enemyPrefabs[prefabIndex];
        float stagger = 0.7f;
        for (int i = 0; i < count; i++)
        {
            var go = SpawnFromPool(prefab, spawnPoint.position, Quaternion.identity);
            var runner = go != null ? go.GetComponent<RunnerEnemy>() : null;
            if (runner != null)
            {
                runner.SetFallSpeed(IsHorseSkillActive ? HorseSkillEnemySpeed : NormalEnemySpeed);
            }
            yield return new WaitForSeconds(stagger);
        }
    }

    private void SpawnBonus()
    {
        if (bonusPrefabs.Count == 0 || spawnPoints.Length == 0) return;
        int prefabIndex = Random.Range(0, bonusPrefabs.Count);
        GameObject prefab = bonusPrefabs[prefabIndex];
        int pointIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[pointIndex];
        SpawnFromPool(prefab, spawnPoint.position, Quaternion.identity);
    }

    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var go = GetFromPool(prefab);
        if (go == null) return null;
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.SetParent(worldContainer);
        go.SetActive(true);
        var poolable = go.GetComponent<IPoolResettable>();
        if (poolable != null)
            poolable.OnSpawn();
        return go;
    }

    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        StopAllCoroutines();
    }
}

public interface IPoolResettable
{
    void OnSpawn();
}
