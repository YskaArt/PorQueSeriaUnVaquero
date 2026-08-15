/*
 * MissionsHubPanel
 * ----------------
 * Panel ÚNICO de Misiones con dos pestañas: Daily y Zona.
 * No reemplaza a DailyMissionsPanel ni a ZoneMissionsPanel: los USA tal cual,
 * pero el "panelRoot" de cada uno pasa a ser el contenedor de SU pestaña
 * (no el diálogo completo). Este script controla el diálogo exterior y
 * decide cuál de los dos contenidos mostrar.
 *
 * JERARQUÍA ESPERADA EN EL EDITOR (ver guía paso a paso aparte):
 *
 * MissionsHubPanel (este script)
 *  └─ DialogRoot                     <- asignar a "panelRoot"
 *      ├─ Header
 *      │   ├─ TabDaily (Button)      <- dailyTabButton
 *      │   ├─ TabZone  (Button)      <- zoneTabButton
 *      │   └─ CloseButton (Button)   <- closeButton
 *      ├─ DailyContent               <- asignar como "panelRoot" DENTRO del
 *      │   (3x MissionEntryUI)          componente DailyMissionsPanel
 *      └─ ZoneContent                <- asignar como "panelRoot" DENTRO del
 *          (2x ZoneMissionEntryUI)      componente ZoneMissionsPanel
 *
 * WIRING:
 * - Conectar el botón "Misiones" del HUD a MissionsHubPanel.OpenPanel().
 * - hudBadge (opcional): GameObject sobre ESE botón del HUD que se prende
 *   si hay CUALQUIER recompensa (daily o zona) sin reclamar, sin importar
 *   si el panel está abierto o cerrado.
 * - tabDailyBadge / tabZoneBadge (opcional): puntito sobre cada pestaña
 *   indicando que ESA pestaña en particular tiene algo para reclamar.
 */

using UnityEngine;
using UnityEngine.UI;

public class MissionsHubPanel : MonoBehaviour
{
    public enum Tab { Daily, Zone }

    [Header("Diálogo")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Pestañas")]
    [SerializeField] private Button dailyTabButton;
    [SerializeField] private Button zoneTabButton;
    [SerializeField] private Tab defaultTab = Tab.Daily;

    [Header("Paneles internos (ya existentes)")]
    [SerializeField] private DailyMissionsPanel dailyMissionsPanel;
    [SerializeField] private ZoneMissionsPanel zoneMissionsPanel;

    [Header("Resaltado de pestaña activa (opcional)")]
    [Tooltip("GameObject (ej: una barrita/underline) que se activa bajo la pestaña Daily cuando está seleccionada")]
    [SerializeField] private GameObject dailyTabSelectedMark;
    [SerializeField] private GameObject zoneTabSelectedMark;

    [Header("Badges de pendientes (opcionales)")]
    [Tooltip("Badge sobre el botón 'Misiones' del HUD (fuera de este panel)")]
    [SerializeField] private GameObject hudBadge;
    [SerializeField] private GameObject tabDailyBadge;
    [SerializeField] private GameObject tabZoneBadge;

    [Header("Refresco de badges")]
    [SerializeField] private float badgeRefreshInterval = 0.5f;
    private float badgeRefreshTimer;

    private bool hasOpenedOnce;
    private Tab currentTab;

    private void Awake()
    {
        
        if (dailyTabButton != null) dailyTabButton.onClick.AddListener(ShowDailyTab);
        if (zoneTabButton != null) zoneTabButton.onClick.AddListener(ShowZoneTab);
    }

    private void Start()
    {
        // No forzar panelRoot.SetActive(false) acá: como panelRoot puede ser
        // el propio GameObject de este script (arranca inactivo por diseño),
        // Start() recién corre la PRIMERA vez que algo lo activa -- que
        // normalmente es el propio OpenPanel() del primer tap del jugador.
        // Si acá lo forzábamos a cerrar, pisábamos ese primer OpenPanel()
        // legítimo, y recién el segundo tap quedaba abierto de verdad.
    }

    private void Update()
    {
        // Los badges se mantienen actualizados aunque el panel esté cerrado.
        badgeRefreshTimer += Time.unscaledDeltaTime;
        if (badgeRefreshTimer < badgeRefreshInterval) return;
        badgeRefreshTimer = 0f;

        int dailyPending = DailyMissionManager.Instance != null ? DailyMissionManager.Instance.PendingClaimCount() : 0;
        int zonePending = ZoneMissionManager.Instance != null ? ZoneMissionManager.Instance.PendingClaimCount() : 0;

        if (hudBadge != null) hudBadge.SetActive(dailyPending > 0 || zonePending > 0);
        if (tabDailyBadge != null) tabDailyBadge.SetActive(dailyPending > 0);
        if (tabZoneBadge != null) tabZoneBadge.SetActive(zonePending > 0);
    }

   

    // ================== PESTAÑAS ==================

    public void ShowDailyTab() => ShowTab(Tab.Daily);
    public void ShowZoneTab() => ShowTab(Tab.Zone);

    private void ShowTab(Tab tab)
    {
        currentTab = tab;

        if (tab == Tab.Daily)
        {
            dailyMissionsPanel?.OpenPanel();
            zoneMissionsPanel?.ClosePanel();
        }
        else
        {
            zoneMissionsPanel?.OpenPanel();
            dailyMissionsPanel?.ClosePanel();
        }

        if (dailyTabSelectedMark != null) dailyTabSelectedMark.SetActive(tab == Tab.Daily);
        if (zoneTabSelectedMark != null) zoneTabSelectedMark.SetActive(tab == Tab.Zone);

        // Convención habitual: la pestaña activa queda no-interactuable (ya estás ahí).
        if (dailyTabButton != null) dailyTabButton.interactable = tab != Tab.Daily;
        if (zoneTabButton != null) zoneTabButton.interactable = tab != Tab.Zone;
    }
}
