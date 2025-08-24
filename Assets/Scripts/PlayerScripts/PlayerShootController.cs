using UnityEngine;
using System;
using System.Collections;

public class PlayerShootController : MonoBehaviour
{
    [SerializeField] private Animator anim;           // Animador del jugador
    [SerializeField] private float shootSpeedNormal = 1f; // Velocidad normal de animación de disparo
    [SerializeField] private float shootSpeedFast = 2f;   // Velocidad rápida (cuando se toca la pantalla)

    private bool isShooting = false;                  // Indica si el jugador está disparando

    // MÉTODO: StartShooting()
    // Activa la animación de disparo y ajusta la velocidad normal
    public void StartShooting()
    {
        isShooting = true;
        anim.Play("Shoot");
        anim.speed = shootSpeedNormal;
    }

    // MÉTODO: StopShooting()
    // Detiene el disparo y vuelve a animación de correr
    public void StopShooting()
    {
        isShooting = false;
        anim.speed = 1f;
        anim.Play("Run");
    }

    // MÉTODO: Update()
    // Detecta input táctil para acelerar la animación de disparo en móviles
    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (isShooting && Input.touchCount > 0)
            anim.speed = shootSpeedFast;
        else if (isShooting)
            anim.speed = shootSpeedNormal;
#endif
    }

    // MÉTODO: OnShootHit()
    // Llamado desde un evento de animación; inflige daño al MiniBoss
    public void OnShootHit()
    {
        FindFirstObjectByType<MiniBossController>().TakeDamage();
    }

    // MÉTODO: MoveOut(Action onComplete)
    // Mueve al jugador hacia arriba fuera de la pantalla tras terminar el minijuego
    public void MoveOut(Action onComplete)
    {
        StartCoroutine(MoveUpAndExit(onComplete));
    }

    // COROUTINE: MoveUpAndExit(Action onComplete)
    // Lerp simple para mover al jugador hacia arriba durante 1.5s y llamar callback
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
