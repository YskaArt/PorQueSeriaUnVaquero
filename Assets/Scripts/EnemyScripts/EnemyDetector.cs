using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    [SerializeField] private Animator anim;
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificamos si el objeto que entró tiene el componente RunnerEnemy
        RunnerEnemy enemigo = other.GetComponent<RunnerEnemy>();
        anim.SetTrigger("Attack");

        if (enemigo != null)
        {
          
            enemigo.Eliminar(); // Llama a la función pública para destruir al enemigo
        }
    }
}
