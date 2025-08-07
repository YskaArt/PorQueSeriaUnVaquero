using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class MiniBossController : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private Image healthBar;
    [SerializeField] private int totalHits = 10;

    private int currentHits = 0;
    private Action onDeath;

    public void MoveTo(Vector3 targetPos, Action onArrived)
    {
        StartCoroutine(MoveRoutine(targetPos, onArrived));
    }

    private IEnumerator MoveRoutine(Vector3 target, Action onArrived)
    {
        float t = 0f;
        Vector3 start = transform.position;

        // Activar animación de caminar
        anim.SetBool("IsWalking", true);

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(start, target, t);
            t += Time.deltaTime;
            yield return null;
        }

        // Detener animación de caminar
        anim.SetBool("IsWalking", false);

        onArrived?.Invoke();
    }

    public void TakeDamage()
    {
        currentHits++;
        anim.SetTrigger("Hit");
        healthBar.fillAmount = 1f - (float)currentHits / totalHits;

        if (currentHits >= totalHits)
        {
            Die();
        }
    }

    private void Die()
    {
        GoldManager.Instance.AddGold(50);
        anim.SetTrigger("Die");
        GetComponent<MiniGameController>().OnEnemyDefeated();
    }
}
