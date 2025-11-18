/*
    PlayerShootController
    ---------------------
    Controla toda la lógica de disparo del jugador durante el minijuego del MiniBoss.

    FUNCIONALIDADES PRINCIPALES:
    • Activa y detiene la animación de disparo.
    • Cambia la velocidad de la animación según el input del jugador:
        - Velocidad normal mientras dispara.
        - Velocidad rápida cuando se mantiene el dedo en la pantalla (solo móviles).
    • Recibe un evento de animación ("OnShootHit") para aplicar daño al MiniBoss.
    • Permite desplazar al jugador hacia arriba (fuera de la pantalla) al finalizar el minijuego.
      Esto se hace mediante una coroutine con un callback (Action) que se ejecuta al terminar.

    NOTAS:
    • Está pensado para usarse solo dentro del minijuego del MiniBoss.
    • Usa FindFirstObjectByType para obtener el MiniBossController en cada impacto, lo cual
      funciona pero puede optimizarse cacheando la referencia (cuando quieras mejorar rendimiento).
*/

using UnityEngine;
using System;
using System.Collections;

public class PlayerShootController : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private float shootSpeedNormal = 1f;
    [SerializeField] private float shootSpeedFast = 2f;

    private bool isShooting = false;
    private MiniBossController targetBoss;

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

        if (isShooting && Input.touchCount > 0)
            anim.speed = shootSpeedFast;
        else if (isShooting)
            anim.speed = shootSpeedNormal;

    }

    // Ahora usa el target cacheado asignado por MiniGameController.
    public void OnShootHit()
    {
        if (targetBoss != null)
        {
            targetBoss.TakeDamage();
            return;
        }

        // Fallback: intentar encontrar uno (solo si no hay target asignado).
        var mb = FindFirstObjectByType<MiniBossController>();
        if (mb != null)
            mb.TakeDamage();
    }

    public void MoveOut(Action onComplete)
    {
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

    // API pública para que MiniGameController (u otro) asignen el boss activo
    public void SetTarget(MiniBossController boss)
    {
        targetBoss = boss;
    }
}

