using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RewardedAdPrompt
/// ----------------
/// - Singleton que muestra un panel modal con 2 botones: "View ad" y "Cancel".
/// - Al abrir pausa el juego (Time.timeScale = 0).
/// - Llama onAccept si el jugador acepta, onCancel si cancela.
/// - Siempre reestablece Time.timeScale a 1 antes de invocar el callback.
/// </summary>
public class RewardedAdPrompt : MonoBehaviour
{
    public static RewardedAdPrompt Instance { get; private set; }

    [Header("UI refs")]
    [SerializeField] private GameObject panel;                 // panel raíz que contiene todo (se activa/desactiva)
    [SerializeField] private TextMeshProUGUI titleText;        // título opcional (ej. "Watch ad?")
    [SerializeField] private TextMeshProUGUI bodyText;         // mensaje con info del bonus
    [SerializeField] private Button acceptButton;              // "View ad"
    [SerializeField] private Button cancelButton;              // "Cancel"

    private Action onAccept;
    private Action onCancel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (panel != null) panel.SetActive(false);

        // seguridad: remover listeners previos
        if (acceptButton != null) { acceptButton.onClick.RemoveAllListeners(); acceptButton.onClick.AddListener(OnAcceptClicked); }
        if (cancelButton != null) { cancelButton.onClick.RemoveAllListeners(); cancelButton.onClick.AddListener(OnCancelClicked); }
    }

    private void OnDestroy()
    {
        if (acceptButton != null) acceptButton.onClick.RemoveAllListeners();
        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Muestra el prompt. Pausa el juego (Time.timeScale = 0).
    /// onAccept/onCancel son invocados cuando el jugador presiona un botón.
    /// message puede contener la descripción del bonus (ej: "Watch ad to get x3 GPS for 90s?")
    /// Defaults are English.
    /// </summary>
    public void Show(string title, string message, Action onAcceptCallback, Action onCancelCallback)
    {
        if (panel == null)
        {
            Debug.LogWarning("[RewardedAdPrompt] No panel asignado. Ejecutando callback de accept directamente.");
            onAcceptCallback?.Invoke();
            return;
        }

        // set texts (default English)
        if (titleText != null) titleText.text = title ?? "Watch rewarded AD?";
        if (bodyText != null) bodyText.text = message ?? "Would you like to watch an AD to claim a boosted bonus?";

        onAccept = onAcceptCallback;
        onCancel = onCancelCallback;

        // Pause game AFTER caller already returned the pickup object to pool (caller must ensure it)
        Time.timeScale = 0f;

        panel.SetActive(true);
    }

    private void Hide()
    {
        if (panel != null) panel.SetActive(false);
        onAccept = null;
        onCancel = null;
    }

    private void OnAcceptClicked()
    {
        // Capture callback references before hiding/clearing
        var accept = onAccept;

        // Restore time BEFORE invoking ad (ads SDK often expects real-time)
        Time.timeScale = 1f;

        // Hide and clear stored callbacks
        Hide();

        try { accept?.Invoke(); }
        catch (Exception ex) { Debug.LogWarning("[RewardedAdPrompt] Exception in onAccept: " + ex); }
    }

    private void OnCancelClicked()
    {
        var cancel = onCancel;

        Time.timeScale = 1f;

        Hide();

        try { cancel?.Invoke(); }
        catch (Exception ex) { Debug.LogWarning("[RewardedAdPrompt] Exception in onCancel: " + ex); }
    }
}
