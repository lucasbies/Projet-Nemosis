using UnityEngine;

[CreateAssetMenu(menuName = "Effects/God/ChangeGodRelation")]
public class ChangeGodRelationEffectSO : EffectSO
{
    public int relationChange;
    public GodDataSO godData;

    public override Effect CreateInstance()
    {
        return new Effect_ChangeGodRelation(this);
    }
}

public class Effect_ChangeGodRelation : Effect
{
    private ChangeGodRelationEffectSO soData;

    public Effect_ChangeGodRelation(ChangeGodRelationEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }
    public override void Activate()
    {
        Debug.Log("[ChangeGodRelationEffect] Activating effect...");
        if (IsActive) return;
        IsActive = true;
        
        GodDataSO targetGod = soData.godData ?? ChooseRelationUI.Instance.selectedGod;
        if (targetGod == null)
        {
            Debug.LogError("[ChangeGodRelationEffect] No target god found!");
            DestroySelf();
            return;
        }
        
        targetGod.relation += soData.relationChange;
        Debug.Log($"[ChangeGodRelationEffect] Changed {targetGod.displayName} relation by {soData.relationChange}. New relation: {targetGod.relation}");
        
        // Marque le SO comme modifié pour que Unity le sauvegarde
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetGod);
        #endif
        
        DestroySelf();
    }
    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        return;
    }

}
