using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class MiniBossController : MonoBehaviour
{
    // Referencia al Animator para controlar animaciones del miniboss.
    [SerializeField] private Animator anim;

    // Imagen que representa la barra de vida en la UI.
    [SerializeField] private Image healthBar;

    // Cantidad total de golpes que necesita para ser derrotado.
    [SerializeField] private int totalHits = 10;

    // Contador de golpes actuales recibidos.
    private int currentHits = 0;

    // Acción opcional que se puede invocar al morir (callback externo).
    private Action onDeath;

    // MÉTODO: MoveTo()
    // Inicia la corrutina para mover al miniboss hasta una posición objetivo.
    // Al llegar, ejecuta la acción onArrived (si se pasa).
    public void MoveTo(Vector3 targetPos, Action onArrived)
    {
        StartCoroutine(MoveRoutine(targetPos, onArrived));
    }

    // MÉTODO: MoveRoutine()
    // Corrutina encargada del movimiento suave hacia un punto.
    // - Activa animación de caminar mientras se mueve.
    // - Usa Vector3.Lerp para interpolar entre la posición inicial y el destino.
    // - Cuando llega, detiene la animación y ejecuta la acción onArrived.
    private IEnumerator MoveRoutine(Vector3 target, Action onArrived)
    {
        float t = 0f;
        Vector3 start = transform.position;

        // Activa animación de caminar
        anim.SetBool("IsWalking", true);

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(start, target, t);
            t += Time.deltaTime; // Incrementa t con el tiempo para interpolación
            yield return null;
        }

        // Detiene animación de caminar
        anim.SetBool("IsWalking", false);

        // Ejecuta el callback al llegar
        onArrived?.Invoke();
    }

    // MÉTODO: TakeDamage()
    // Maneja el daño recibido por el miniboss:
    // - Aumenta contador de golpes.
    // - Activa animación de golpe ("Hit").
    // - Actualiza la barra de vida en base a los golpes recibidos.
    // - Si los golpes igualan o superan totalHits, llama a Die().
    public void TakeDamage()
    {
        currentHits++;
        anim.SetTrigger("Hit");

        // Ajusta el fillAmount de la barra (vida restante)
        healthBar.fillAmount = 1f - (float)currentHits / totalHits;

        if (currentHits >= totalHits)
        {
            Die();
        }
    }

    // MÉTODO: Die()
    // Lógica al morir el miniboss:
    // - Agrega oro al jugador usando GoldManager.
    // - Dispara animación de muerte.
    // - Notifica al MiniGameController que el enemigo fue derrotado.
    private void Die()
    {
        GoldManager.Instance.AddGold(50);
        anim.SetTrigger("Die");
        GetComponent<MiniGameController>().OnEnemyDefeated();
    }
}
