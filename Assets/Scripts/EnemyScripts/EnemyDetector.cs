/// <summary>
/// Detecta enemigos que ingresan en el trigger del jugador.
/// Al detectar un RunnerEnemy, reproduce la animación de ataque y elimina al enemigo.
/// Usado como zona de detección frente al personaje.
/// </summary>
using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    [SerializeField] private Animator anim;

    private void OnTriggerEnter2D(Collider2D other)
    {
        RunnerEnemy enemigo = other.GetComponent<RunnerEnemy>();

        anim.SetTrigger("Attack");

        if (enemigo != null)
        {
            enemigo.Eliminar();
        }
    }
}
