using UnityEngine;
using System;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Effects/ChangeMaxRessource")]
public class ChangeMaxRessourceEffectSO : EffectSO
{
    // Remplacer le Dictionary par une liste sérialisable visible dans l'inspector
    public List<ResourceEntry> MaxRessourceToChange = new List<ResourceEntry>();

    public override Effect CreateInstance()
    {
        return new Effect_ChangeMaxRessource(this);
    }
}
// Effet TOUJOURS instantané : ajoute des ressources puis se détruit automatiquement
public class Effect_ChangeMaxRessource : Effect
{
    private ChangeMaxRessourceEffectSO soData;

    public Effect_ChangeMaxRessource(ChangeMaxRessourceEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }
    public override void Activate()
    {
        if (IsActive) return;
        IsActive = true;

        // Parcourt la liste d'entrées sérialisées
        foreach (var entry in soData.MaxRessourceToChange)
        {
            StatType stat = entry.statType;
            float amount = entry.amount;
            GameManager.Instance.changeStatMax(stat, amount);
        }
        // Toujours instantané : se détruit après application
        DestroySelf();
    }
    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        return;
    }

}


