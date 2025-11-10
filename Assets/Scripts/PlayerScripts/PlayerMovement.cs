using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float laneChangeSpeed = 10f;
    [SerializeField, Range(0.1f, 1f)] private float activeScreenWidth = 0.7f;
    // Porción de la pantalla donde los toques cuentan para moverse (ej: 0.7 = 70% izquierda)

    private Vector3[] lanes = new Vector3[3];
    private int currentLane = 1;

    private Vector2 touchStart;
    private Vector2 touchEnd;
    private bool isSwiping = false;

    void Start()
    {
        lanes[0] = new Vector3(-3.45f, transform.position.y, transform.position.z); // Izquierda
        lanes[1] = new Vector3(-1.1f, transform.position.y, transform.position.z);  // Centro
        lanes[2] = new Vector3(1.35f, transform.position.y, transform.position.z);  // Derecha
    }

    void Update()
    {
        HandleTouchInput();

        // Movimiento suave hacia el carril actual
        Vector3 targetPosition = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * laneChangeSpeed);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount <= 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStart = touch.position;
            isSwiping = true;
        }
        else if (touch.phase == TouchPhase.Ended && isSwiping)
        {
            touchEnd = touch.position;
            Vector2 swipe = touchEnd - touchStart;

            // Detección de swipe horizontal
            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y) && Mathf.Abs(swipe.x) > 50f)
            {
                if (swipe.x > 0)
                    MoveLane(1);
                else
                    MoveLane(-1);
            }
            else
            {
                // Si no fue un swipe, interpretamos como toque directo en pantalla
                HandleTap(touchEnd);
            }

            isSwiping = false;
        }
    }

    void HandleTap(Vector2 tapPosition)
    {
        float screenWidth = Screen.width;
        float validWidth = screenWidth * activeScreenWidth; // Solo el área izquierda cuenta
        float rightMargin = screenWidth - validWidth;

        // Si tocó dentro del área válida (izquierda)
        if (tapPosition.x < validWidth)
        {
            float relativeX = tapPosition.x / validWidth; // Normalizamos dentro del área válida (0 a 1)

            if (relativeX < 0.33f)
                currentLane = 0; // Carril izquierdo
            else if (relativeX < 0.66f)
                currentLane = 1; // Carril central
            else
                currentLane = 2; // Carril derecho
        }
        else
        {
            // Ignorar toques sobre la parte derecha de la interfaz
            Debug.Log("Toque en zona de interfaz (ignorado)");
        }
    }

    void MoveLane(int direction)
    {
        currentLane += direction;
        currentLane = Mathf.Clamp(currentLane, 0, 2);
    }

    public void CenterToMiddleLane()
    {
        currentLane = 1;
        transform.position = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
    }
}
