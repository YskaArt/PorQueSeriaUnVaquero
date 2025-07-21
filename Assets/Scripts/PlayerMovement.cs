using UnityEngine;

public class RunnerCharacter : MonoBehaviour
{
    private Vector3[] lanes = new Vector3[3];
    private int currentLane = 1; // 0 = izquierda, 1 = centro, 2 = derecha

    [SerializeField] private float laneChangeSpeed = 10f;

    private Vector2 touchStart;
    private Vector2 touchEnd;
    private bool isSwiping = false;

    void Start()
    {
        // Definimos las posiciones X fijas
        lanes[0] = new Vector3(-3.45f, transform.position.y, transform.position.z); // Izquierda
        lanes[1] = new Vector3(-1.1f, transform.position.y, transform.position.z);  // Centro
        lanes[2] = new Vector3(1.35f, transform.position.y, transform.position.z);  // Derecha
    }

    void Update()
    {

        HandleTouchInput();

        // Movimiento suave entre carriles
        Vector3 targetPosition = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * laneChangeSpeed);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Inicia el swipe
            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                isSwiping = true;
            }

            // Termina el swipe
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                touchEnd = touch.position;
                Vector2 swipe = touchEnd - touchStart;

                if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y) && Mathf.Abs(swipe.x) > 50f)
                {
                    if (swipe.x > 0)
                        MoveLane(1); // Derecha
                    else
                        MoveLane(-1); // Izquierda
                }

                isSwiping = false;
            }
        }
    }

    void MoveLane(int direction)
    {
        currentLane += direction;
        currentLane = Mathf.Clamp(currentLane, 0, 2);
    }
}
