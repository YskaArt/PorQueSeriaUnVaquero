/*
 * MasteryProgressHUD
 * ------------------
 * Barra de progreso de maestría para la parte SUPERIOR del HUD
 * (el GDD la pide junto al contador de monedas).
 *
 * WIRING: asignar una Image (type Filled) y opcionalmente un texto.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MasteryProgressHUD : MonoBehaviour
{
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI label; // opcional: "MASTERY +1"
    [SerializeField] private float refreshInterval = 0.5f;

    private float timer;

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < refreshInterval) return;
        timer = 0f;

        var mastery = MasteryManager.Instance;
        if (mastery == null) return;

        if (progressFill != null)
            progressFill.fillAmount = mastery.ProgressToNextPoint();

        if (label != null)
        {
            int earnable = mastery.PointsEarnedOnPrestige();
            label.text = earnable > 0 ? $"MASTERY +{earnable}" : "MASTERY";
        }
    }
}
