using UnityEngine;

public class HorseCooldownManager : MonoBehaviour
{
    // Instancia única para el patrón Singleton.
    public static HorseCooldownManager Instance;

    // Duración total del cooldown (en segundos).
    [SerializeField] private float cooldownDuration = 180f;

    // Tiempo restante del cooldown.
    private float currentCooldown = 0f;

    // MÉTODO: Awake()
    // Configura el patrón Singleton. 
    // Asegura que solo exista una instancia y se mantenga entre escenas.
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persiste entre cargas de escena.
        }
        else
        {
            Destroy(gameObject); // Si ya existe, destruye el duplicado.
        }
    }

    // MÉTODO: Update()
    // Reduce el contador del cooldown en cada frame hasta llegar a 0.
    private void Update()
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime; // Resta tiempo real.
            if (currentCooldown < 0f) currentCooldown = 0f; // Evita números negativos.
        }
    }

    // MÉTODO: IsReady()
    // Devuelve true si el cooldown terminó (está listo para usar).
    public bool IsReady()
    {
        return currentCooldown <= 0f;
    }

    // MÉTODO: StartCooldown()
    // Reinicia el contador, comenzando un nuevo cooldown completo.
    public void StartCooldown()
    {
        currentCooldown = cooldownDuration;
    }

    // MÉTODO: GetCooldownProgress()
    // Retorna el progreso del cooldown (1 = en cooldown, 0 = listo).
    // Útil para llenar una barra de progreso en UI.
    public float GetCooldownProgress()
    {
        return currentCooldown / cooldownDuration;
    }
}
