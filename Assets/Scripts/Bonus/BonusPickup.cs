using UnityEngine;

[DisallowMultipleComponent]
public class BonusPickup : MonoBehaviour, EnemySpawner.IPoolResettable
{
    [Header("Movement (Runner-like)")]
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float minY = -25f;
    [SerializeField] private float lifetime = 10f;
    private float lifetimeTimer;

    [Header("Pickup Settings")]
    [Tooltip("Si es true, se usará 'customDuration'. Si es false, BonusManager usará duración aleatoria min/max.")]
    [SerializeField] protected bool overrideDuration = false;

    [Tooltip("Duración del bonus SOLO si overrideDuration = true")]
    [SerializeField] protected float customDuration = 30f;

    [Header("Effects (optional)")]
    [SerializeField] protected GameObject pickupVFXPrefab;
    [SerializeField] protected AudioClip pickupSFX;
    [SerializeField] protected float destroyVFXAfter = 2f;

    // Internal flag protected para que la hija la reutilice
    protected bool picked = false;

    public void OnSpawn()
    {
        picked = false;
        gameObject.SetActive(true);
        lifetimeTimer = lifetime;
        var ps = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in ps) { p.Clear(); p.Play(); }

        Collider2D col2d = GetComponent<Collider2D>();
        if (col2d != null)
        {
            var rb2d = GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                rb2d = gameObject.AddComponent<Rigidbody2D>();
                rb2d.bodyType = RigidbodyType2D.Kinematic;
                rb2d.gravityScale = 0f;
                rb2d.simulated = true;
            }
        }
    }

    private void OnDisable()
    {
        picked = false;
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f || transform.position.y <= minY) ReturnToPool();
    }

    protected void ReturnToPool()
    {
        gameObject.SetActive(false);
    }

    public void SetFallSpeed(float speed) { fallSpeed = speed; }
    public float GetFallSpeed() => fallSpeed;

    protected virtual void OnTriggerEnter2D(Collider2D other) { TryPickup(other?.gameObject); }

    protected virtual void TryPickup(GameObject other)
    {
        if (picked) return;
        if (other == null) return;
        if (!other.CompareTag("Player"))
        {
            Debug.Log($"[BonusPickup] Collided with '{other.name}' but tag is '{other.tag}', expected 'Player'.");
            return;
        }

        picked = true;
        float durationToUse = overrideDuration ? Mathf.Max(0.1f, customDuration) : -1f;

        HandlePicked(durationToUse);
        ReturnToPool();
    }

    /// <summary>
    /// Activa bonus usando BonusManager (la versión normal, no boost).
    /// Está separado para que la hija pueda aplicar un boosted call en su lugar y
    /// luego usar estos efectos (VFX/SFX).
    /// </summary>
    protected virtual void HandlePicked(float duration)
    {
        if (BonusManager.Instance != null)
            BonusManager.Instance.ActivateRandomBonus(duration);
        else
            Debug.LogWarning("[BonusPickup] No hay BonusManager.Instance para activar el bonus.");

        if (pickupSFX != null) AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
        if (pickupVFXPrefab != null)
        {
            var vfx = Instantiate(pickupVFXPrefab, transform.position, Quaternion.identity);
            if (destroyVFXAfter > 0f) Destroy(vfx, destroyVFXAfter);
        }
    }
}
