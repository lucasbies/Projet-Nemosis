using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Batiments/UpgradeBatiment")]
public class UpgradeBatimentEffectSO : EffectSO
{
    public BuildingData UpgradedBuilding;
    public ResourceEntry[] UpgradeCost;

    public override Effect CreateInstance()
    {
        return new Effect_UpgradeBatiment(this);
    }
}

public class Effect_UpgradeBatiment : Effect
{
    private UpgradeBatimentEffectSO soData;

    public Effect_UpgradeBatiment(UpgradeBatimentEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }
    public override void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        
        // Vérifie les ressources et affiche les stats insuffisantes en rouge
        bool hasEnoughResources = true;
        foreach (var entry in soData.UpgradeCost)
        {
            StatType stat = entry.statType;
            float amount = entry.amount;
            if (GameManager.Instance.Valeurs[stat] < amount)
            {
                hasEnoughResources = false;
                // Fait secouer et rendre rouge le stat panel
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.InvalidateStatUI(stat);
                }
            }
        }
        
        // Si pas assez de ressources, annule l'upgrade
        if (!hasEnoughResources)
        {
            DestroySelf();
            return;
        }
        foreach (var entry in soData.UpgradeCost)
        {
            StatType stat = entry.statType;
            float amount = entry.amount;
            GameManager.Instance.changeStat(stat, -amount);
        }
        
        if (VillageManager.Instance == null) return;
        if (VillageManager.Instance.buildingClicked == null) return;
        BuildingData buildingToUpgrade = VillageManager.Instance.buildingClicked;
        VillageManager.Instance.buildingClicked = null;
        UIManager.Instance.HideInteractionMenu();
        VillageManager.Instance.RemoveBuilding(buildingToUpgrade);
        VillageManager.Instance.AddBuilding(soData.UpgradedBuilding);
        VillageManager.Instance.AfficheBuildings2D();
        DestroySelf();
    }
    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        return;
    }

}
