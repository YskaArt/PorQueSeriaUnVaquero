/*
 * MissionEntryUI
 * --------------
 * Una fila del panel de misiones diarias. DailyMissionsPanel le pasa la misión
 * a mostrar con Bind() y este componente actualiza sus textos y botón.
 *
 * WIRING EN EL EDITOR:
 * - descriptionText: descripción de la misión.
 * - progressText: "35 / 150".
 * - progressFill: Image (type Filled), opcional.
 * - rewardText: recompensa ("1.2K Gold + 1 Mastery"), opcional.
 * - claimButton: botón de reclamar (se habilita al completar).
 * - claimedIndicator: GameObject que se muestra cuando ya se reclamó (tilde), opcional.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedIndicator;

    private DailyMissionManager.ActiveMission mission;

    private void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    public void Bind(DailyMissionManager.ActiveMission newMission)
    {
        mission = newMission;
        Refresh();
    }

    public void Refresh()
    {
        bool hasMission = mission != null && mission.data != null;
        gameObject.SetActive(hasMission);
        if (!hasMission) return;

        if (descriptionText != null)
            descriptionText.text = mission.data.description;

        if (progressText != null)
        {
            string current = GoldManager.FormatNumber(System.Math.Min(mission.progress, mission.target));
            string target = GoldManager.FormatNumber(mission.target);
            progressText.text = $"{current} / {target}";
        }

        if (progressFill != null)
            progressFill.fillAmount = mission.Progress01;

        if (rewardText != null)
            rewardText.text = mission.data.BuildRewardLabel();

        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(!mission.claimed);
            claimButton.interactable = mission.IsCompleted && !mission.claimed;
        }

        if (claimedIndicator != null)
            claimedIndicator.SetActive(mission.claimed);
    }

    private void OnClaimClicked()
    {
        if (mission == null) return;
        DailyMissionManager.Instance?.ClaimMission(mission);
        Refresh();
    }
}
