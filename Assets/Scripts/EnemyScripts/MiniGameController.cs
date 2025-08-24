using UnityEngine;

public class MiniGameController : MonoBehaviour
{
    [Header("Refs")]
    // Referencias principales necesarias para el minijuego.
    [SerializeField] private InfiniteTilemapLoop tilemapScroller; // Controla el scroll del fondo.
    [SerializeField] private GameObject specialEnemy; // El jefe/miniboss que aparece en el minijuego.
    [SerializeField] private Transform enemyTargetPosition; // Posición donde el miniboss debe colocarse.
    [SerializeField] private PlayerShootController playerShooter; // Control del disparo del jugador.
    [SerializeField] private PlayerMovement playerMovement; // Control de movimiento del jugador.
    [SerializeField] private GameStartManager sceneManager; // Maneja el cambio de escenas.
    [SerializeField] private HorseSkillController horseSkill; // Controla la habilidad del caballo (habilita/deshabilita).

    [Header("Escenas siguientes (elige una al azar)")]
    // Lista de escenas posibles a cargar después del minijuego.
    [SerializeField] private string[] nextScenes;

    // MÉTODO: Awake()
    // Asegura que la referencia a HorseSkillController exista.
    private void Awake()
    {
        if (horseSkill == null)
            horseSkill = FindAnyObjectByType<HorseSkillController>();
    }

    // MÉTODO: StartMiniGame()
    // Activa el flujo inicial del minijuego:
    // - Bloquea la skill del caballo.
    // - Detiene el scroll del mapa.
    // - Centra al jugador en el carril del medio.
    // - Activa el miniboss y lo mueve hasta su posición objetivo.
    // - Cuando el miniboss llega, comienza el disparo automático del jugador.
    public void StartMiniGame()
    {
        if (horseSkill != null)
            horseSkill.SetMiniGameActive(true);

        if (tilemapScroller != null)
            tilemapScroller.ScrollSpeed = 0f;

        if (playerMovement != null)
            playerMovement.CenterToMiddleLane();

        if (specialEnemy != null && enemyTargetPosition != null)
        {
            specialEnemy.SetActive(true);
            var boss = specialEnemy.GetComponent<MiniBossController>();
            if (boss != null)
            {
                // Movimiento del miniboss con callback al llegar
                boss.MoveTo(enemyTargetPosition.position, () =>
                {
                    if (playerShooter != null)
                        playerShooter.StartShooting();
                });
            }
        }
    }

    // MÉTODO: OnEnemyDefeated()
    // Se llama cuando el miniboss muere:
    // - Detiene el disparo del jugador.
    // - Ejecuta la animación de salida del jugador.
    // - Habilita nuevamente la skill del caballo.
    // - Selecciona aleatoriamente una escena y solicita la transición.
    public void OnEnemyDefeated()
    {
        if (playerShooter != null)
            playerShooter.StopShooting();

        if (playerShooter != null)
        {
            // Movimiento de salida del jugador con callback.
            playerShooter.MoveOut(() =>
            {
                if (horseSkill != null)
                    horseSkill.SetMiniGameActive(false);

                string targetScene = PickRandomNextScene();
                if (!string.IsNullOrEmpty(targetScene) && sceneManager != null)
                {
                    sceneManager.EndSceneAndLoadNext(targetScene);
                }
                else
                {
                    Debug.LogWarning("MiniGameController: No hay escenas en nextScenes o falta SceneManager.");
                }
            });
        }
        else
        {
            // Si no hay animación de salida, fallback directo.
            if (horseSkill != null)
                horseSkill.SetMiniGameActive(false);

            string targetScene = PickRandomNextScene();
            if (!string.IsNullOrEmpty(targetScene) && sceneManager != null)
                sceneManager.EndSceneAndLoadNext(targetScene);
        }
    }

    // MÉTODO: PickRandomNextScene()
    // Devuelve el nombre de una escena aleatoria de la lista `nextScenes`.
    private string PickRandomNextScene()
    {
        if (nextScenes == null || nextScenes.Length == 0)
            return null;

        int index = Random.Range(0, nextScenes.Length);
        return nextScenes[index];
    }
}
