using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class MiniBossController : MonoBehaviour
{
    [Header("Anim & UI")]
    [SerializeField] private Animator anim;
    [SerializeField] private Image healthBar;

    [Header("Vida / daño")]
    [SerializeField] private int totalHits = 10;
    [SerializeField] private float deathDelay = 0.9f; // tiempo para esperar anim de muerte antes de callback/destruir

    private int currentHits = 0;
    private bool isDead = false;

    // movimiento
    private Coroutine moveRoutine;

    // Callback asignado por el MiniGameController
    private Action onDeath;

    // -------------------------
    // MOVIMIENTO
    // -------------------------
    public void MoveTo(Vector3 targetPos, Action onArrived)
    {
        // Cancelar corrutina previa si existe
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(targetPos, onArrived));
    }

    private IEnumerator MoveRoutine(Vector3 target, Action onArrived)
    {
        float t = 0f;
        Vector3 start = transform.position;
        if (anim != null) anim.SetBool("IsWalking", true);

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(start, target, t);
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        if (anim != null) anim.SetBool("IsWalking", false);
        moveRoutine = null;
        onArrived?.Invoke();
    }

    // -------------------------
    // DAÑO / MUERTE
    // -------------------------
    public void TakeDamage()
    {
        if (isDead) return; // protección contra hits extra

        currentHits++;
        if (anim != null) anim.SetTrigger("Hit");

        if (healthBar != null)
            healthBar.fillAmount = 1f - (float)currentHits / Mathf.Max(1, totalHits);

        if (currentHits >= totalHits)
            StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (isDead) yield break;
        isDead = true;

        // Añadir gold, disparar anim de muerte
        GoldManager.Instance?.AddGold(50);
        if (anim != null) anim.SetTrigger("Die");

        // Desactivar colisiones / lógica de daño para evitar más eventos
        var colliders2D = GetComponentsInChildren<Collider2D>();
        foreach (var c in colliders2D) c.enabled = false;
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders) c.enabled = false;

        // Esperar a que la animación de muerte se reproduzca (o deathDelay)
        float wait = Mathf.Max(0.05f, deathDelay);
        float timer = 0f;
        while (timer < wait)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Invocar callback UNA VEZ (si existe)
        try
        {
            onDeath?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MiniBossController] Error al invocar onDeath: " + ex);
        }

        // Opcional: destruir el boss después de la muerte para limpiar la escena
        Destroy(gameObject);
        yield break;
    }

    // -------------------------
    // CALLBACK / UTIL
    // -------------------------
    /// <summary>
    /// Asigna (o reemplaza) el callback que será llamado una vez al morir.
    /// </summary>
    public void AssignDeathCallback(Action callback)
    {
        onDeath = callback; // reemplaza, así evitamos doble suscripción accidental
    }

    /// <summary>
    /// Limpia el callback (por seguridad).
    /// </summary>
    public void ClearDeathCallback()
    {
        onDeath = null;
    }
}
