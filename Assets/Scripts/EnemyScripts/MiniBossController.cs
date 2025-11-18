/*
    MiniBossController
    ------------------
    Controla por completo el comportamiento del MiniBoss, incluyendo:
    - Movimiento interpolado hacia posiciones objetivo.
    - Sistema de vida basado en cantidad de golpes.
    - Actualización de barra de vida.
    - Animaciones de caminar, recibir daño y morir.
    - Desactivación de colisiones al morir.
    - Invocación de un callback externo cuando el MiniBoss muere (usado por el MiniGameController).
    - Destrucción automática del objeto una vez completada la animación de muerte.
*/

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
    [SerializeField] private float deathDelay = 0.9f;

    private int currentHits = 0;
    private bool isDead = false;

    private Coroutine moveRoutine;
    private Action onDeath;

    // -------------------------
    // MOVIMIENTO
    // -------------------------
    public void MoveTo(Vector3 targetPos, Action onArrived)
    {
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
        if (isDead) return;

        currentHits++;
        if (anim != null) anim.SetTrigger("Hit");
        SFXManager.Instance.Play("RevolverShoot");
        if (healthBar != null)
            healthBar.fillAmount = 1f - (float)currentHits / Mathf.Max(1, totalHits);

        if (currentHits >= totalHits)
            StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (isDead) yield break;
        isDead = true;

        if (GoldManager.Instance != null)
        {
            double reward = 1.0;

            if (EnemyGoldManager.Instance != null)
                reward = EnemyGoldManager.Instance.GetEnemyGoldReward();

            GoldManager.Instance.AddGold(reward * 50);
        }
        if (anim != null) anim.SetTrigger("Die");

        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        float wait = Mathf.Max(0.05f, deathDelay);
        float timer = 0f;
        while (timer < wait)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        try
        {
            onDeath?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MiniBossController] Error al invocar onDeath: " + ex);
        }

        Destroy(gameObject);
    }

    // -------------------------
    // CALLBACK / UTIL
    // -------------------------
    public void AssignDeathCallback(Action callback)
    {
        onDeath = callback;
    }

    public void ClearDeathCallback()
    {
        onDeath = null;
    }
}
