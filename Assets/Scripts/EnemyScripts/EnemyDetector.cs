/// <summary>
/// Detecta enemigos que ingresan en el trigger del jugador.
/// Al detectar un RunnerEnemy, reproduce la animación de ataque y elimina al enemigo.
/// Usado como zona de detección frente al personaje.
/// </summary>
using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private HorseSkillController horseSkill;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (horseSkill == null || !horseSkill.IsActive)
        {
            SFXManager.Instance.Play("Attack");
        }
        else
        {
            SFXManager.Instance.Play("Horse");
        }
        RunnerEnemy enemigo = other.GetComponent<RunnerEnemy>();

       
        anim.SetTrigger("Attack");

        if (enemigo != null)
        {
            enemigo.Eliminar();
        }
    }
}
