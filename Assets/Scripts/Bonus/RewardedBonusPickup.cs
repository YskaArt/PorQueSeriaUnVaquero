using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class RewardedBonusPickup : BonusPickup
{
    [Header("Rewarded Ad Settings")]
    [Tooltip("Si true, sólo se concederá el bonus si el usuario realmente obtuvo la recompensa del ad.")]
    [SerializeField] private bool requireRewardToGrant = false;

    [Header("Boosted parameters (ajustables)")]
    [Tooltip("Multiplicador aplicado en el rewarded (ej: 3.0 = x3)")]
    [SerializeField] private double rewardedGpsMultiplier = 3.0;
    [SerializeField] private double rewardedEnemyMultiplier = 3.0;
    [Tooltip("Duración por defecto para Frenzy cuando es rewarded (s).")]
    [SerializeField] private float rewardedFrenzyDuration = 45f;
    [Tooltip("Duración por defecto para GPS/Enemy rewarded (s).")]
    [SerializeField] private float rewardedDefaultDuration = 90f;

    // Intercept collision and run prompt -> ad -> grant flow
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        TryPickup(other?.gameObject);
    }

    protected override void TryPickup(GameObject other)
    {
        if (picked) return;
        if (other == null) return;
        if (!other.CompareTag("Player")) return;

        // marcar como recogido para evitar doble activación mientras el prompt esté abierto
        picked = true;

        // Determinar duración respetando overrideDuration/customDuration (campos son protected en base)
        float durationToUse = overrideDuration ? Mathf.Max(0.1f, customDuration) : -1f;

        // Capture position to play VFX/SFX after returning to pool
        Vector3 pickupPos = transform.position;

        // First return the object to pool so it disappears before pausing the game / showing UI
        ReturnToPool();

        // Show confirmation prompt in English asking to watch the ad
        if (RewardedAdPrompt.Instance != null)
        {
            string title = "Watch rewarded ad?";
            string message = "Watch an ad to claim a boosted bonus?";

            RewardedAdPrompt.Instance.Show(title, message,
      onAcceptCallback: () =>
      {
          Debug.Log("[RewardedBonusPickup] Player accepted prompt — starting rewarded coroutine.");
          if (AdsManager.Instance != null)
          {
              // Start coroutine on AdsManager singleton which is active
              AdsManager.Instance.StartCoroutine(AdsManager.Instance.ShowRewardedAdCoroutine((bool granted) =>
              {
                  if (requireRewardToGrant && !granted)
                  {
                      Debug.Log("[RewardedBonusPickup] User did not earn reward. No bonus granted.");
                      return;
                  }
                  GrantBoosted(durationToUse, pickupPos);
              }));
          }
          else
          {
              Debug.LogWarning("[RewardedBonusPickup] AdsManager missing -> granting boosted immediately as fallback.");
              GrantBoosted(durationToUse, pickupPos);
          }
      },
      onCancelCallback: () => { /* user cancelled - nothing to do */ });

        }
        else
        {
            // fallback: no prompt available, proceed with ad flow immediately
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.StartCoroutine(AdsManager.Instance.ShowRewardedAdCoroutine((bool granted) =>
                {
                    if (requireRewardToGrant && !granted)
                    {
                        Debug.Log("[RewardedBonusPickup] User did not earn reward. No bonus granted.");
                        return;
                    }

                    GrantBoosted(durationToUse, pickupPos);
                }));
            }
            else
            {
                GrantBoosted(durationToUse, pickupPos);
            }
        }
    }

    private void GrantBoosted(float durationToUse, Vector3 spawnPos)
    {
        if (BonusManager.Instance != null)
        {
            BonusManager.Instance.ActivateRandomBoostedBonus(
                duration: durationToUse,
                gpsMult: rewardedGpsMultiplier,
                enemyMult: rewardedEnemyMultiplier,
                frenzyBoostDuration: rewardedFrenzyDuration
            );
        }
        else
        {
            Debug.LogWarning("[RewardedBonusPickup] No hay BonusManager para conceder boosted bonus. Ejecutando fallback normal.");
            HandlePicked(durationToUse); // fallback: normal bonus
        }

        // Reproducir efectos (usa campos protected heredados) at captured position
        if (pickupSFX != null) AudioSource.PlayClipAtPoint(pickupSFX, spawnPos);
        if (pickupVFXPrefab != null)
        {
            var vfx = Instantiate(pickupVFXPrefab, spawnPos, Quaternion.identity);
            if (destroyVFXAfter > 0f) Destroy(vfx, destroyVFXAfter);
        }

        // Ensure the object is inactive in pool
        gameObject.SetActive(false);
    }
}
