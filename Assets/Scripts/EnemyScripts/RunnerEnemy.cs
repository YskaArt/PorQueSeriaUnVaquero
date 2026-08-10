// ---------------------------------------------------------------
// RunnerEnemy
// ---------------------------------------------------------------
// Controla el comportamiento de los enemigos que caen por la pantalla.
// - Se mueven hacia abajo usando fallSpeed.
// - Se eliminan autom�ticamente al pasar cierto l�mite o agotarse su lifetime.
// - Al ser eliminados por el jugador, otorgan oro mediante GoldManager.
// - Trabaja con object pooling: OnSpawn() reinicia su estado y ReturnToPool()
//   simplemente desactiva el objeto.
// ---------------------------------------------------------------

using UnityEngine;
using static EnemySpawner;

public class RunnerEnemy : MonoBehaviour, IPoolResettable
{
    [Header("Identidad (para misiones diarias)")]
    [Tooltip("Id único del tipo de enemigo (ej: \"Boxer\", \"Skeleton\", \"Slime\"). " +
             "Se usa para misiones de tipo 'eliminar N de este enemigo'. Dejar vacío = enemigo genérico.")]
    [SerializeField] private string enemyTypeId = "";

    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float minY = -25f;
    [SerializeField] private float lifetime = 10f;

    private float lifetimeTimer;

    public string EnemyTypeId => enemyTypeId;

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
        ParticleManager.Instance.PlayAtPosition(transform.position);
        if (GoldManager.Instance != null)
        {
            double reward = 1.0;

            if (EnemyGoldManager.Instance != null)
                reward = EnemyGoldManager.Instance.GetEnemyGoldReward();

            GoldManager.Instance.AddGold(reward);
        }

        DailyMissionManager.Instance?.ReportProgress(MissionType.KillEnemies, 1, enemyTypeId);
        ZoneMissionManager.Instance?.ReportProgress(MissionType.KillEnemies, 1, enemyTypeId);

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
