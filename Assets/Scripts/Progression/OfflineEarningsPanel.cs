/*
 * OfflineEarningsPanel
 * ---------------------
 * Panel que se abre SOLO cuando OfflineEarningsManager detecta que hay
 * oro offline para ofrecer. Dos opciones: reclamar el 30% directo, o ver
 * un anuncio para llevarse el 60%.
 *
 * DECISIÓN DE DISEÑO (avisar si se quiere cambiar):
 * - El botón de cerrar (X) NO descarta la oferta: reclama el 30% base.
 *   Así el jugador nunca "pierde" el oro por cerrar el panel sin querer;
 *   simplemente no accede al bonus del anuncio. Si preferís que cerrar
 *   descarte todo, cambiar CloseButton para que llame a Dismiss() en vez
 *   de ClaimBaseAndClose().
 *
 * WIRING EN EL EDITOR:
 * - panelRoot: el diálogo completo (arranca inactivo).
 * - offlineTimeText: "Estuviste afuera 2h 15m".
 * - baseRewardText / adRewardText: montos formateados.
 * - claimBaseButton: reclama el 30%.
 * - claimAdButton: muestra el rewarded y reclama el 60% si se completa.
 * - closeButton (opcional): reclama el 30% y cierra (ver nota arriba).
 * - adLoadingIndicator (opcional): se activa mientras se espera el resultado del ad.
 *
 * Este panel se auto-registra: no hace falta abrirlo a mano, se abre solo
 * al arrancar la partida si corresponde.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OfflineEarningsPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI offlineTimeText;
    [SerializeField] private TextMeshProUGUI baseRewardText;
    [SerializeField] private TextMeshProUGUI adRewardText;

    [Header("Botones")]
    [SerializeField] private Button claimBaseButton;
    [SerializeField] private Button claimAdButton;
    [SerializeField] private Button closeButton;

    [Header("Feedback de anuncio (opcional)")]
    [SerializeField] private GameObject adLoadingIndicator;

    private void Awake()
    {
        if (claimBaseButton != null) claimBaseButton.onClick.AddListener(OnClaimBaseClicked);
        if (claimAdButton != null) claimAdButton.onClick.AddListener(OnClaimAdClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnClaimBaseClicked); // ver nota de diseño arriba
    }

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (OfflineEarningsManager.Instance != null)
        {
            OfflineEarningsManager.Instance.OnOfflineEarningsAvailable += HandleOfferAvailable;

            // Red de seguridad: si CheckForOfflineEarnings() ya corrió (ej. GoldManager.Start()
            // se ejecutó antes que este Start()) y ya había una oferta calculada, el evento
            // se disparó al vacío porque todavía no estábamos suscriptos. Nos ponemos al día
            // a mano en vez de depender 100% de haber escuchado el evento a tiempo.
            if (OfflineEarningsManager.Instance.HasPendingOffer)
            {
                HandleOfferAvailable(
                    OfflineEarningsManager.Instance.PendingBaseReward,
                    OfflineEarningsManager.Instance.PendingAdReward,
                    OfflineEarningsManager.Instance.PendingElapsedSeconds
                );
            }
        }
    }

    private void OnDestroy()
    {
        if (OfflineEarningsManager.Instance != null)
            OfflineEarningsManager.Instance.OnOfflineEarningsAvailable -= HandleOfferAvailable;
    }

    private void HandleOfferAvailable(double baseReward, double adReward, double elapsedSeconds)
    {
        if (offlineTimeText != null)
            offlineTimeText.text = $"Estuviste afuera {FormatElapsed(elapsedSeconds)}";

        if (baseRewardText != null)
            baseRewardText.text = GoldManager.FormatNumber(baseReward);

        if (adRewardText != null)
            adRewardText.text = GoldManager.FormatNumber(adReward);

        SetAdBusy(false);

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void OnClaimBaseClicked()
    {
        OfflineEarningsManager.Instance?.ClaimBase();
        ClosePanel();
    }

    private void OnClaimAdClicked()
    {
        if (OfflineEarningsManager.Instance == null || !OfflineEarningsManager.Instance.HasPendingOffer)
        {
            ClosePanel();
            return;
        }

        SetAdBusy(true);
        OfflineEarningsManager.Instance.ClaimWithAd(granted =>
        {
            SetAdBusy(false);
            // Si el ad no se completó (granted=false), la oferta ya se limpió igual
            // (ver ClaimWithAd): evitamos dejar al jugador en un estado ambiguo.
            ClosePanel();
        });
    }

    private void SetAdBusy(bool busy)
    {
        if (adLoadingIndicator != null) adLoadingIndicator.SetActive(busy);
        if (claimAdButton != null) claimAdButton.interactable = !busy;
        if (claimBaseButton != null) claimBaseButton.interactable = !busy;
    }

    private void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private static string FormatElapsed(double seconds)
    {
        var span = System.TimeSpan.FromSeconds(seconds);
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes}m";
        return $"{span.Minutes}m {span.Seconds}s";
    }
}