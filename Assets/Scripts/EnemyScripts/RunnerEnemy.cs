using UnityEngine;

public class RunnerEnemy : MonoBehaviour
{
    // Velocidad de caída (debe coincidir con la velocidad del scroll del mapa).
    [SerializeField] private float fallSpeed = 5f;

    // Límite mínimo en Y antes de eliminar el objeto (por ejemplo, fuera de pantalla).
    [SerializeField] private float minY = -25f;

    // Tiempo máximo de vida del enemigo antes de autodestruirse (en segundos).
    [SerializeField] private float lifetime = 10f;

    // Temporizador interno que cuenta hacia atrás.
    private float lifetimeTimer;

    // MÉTODO: Start()
    // Inicializa el temporizador de vida con el valor configurado.
    void Start()
    {
        lifetimeTimer = lifetime;
    }

    // MÉTODO: Update()
    // Ejecuta cada frame:
    // - Mueve al enemigo hacia abajo en el eje Y (simulando scroll).
    // - Reduce el temporizador de vida.
    // - Destruye el objeto si se cumple alguna condición:
    //   (a) Tiempo agotado.
    //   (b) Salió del límite inferior de la pantalla.
    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f || transform.position.y <= minY)
        {
            Destroy(gameObject);
        }
    }

    // MÉTODO: Eliminar()
    // Método público para eliminar al enemigo manualmente (por ejemplo, cuando recibe un disparo).
    // - Suma oro al jugador usando GoldManager.
    // - Destruye el objeto.
    // Se puede ampliar para animación o efectos de muerte.
    public void Eliminar()
    {
        GoldManager.Instance.AddGold(1);
        Destroy(gameObject);
    }
}
