using System;
using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// BonusUI simple: solo panel + nombre del bonus + contador.
/// </summary>
public class BonusUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI labelText; // "GPS BONUS" / "ENEMY BONUS" / "FRENZY"
    [SerializeField] private TextMeshProUGUI timerText; 

    private Coroutine blinkCoroutine;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (BonusManager.Instance != null)
        {
            BonusManager.Instance.OnBonusStarted += HandleStarted;
            BonusManager.Instance.OnBonusTick += HandleTick;
            BonusManager.Instance.OnBonusEnded += HandleEnded;
        }
    }

    private void OnDisable()
    {
        if (BonusManager.Instance != null)
        {
            BonusManager.Instance.OnBonusStarted -= HandleStarted;
            BonusManager.Instance.OnBonusTick -= HandleTick;
            BonusManager.Instance.OnBonusEnded -= HandleEnded;
        }

        StopBlinking();
    }

    private void HandleStarted(BonusManager.BonusType type, float duration)
    {
        if (panel != null) panel.SetActive(true);

        if (labelText != null)
        {
            if (type == BonusManager.BonusType.GPSDouble)
            {
                double mult = BonusManager.Instance != null ? BonusManager.Instance.GetGpsMultiplier() : 2.0;
                labelText.text = $"GPS BONUS {FormatMultiplier(mult)}";
            }
            else if (type == BonusManager.BonusType.EnemyDouble)
            {
                double mult = BonusManager.Instance != null ? BonusManager.Instance.GetEnemyRewardMultiplier() : 2.0;
                labelText.text = $"ENEMY BONUS {FormatMultiplier(mult)}";
            }
            else // Frenzy
            {
                labelText.text = "A HORDE IS APPROACHING";
            }

            // Ensure label is visible initially
            labelText.enabled = true;

            // Start blinking
            StartBlinking();
        }

        // For Frenzy we hide the timer; for others show it
        if (timerText != null)
        {
            if (type == BonusManager.BonusType.Frenzy)
                timerText.gameObject.SetActive(false);
            else
            {
                timerText.gameObject.SetActive(true);
                UpdateTimerText(duration);
            }
        }
    }

    private void HandleTick(float remaining)
    {
        // Only update timer if it's visible
        if (timerText != null && timerText.gameObject.activeSelf)
            UpdateTimerText(remaining);
    }

    private void HandleEnded()
    {
        if (panel != null) panel.SetActive(false);
        StopBlinking();
    }

    private void UpdateTimerText(float remaining)
    {
        if (timerText == null) return;
        int sec = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        timerText.text = $"{sec}s";
    }

    private void StartBlinking()
    {
        StopBlinking();
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (labelText != null) labelText.enabled = true;
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            if (labelText != null)
                labelText.enabled = !labelText.enabled;
            yield return new WaitForSeconds(0.4f);
        }
    }

    private string FormatMultiplier(double mult)
    {
        if (double.IsNaN(mult) || double.IsInfinity(mult)) return "x?";
        // Show as integer if close, otherwise one decimal
        double rounded = Math.Round(mult);
        if (Math.Abs(mult - rounded) < 0.01)
            return $"x{(int)rounded}";
        else
            return $"x{mult:0.0}";
    }
}
