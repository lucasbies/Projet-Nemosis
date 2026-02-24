using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


[CreateAssetMenu(menuName = "Effects/ModifyPercentage")]
public class ModifyPercentageEffectSO : EffectSO
{
    public StatType statType;
    public float multiplier; // exemple : 0.1f pour +10% (ou 0.1 = +10% additionnel)
    // conditions[] hérité de EffectSO
    // Peut être INSTANTANÉ (bonus permanent) ou PERSISTANT (conditionnel) selon isInstant

    public override Effect CreateInstance()
    {
        return new Effect_ModifyPourcentageGain(this);
    }
}


public class Effect_ModifyPourcentageGain : Effect
{
    private ModifyPercentageEffectSO donnee; 
    public Effect_ModifyPourcentageGain(ModifyPercentageEffectSO soData) : base(soData)
    {
        donnee = soData;
        // L'ajout et l'évaluation se font à l'appelant (UI/Dialogue/Card).
        Debug.Log($"Effet de modification de pourcentage créé pour {donnee.statType} avec un multiplicateur actuelle {GameManager.Instance.Multiplicateur[donnee.statType]} multi actif : {IsActive}");
    }
    
    public override void Activate()
    {   
        if (IsActive) return;
        IsActive = true;
        Debug.Log($"[Effect] ModifyPercentage activated for {donnee.statType}. Multiplier before: {GameManager.Instance.Multiplicateur[donnee.statType]}");
        GameManager.Instance.Multiplicateur[donnee.statType] += donnee.multiplier;
        Debug.Log($"[Effect] New multiplier for {donnee.statType}: {GameManager.Instance.Multiplicateur[donnee.statType]}");
        if (IsInstant)
        {
            Debug.Log($"[Effect] ModifyPercentage is instant, destroying self.");
            DestroySelf();
        }
    }



    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        GameManager.Instance.Multiplicateur[donnee.statType] -= donnee.multiplier;
        Debug.Log($"[Effect] ModifyPercentage deactivated for {donnee.statType}. Multiplier restored to: {GameManager.Instance.Multiplicateur[donnee.statType]}");
    }

}
