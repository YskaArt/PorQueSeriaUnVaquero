using UnityEngine;

/// <summary>
/// Gestiona un cooldown global para el sistema del caballo mediante un Singleton.
/// Lleva el tiempo restante, avanza el cooldown cada frame y expone métodos para
/// consultar su estado, reiniciarlo, modificarlo (al cargar partida) y obtener
/// datos útiles para la UI como el progreso y el tiempo restante.
/// </summary>
public class HorseCooldownManager : MonoBehaviour
{
    public static HorseCooldownManager Instance;

    [SerializeField] private float cooldownDuration = 180f;

    private float currentCooldown = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown < 0f) currentCooldown = 0f;
        }
    }

    public bool IsReady()
    {
        return currentCooldown <= 0f;
    }

    public void StartCooldown()
    {
        currentCooldown = cooldownDuration;
    }

    public float GetCooldownProgress()
    {
        return currentCooldown / cooldownDuration;
    }

    public float GetRemainingCooldown()
    {
        return Mathf.Max(currentCooldown, 0f);
    }

    public void SetRemainingCooldown(float seconds)
    {
        currentCooldown = Mathf.Clamp(seconds, 0f, cooldownDuration);
    }

    public float GetCooldownDuration()
    {
        return cooldownDuration;
    }
}
