using UnityEngine;

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
        return currentCooldown / cooldownDuration; // 1 = lleno, 0 = listo
    }
}
