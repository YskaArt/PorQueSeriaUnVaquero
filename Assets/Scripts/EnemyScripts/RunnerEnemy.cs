// ---------------------------------------------------------------
// RunnerEnemy
// ---------------------------------------------------------------
// Controla el comportamiento de los enemigos que caen por la pantalla.
// - Se mueven hacia abajo usando fallSpeed.
// - Se eliminan automáticamente al pasar cierto límite o agotarse su lifetime.
// - Al ser eliminados por el jugador, otorgan oro mediante GoldManager.
// - Trabaja con object pooling: OnSpawn() reinicia su estado y ReturnToPool()
//   simplemente desactiva el objeto.
// ---------------------------------------------------------------

using UnityEngine;
using static EnemySpawner;

public class RunnerEnemy : MonoBehaviour, IPoolResettable
{
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float minY = -25f;
    [SerializeField] private float lifetime = 10f;

    private float lifetimeTimer;

    void Start()
    {
        lifetimeTimer = lifetime;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f || transform.position.y <= minY)
        {
            ReturnToPool();
        }
    }

    public void Eliminar()
    {
        if (GoldManager.Instance != null)
        {
            double reward = 1.0;

            if (EnemyGoldManager.Instance != null)
                reward = EnemyGoldManager.Instance.GetEnemyGoldReward();

            GoldManager.Instance.AddGold(reward);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);
    }

    public void OnSpawn()
    {
        lifetimeTimer = lifetime;
    }

    public void SetFallSpeed(float speed)
    {
        fallSpeed = speed;
    }

    public float GetFallSpeed()
    {
        return fallSpeed;
    }
}
