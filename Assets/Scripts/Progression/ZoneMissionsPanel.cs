/*
 * ZoneMissionsPanel
 * ------------------
 * Panel de misiones de zona. Mismo patrón que DailyMissionsPanel, pero
 * escucha a ZoneMissionManager y usa ZoneMissionEntryUI por fila.
 *
 * WIRING EN EL EDITOR:
 * - panelRoot: GameObject del panel completo.
 * - entries: filas ZoneMissionEntryUI (tantas como missionsPerZone, normalmente 2).
 * - closeButton: cierra el panel.
 * - pendingBadge (opcional): recompensas sin reclamar.
 *
 * Conectar OpenPanel() al botón de Misiones de Zona del HUD.
 * (Puede ser el mismo botón "Misiones" con dos pestañas, o uno separado —
 * a definir en la Fase 3 de armado de escena.)
 */

using UnityEngine;
using UnityEngine.UI;

public class ZoneMissionsPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Filas de misiones")]
    [SerializeField] private ZoneMissionEntryUI[] entries;

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
        if (ZoneMissionManager.Instance != null)
            ZoneMissionManager.Instance.OnMissionsChanged += RebindAll;

        RebindAll();
    }

    private void OnDestroy()
    {
        if (ZoneMissionManager.Instance != null)
            ZoneMissionManager.Instance.OnMissionsChanged -= RebindAll;
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < refreshInterval) return;
        refreshTimer = 0f;

        if (pendingBadge != null && ZoneMissionManager.Instance != null)
            pendingBadge.SetActive(ZoneMissionManager.Instance.PendingClaimCount() > 0);

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

        var manager = ZoneMissionManager.Instance;
        var missions = manager != null ? manager.ActiveMissions : null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null) continue;
            entries[i].Bind(missions != null && i < missions.Count ? missions[i] : null);
        }
    }
}