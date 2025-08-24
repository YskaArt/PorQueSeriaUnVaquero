using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // Lista de prefabs de enemigos que se pueden generar.
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

    // Array de puntos en el escenario donde los enemigos pueden aparecer.
    // (Por ejemplo: izquierda, centro y derecha).
    [SerializeField] private Transform[] spawnPoints;

    // Tiempo mínimo y máximo entre spawns.
    [SerializeField] private float minSpawnTime = 2f;
    [SerializeField] private float maxSpawnTime = 7f;

    // Contenedor del mundo: los enemigos instanciados se asignarán como hijos de este objeto.
    [SerializeField] private Transform worldContainer;

    // Bandera para saber si el spawner está activo.
    private bool isSpawning;

    // Propiedades públicas para modificar los tiempos de spawn desde otros scripts.
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

    // MÉTODO: Start()
    // Activa el spawner al iniciar y lanza la corrutina de generación de enemigos.
    private void Start()
    {
        isSpawning = true;
        StartCoroutine(SpawnRoutine());
    }

    // MÉTODO: SpawnRoutine()
    // Corrutina principal encargada de instanciar enemigos en intervalos aleatorios.
    // - Espera un tiempo aleatorio entre minSpawnTime y maxSpawnTime.
    // - Elige un enemigo y un punto de spawn aleatorio.
    // - Instancia el enemigo como hijo de worldContainer.
    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            // Espera un tiempo aleatorio antes de spawnear el siguiente enemigo.
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            // Si no hay enemigos configurados o puntos de spawn, termina la corrutina.
            if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0)
                yield break;

            // Selección aleatoria del prefab y el punto de aparición.
            int randomEnemy = Random.Range(0, enemyPrefabs.Count);
            int randomPoint = Random.Range(0, spawnPoints.Length);

            // Instanciación del enemigo y lo asignamos al contenedor del mundo.
            Instantiate(enemyPrefabs[randomEnemy], spawnPoints[randomPoint].position, Quaternion.identity, worldContainer);
        }
    }

    // MÉTODO: StopSpawning()
    // Detiene el proceso de generación de enemigos.
    // - Cambia la bandera isSpawning a false.
    // - Detiene todas las corrutinas activas en este script (opcional).
    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines(); // Se asegura de detener la rutina de spawn inmediatamente.
    }
}
