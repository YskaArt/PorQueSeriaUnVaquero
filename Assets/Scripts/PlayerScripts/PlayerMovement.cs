using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Vector3[] lanes = new Vector3[3]; // Posiciones fijas de los carriles (izq, centro, der)
    private int currentLane = 1; // Carril actual: 0=izquierda, 1=centro, 2=derecha

    [SerializeField] private float laneChangeSpeed = 10f; // Velocidad de interpolación entre carriles

    private Vector2 touchStart;
    private Vector2 touchEnd;
    private bool isSwiping = false;

    // Thresholds
    private const float swipeThreshold = 50f; // px
    private const float tapMaxDistance = 20f; // px (si no es swipe, se considera tap)

    // Cámara principal cacheada
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        lanes[0] = new Vector3(-3.45f, transform.position.y, transform.position.z); // Izquierda
        lanes[1] = new Vector3(-1.1f, transform.position.y, transform.position.z);  // Centro
        lanes[2] = new Vector3(1.35f, transform.position.y, transform.position.z);  // Derecha
    }

    void Update()
    {
        HandleTouchInput();

        // Movimiento suave hacia la posición X del carril actual
        Vector3 targetPosition = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * laneChangeSpeed);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                isSwiping = true;
            }
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isSwiping)
            {
                // opcional: podrías mostrar swipe visual
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                touchEnd = touch.position;
                Vector2 delta = touchEnd - touchStart;

                // Si hay un swipe horizontal grande -> cambia de carril
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > swipeThreshold)
                {
                    if (delta.x > 0)
                        MoveLane(1); // swipe a la derecha
                    else
                        MoveLane(-1); // swipe a la izquierda
                }
                else
                {
                    // Si no fue swipe, tratar como tap: seleccionar carril más cercano a la X tocada
                    if (Vector2.Distance(touchEnd, touchStart) <= tapMaxDistance)
                    {
                        TryMoveToNearestLane(touchEnd);
                    }
                    else
                    {
                        // Si fue un movimiento pequeño diagonal, también mapear a la posición
                        TryMoveToNearestLane(touchEnd);
                    }
                }

                isSwiping = false;
            }
        }
        else
        {
            // Soporte para click del editor (opcional)
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                touchStart = mousePos;
                isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && isSwiping)
            {
                Vector2 mousePos = Input.mousePosition;
                touchEnd = mousePos;
                Vector2 delta = touchEnd - touchStart;

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > swipeThreshold)
                {
                    if (delta.x > 0) MoveLane(1); else MoveLane(-1);
                }
                else
                {
                    TryMoveToNearestLane(mousePos);
                }

                isSwiping = false;
            }
        }
    }

    private void TryMoveToNearestLane(Vector2 screenPos)
    {
        if (mainCam == null) mainCam = Camera.main;
        // Convertir posición de las 3 lanes a pantalla y elegir la más cercana en X
        float bestDist = float.MaxValue;
        int bestLane = currentLane;

        for (int i = 0; i < lanes.Length; i++)
        {
            Vector3 laneWorld = lanes[i];
            Vector3 laneScreen = mainCam.WorldToScreenPoint(laneWorld);
            float dist = Mathf.Abs(screenPos.x - laneScreen.x);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestLane = i;
            }
        }

        currentLane = bestLane;
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
