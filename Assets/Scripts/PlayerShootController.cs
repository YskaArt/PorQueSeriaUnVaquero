using UnityEngine;
using System;
using System.Collections;
public class PlayerShootController : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private float shootSpeedNormal = 1f;
    [SerializeField] private float shootSpeedFast = 2f;

    private bool isShooting = false;

    public void StartShooting()
    {
        isShooting = true;
        anim.Play("Shoot");
        anim.speed = shootSpeedNormal;
    }

    public void StopShooting()
    {
        isShooting = false;
        anim.speed = 1f;
        anim.Play("Run");
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (isShooting && Input.touchCount > 0)
            anim.speed = shootSpeedFast;
        else if (isShooting)
            anim.speed = shootSpeedNormal;
#endif
    }

    // Evento en animación
    public void OnShootHit()
    {
        FindFirstObjectByType<MiniBossController>().TakeDamage();
    }   

    public void MoveOut(Action onComplete)
    {
        // Simplemente mover al jugador hacia arriba (puede mejorarse con tweening)
        StartCoroutine(MoveUpAndExit(onComplete));
    }

    private IEnumerator MoveUpAndExit(Action onComplete)
    {
        float duration = 1.5f;
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * 5f;

        while (t < duration)
        {
            transform.position = Vector3.Lerp(start, end, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        onComplete?.Invoke();
    }
}
