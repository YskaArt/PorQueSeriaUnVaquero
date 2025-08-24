using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Vector3[] lanes = new Vector3[3]; // Posiciones fijas de los carriles (izq, centro, der)
    private int currentLane = 1; // Carril actual: 0=izquierda, 1=centro, 2=derecha

    [SerializeField] private float laneChangeSpeed = 10f; // Velocidad de interpolación entre carriles

    private Vector2 touchStart;
    private Vector2 touchEnd;
    private bool isSwiping = false;

    // MÉTODO: Start()
    // Define las posiciones X de los 3 carriles al inicio
    void Start()
    {
        lanes[0] = new Vector3(-3.45f, transform.position.y, transform.position.z); // Izquierda
        lanes[1] = new Vector3(-1.1f, transform.position.y, transform.position.z);  // Centro
        lanes[2] = new Vector3(1.35f, transform.position.y, transform.position.z);  // Derecha
    }

    // MÉTODO: Update()
    // Maneja input táctil y movimiento suave entre carriles
    void Update()
    {
        HandleTouchInput();

        // Movimiento suave hacia la posición X del carril actual
        Vector3 targetPosition = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * laneChangeSpeed);
    }

    // MÉTODO: HandleTouchInput()
    // Detecta swipes horizontales para cambiar de carril
    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Inicio del swipe
            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                isSwiping = true;
            }

            // Fin del swipe
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                touchEnd = touch.position;
                Vector2 swipe = touchEnd - touchStart;

                // Detecta swipe horizontal significativo
                if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y) && Mathf.Abs(swipe.x) > 50f)
                {
                    if (swipe.x > 0)
                        MoveLane(1); // Swipe derecha → carril derecho
                    else
                        MoveLane(-1); // Swipe izquierda → carril izquierdo
                }

                isSwiping = false;
            }
        }
    }

    // MÉTODO: MoveLane(int direction)
    // Cambia el carril actual en la dirección indicada y lo mantiene dentro de 0-2
    void MoveLane(int direction)
    {
        currentLane += direction;
        currentLane = Mathf.Clamp(currentLane, 0, 2);
    }

    // MÉTODO: CenterToMiddleLane()
    // Centra al jugador en el carril medio (usado, por ejemplo, al iniciar un minijuego)
    public void CenterToMiddleLane()
    {
        currentLane = 1;
        transform.position = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
    }
}
