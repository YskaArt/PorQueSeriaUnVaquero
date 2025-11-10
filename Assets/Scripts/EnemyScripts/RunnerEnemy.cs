using UnityEngine;
using static EnemySpawner;

public class RunnerEnemy : MonoBehaviour, IPoolResettable
{
    // Velocidad de caída (debe coincidir con la velocidad del scroll del mapa).
    [SerializeField] private float fallSpeed = 5f;

    // Límite mínimo en Y antes de eliminar el objeto (por ejemplo, fuera de pantalla).
    [SerializeField] private float minY = -25f;

    // Tiempo máximo de vida del enemigo antes de volver a la pool (en segundos).
    [SerializeField] private float lifetime = 10f;

    // Temporizador interno que cuenta hacia atrás.
    private float lifetimeTimer;

    // M TODO: Start() -> ahora inicializamos en OnSpawn (pool)
    void Start()
    {
        // Si se instancia sin pasar por la pool, inicializar el temporizador
        lifetimeTimer = lifetime;
    }

    void Update()
    {
        // Solo actualizar si el objeto está activo
        if (!gameObject.activeInHierarchy) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f || transform.position.y <= minY)
        {
            ReturnToPool();
        }
    }

    // Método público para eliminar al enemigo manualmente (por ejemplo, cuando recibe un disparo).
    // - Suma oro al jugador usando GoldManager.
    // - Devuelve el objeto a la pool en vez de destruirlo.
    public void Eliminar()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(1);

        ReturnToPool();
    }

    // Devuelve el objeto a la pool (lo desactiva)
    private void ReturnToPool()
    {
        // Aquí puedes resetear efectos, partículas, etc. antes de desactivar.
        gameObject.SetActive(false);
    }

    // Implementación de IPoolResettable
    public void OnSpawn()
    {
        // Reiniciar temporizador y cualquier estado necesario al salir de la pool
        lifetimeTimer = lifetime;
    }

    // Nuevo: Permite modificar la velocidad desde fuera
    public void SetFallSpeed(float speed)
    {
        fallSpeed = speed;
    }

    // Nuevo: Permite leer la velocidad actual
    public float GetFallSpeed()
    {
        return fallSpeed;
    }

}
