/*
    PlayerMovement
    ----------------
    Este script controla el movimiento del jugador entre 3 carriles fijos
    en un entorno top-down vertical. El jugador puede cambiar de carril
    mediante swipe o tap, con soporte tanto para dispositivos móviles como
    para mouse en el editor.

    Funcionalidad principal:
    - Define las posiciones de los 3 carriles (izq, centro, der).
    - Detecta gestos táctiles: swipe horizontal para cambiar carril,
      o tap corto para saltar al carril más cercano a la posición tocada.
    - Incluye una "deadzone" en el lado derecho donde los toques son ignorados
      (por ejemplo para no interferir con botones de UI).
    - Utiliza interpolación suave para moverse hacia el carril objetivo.
    - Durante el minijuego:
        * El movimiento se bloquea.
        * El jugador es forzado al carril central.
        * Se ignora todo input.
    - API pública:
        * SetLockedForMiniGame() bloquea/desbloquea movimiento.
        * CenterToMiddleLane() centra manualmente al jugador.
*/

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Vector3[] lanes = new Vector3[3];
    private int currentLane = 1; // 0=izq, 1=centro, 2=der

    [Header("Movimiento")]
    [SerializeField] private float laneChangeSpeed = 10f;

    // Touch thresholds
    private const float swipeThreshold = 50f;
    private const float tapMaxDistance = 20f;

    [Header("Zona muerta (UI)")]
    [Range(0.0f, 0.6f)]
    [SerializeField] private float rightDeadzonePercent = 0.30f;

    private bool lockedForMinigame = false;

    private Vector2 touchStart;
    private Vector2 touchEnd;
    private bool isSwiping = false;

    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
    }

    void Start()
    {
        lanes[0] = new Vector3(-3.45f, transform.position.y, transform.position.z);
        lanes[1] = new Vector3(-1.1f, transform.position.y, transform.position.z);
        lanes[2] = new Vector3(1.35f, transform.position.y, transform.position.z);

        CenterToMiddleLane();
    }

    void Update()
    {
        if (lockedForMinigame)
        {
            if (currentLane != 1) currentLane = 1;
            MoveTowardsLane();
            return;
        }

        HandleTouchInput();
        MoveTowardsLane();
    }

    private void MoveTowardsLane()
    {
        Vector3 targetPosition = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * laneChangeSpeed);
    }

    void HandleTouchInput()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        // ---- MÓVIL ----
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (IsInRightDeadzone(touch.position)) return;

            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                touchEnd = touch.position;
                Vector2 delta = touchEnd - touchStart;

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > swipeThreshold)
                    MoveLane(delta.x > 0 ? 1 : -1);
                else
                    TryMoveToNearestLane(touchEnd);

                isSwiping = false;
            }
        }
        // ---- PC / EDITOR ----
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 m = Input.mousePosition;
                if (IsInRightDeadzone(m)) return;
                touchStart = m;
                isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && isSwiping)
            {
                Vector2 m = Input.mousePosition;
                if (IsInRightDeadzone(m)) { isSwiping = false; return; }

                touchEnd = m;
                Vector2 delta = touchEnd - touchStart;

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > swipeThreshold)
                    MoveLane(delta.x > 0 ? 1 : -1);
                else
                    TryMoveToNearestLane(m);

                isSwiping = false;
            }
        }
    }

    private bool IsInRightDeadzone(Vector2 screenPos)
    {
        float deadzoneStartX = Screen.width * (1f - rightDeadzonePercent);
        return screenPos.x >= deadzoneStartX;
    }

    private void TryMoveToNearestLane(Vector2 screenPos)
    {
        if (mainCam == null) mainCam = Camera.main;

        float bestDist = float.MaxValue;
        int bestLane = currentLane;

        for (int i = 0; i < lanes.Length; i++)
        {
            Vector3 laneScreen = mainCam.WorldToScreenPoint(lanes[i]);
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
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
    }

    public void CenterToMiddleLane()
    {
        currentLane = 1;
        transform.position = new Vector3(lanes[currentLane].x, transform.position.y, transform.position.z);
    }

    public void SetLockedForMiniGame(bool locked)
    {
        lockedForMinigame = locked;
        if (lockedForMinigame) CenterToMiddleLane();
    }

    public bool IsLockedForMiniGame() => lockedForMinigame;
}
