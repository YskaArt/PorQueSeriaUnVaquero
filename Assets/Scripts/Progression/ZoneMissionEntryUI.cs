/*
 * ZoneMissionEntryUI
 * -------------------
 * Una fila del panel de misiones de zona. Idéntico patrón a MissionEntryUI,
 * pero enlazado a ZoneMissionManager.ActiveZoneMission.
 *
 * WIRING EN EL EDITOR: igual que MissionEntryUI.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneMissionEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedIndicator;

    private ZoneMissionManager.ActiveZoneMission mission;

    private void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    public void Bind(ZoneMissionManager.ActiveZoneMission newMission)
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
        ZoneMissionManager.Instance?.ClaimMission(mission);
        Refresh();
    }
}
