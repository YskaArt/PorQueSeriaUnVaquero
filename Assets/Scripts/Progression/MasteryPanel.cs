/*
 * MasteryPanel
 * ------------
 * UI del sistema de Maestría/Prestigio.
 *
 * WIRING EN EL EDITOR (todos los campos son opcionales salvo panelRoot):
 * - panelRoot: el GameObject del panel completo (se activa/desactiva).
 * - pointsText: "Mastery: 12 (+24%)".
 * - earnableText: puntos que se ganarían al prestigiar ahora.
 * - progressFill: Image (type Filled) con el progreso al próximo punto.
 * - prestigeButton: abre el sub-panel de confirmación (o prestigia directo si no hay).
 * - confirmPanel + confirmButton + cancelButton: confirmación de prestigio.
 * - closeButton: cierra el panel.
 *
 * Conectar OpenPanel() al botón de Maestría del HUD.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MasteryPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI earnableText;
    [SerializeField] private Image progressFill;

    [Header("Prestigio")]
    [SerializeField] private Button prestigeButton;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Refresco")]
    [SerializeField] private float refreshInterval = 0.5f;
    private float refreshTimer;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (prestigeButton != null) prestigeButton.onClick.AddListener(OnPrestigeClicked);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmPrestige);
        if (cancelButton != null) cancelButton.onClick.AddListener(HideConfirm);
    }

    private void Start()
    {
        if (MasteryManager.Instance != null)
            MasteryManager.Instance.OnMasteryChanged += RefreshUI;

        HideConfirm();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (MasteryManager.Instance != null)
            MasteryManager.Instance.OnMasteryChanged -= RefreshUI;
    }

    private void Update()
    {
        // El progreso avanza con el oro ganado, refrescar mientras esté abierto
        if (panelRoot == null || !panelRoot.activeSelf) return;

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshUI();
        }
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        HideConfirm();
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnPrestigeClicked()
    {
        var mastery = MasteryManager.Instance;
        if (mastery == null || !mastery.CanPrestige()) return;

        if (confirmPanel != null)
            confirmPanel.SetActive(true);
        else
            OnConfirmPrestige(); // sin panel de confirmación, prestigiar directo
    }

    private void OnConfirmPrestige()
    {
        HideConfirm();
        if (MasteryManager.Instance != null && MasteryManager.Instance.Prestige())
            ClosePanel();
    }

    private void HideConfirm()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void RefreshUI()
    {
        var mastery = MasteryManager.Instance;
        if (mastery == null) return;

        if (pointsText != null)
        {
            double bonusPct = mastery.MasteryPoints * mastery.BonusPerPoint * 100.0;
            pointsText.text = $"Mastery: {mastery.MasteryPoints} (+{bonusPct:0.#}% Gold)";
        }

        int earnable = mastery.PointsEarnedOnPrestige();
        if (earnableText != null)
        {
            earnableText.text = earnable > 0
                ? $"Prestige now: +{earnable} points"
                : $"Earn {GoldManager.FormatNumber(mastery.GoldRemainingForNextPoint())} more gold for a point";
        }

        if (progressFill != null)
            progressFill.fillAmount = mastery.ProgressToNextPoint();

        if (prestigeButton != null)
            prestigeButton.interactable = earnable > 0;
    }
}
