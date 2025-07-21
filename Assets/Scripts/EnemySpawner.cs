using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private Transform[] spawnPoints; // 3 puntos (izq, centro, der)
    [SerializeField] private float minSpawnTime = 2f;
    [SerializeField] private float maxSpawnTime = 7f;
    public float MinSpawnTime
    {
        get => minSpawnTime;
        set => minSpawnTime = value;
    }

    public float MaxSpawnTime
    {
        get => maxSpawnTime;
        set => maxSpawnTime = value;
    }
    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0)
                yield break;

            int randomEnemy = Random.Range(0, enemyPrefabs.Count);
            int randomPoint = Random.Range(0, spawnPoints.Length);

            Instantiate(enemyPrefabs[randomEnemy], spawnPoints[randomPoint].position, Quaternion.identity);
        }
    }
}
