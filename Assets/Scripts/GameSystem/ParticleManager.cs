using System;
using System.Collections.Generic;
using UnityEngine;

///
/// ParticleManager
/// ---------------
/// Manager central de partículas con pooling.
/// Se usa para efectos de muerte de enemigos.
/// El sistema:
/// - Pre-instancia un pool de ParticleSystems.
/// - Cuando se solicita una partícula, la toma del pool.
/// - La coloca en la posición deseada, la reproduce y la devuelve al pool cuando termina.
///

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private ParticleSystem particlePrefab;
    [SerializeField] private int initialPoolSize = 10;

    private readonly Queue<ParticleSystem> pool = new Queue<ParticleSystem>();


    private void Awake()
    {
        // Singleton simple
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }


    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewParticleInPool();
        }
    }

    private ParticleSystem CreateNewParticleInPool()
    {
        ParticleSystem ps = Instantiate(particlePrefab, transform);
        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);
        return ps;
    }

    /// <summary>
    /// Usa un ParticleSystem del pool, lo coloca en la posición indicada
    /// y ejecuta la animación una vez.
    /// </summary>
    public void PlayAtPosition(Vector3 position)
    {
        if (pool.Count == 0)
            CreateNewParticleInPool();

        ParticleSystem ps = pool.Dequeue();
        ps.transform.position = position;
        ps.gameObject.SetActive(true);

        ps.Play(true);

        // Cuando termine, devolverlo a la pool
        StartCoroutine(ReturnToPoolWhenFinished(ps));
    }

    private System.Collections.IEnumerator ReturnToPoolWhenFinished(ParticleSystem ps)
    {
        // Esperar a que termine completamente
        while (ps.IsAlive(true))
            yield return null;

        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);
    }
}
