using UnityEngine;

public class UpgradeUIManager : MonoBehaviour
{
    [SerializeField] private Upgrade[] upgrades; // Referencias a los objetos Upgrade en el Canvas
    [SerializeField] private GameObject[] upgradeVisuals; // Visuales completas de cada mejora (paneles/contenedores)
    private bool[] hasBeenRevealed; // Flags para saber si la mejora ya fue revelada

    private void Awake()
    {
        // Inicializar el array de flags al tamaño de upgrades
        hasBeenRevealed = new bool[upgrades != null ? upgrades.Length : 0];
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (GoldManager.Instance == null || upgrades == null || upgradeVisuals == null) return;
        if (hasBeenRevealed == null || hasBeenRevealed.Length != upgrades.Length)
            hasBeenRevealed = new bool[upgrades.Length];

        double currentGold = GoldManager.Instance.CurrentGold;

        for (int i = 0; i < upgrades.Length; i++)
        {
            Upgrade upgrade = upgrades[i];
            GameObject visual = (i < upgradeVisuals.Length) ? upgradeVisuals[i] : null;
            if (upgrade == null || visual == null)
                continue;

            UpgradeData data = upgrade.GetUpgradeData();
            if (data == null)
            {
                visual.SetActive(false);
                continue;
            }

            bool canShow = false;

            // Si ya fue revelada, siempre visible
            if (hasBeenRevealed[i])
            {
                canShow = true;
            }
            // Si ya tiene al menos 1 nivel, siempre visible y marcar como revelada
            else if (data.currentLevel > 0)
            {
                canShow = true;
                hasBeenRevealed[i] = true;
            }
            else
            {
                // 1️⃣ Si no es la primera mejora, verificar que la anterior tenga nivel >= 3
                if (i > 0)
                {
                    Upgrade previousUpgrade = upgrades[i - 1];
                    if (previousUpgrade == null || previousUpgrade.GetUpgradeData().currentLevel < 3)
                        canShow = false;
                    else
                        canShow = true;
                }
                else
                {
                    canShow = true; // La primera mejora siempre puede mostrarse si cumple oro
                }

                // 2️⃣ Verificar si tiene al menos la mitad del oro necesario
                double nextCost = data.GetCost();
                if (currentGold < nextCost / 2.0)
                    canShow = false;

                // Si se va a mostrar por primera vez, marcar como revelada
                if (canShow)
                    hasBeenRevealed[i] = true;
            }

            // Mostrar/ocultar el panel visual completo
            visual.SetActive(canShow);

            // Refrescar datos si está visible
            if (canShow)
                upgrade.ForceUpdateUI();
        }
    }
}
