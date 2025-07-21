using UnityEngine;

public class RunnerEnemy : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 5f;   // Igual que el scroll del mapa
    [SerializeField] private float minY = -25f;       // Y mínimo antes de ser eliminado
    [SerializeField] private float lifetime = 10f;    // Tiempo máximo antes de autodestruirse

    private float lifetimeTimer;

    void Start()
    {
        lifetimeTimer = lifetime;
    }

    void Update()
    {
        // Movimiento hacia abajo
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Contador de vida
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f || transform.position.y <= minY)
        {
            Destroy(gameObject); // Auto-destrucción por tiempo o por salir de la pantalla
        }
    }

    // Este método se puede llamar desde un disparo o colisión
    public void Eliminar()
    {
        // Aquí podés agregar una animación o efecto de muerte si querés
        GoldManager.Instance.AddGold(1);
        Destroy(gameObject);
    }
}
