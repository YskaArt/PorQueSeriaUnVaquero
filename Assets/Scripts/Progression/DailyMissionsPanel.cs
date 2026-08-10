/*
 * DailyMissionsPanel
 * ------------------
 * Panel de misiones diarias. Muestra las misiones activas del día usando
 * un MissionEntryUI por fila (crear 3 filas en el editor y asignarlas).
 *
 * WIRING EN EL EDITOR:
 * - panelRoot: GameObject del panel completo.
 * - entries: las filas MissionEntryUI (tantas como missionsPerDay, normalmente 3).
 * - closeButton: cierra el panel.
 * - pendingBadge (opcional): GameObject sobre el botón del HUD que avisa
 *   que hay recompensas sin reclamar (se controla aunque el panel esté cerrado).
 *
 * Conectar OpenPanel() al botón de Misiones del HUD.
 */

using UnityEngine;
using UnityEngine.UI;

public class DailyMissionsPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Filas de misiones")]
    [SerializeField] private MissionEntryUI[] entries;

    [Header("Badge de recompensas pendientes (opcional)")]
    [SerializeField] private GameObject pendingBadge;

    [Header("Refresco")]
    [SerializeField] private float refreshInterval = 0.5f;
    private float refreshTimer;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    private void Start()
    {
        if (DailyMissionManager.Instance != null)
            DailyMissionManager.Instance.OnMissionsChanged += RebindAll;

        if (panelRoot != null) panelRoot.SetActive(false);
        RebindAll();
    }

    private void OnDestroy()
    {
        if (DailyMissionManager.Instance != null)
            DailyMissionManager.Instance.OnMissionsChanged -= RebindAll;
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < refreshInterval) return;
        refreshTimer = 0f;

        // Badge siempre actualizado (aunque el panel esté cerrado)
        if (pendingBadge != null && DailyMissionManager.Instance != null)
            pendingBadge.SetActive(DailyMissionManager.Instance.PendingClaimCount() > 0);

        // El progreso de las filas solo mientras el panel está abierto
        if (panelRoot != null && panelRoot.activeSelf && entries != null)
        {
            foreach (var e in entries)
                if (e != null) e.Refresh();
        }
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        RebindAll();
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void RebindAll()
    {
        if (entries == null) return;

        var manager = DailyMissionManager.Instance;
        var missions = manager != null ? manager.ActiveMissions : null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null) continue;
            entries[i].Bind(missions != null && i < missions.Count ? missions[i] : null);
        }
    }
}
