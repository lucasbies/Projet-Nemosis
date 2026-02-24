using UnityEngine;

[CreateAssetMenu(menuName = "Effects/CreateBatiments")]
public class CreateBatimentsEffectSO : EffectSO
{
    public BuildingData buildingData;

    public override Effect CreateInstance()
    {
        return new Effect_CreateBatiments(this);
    }
}

public class Effect_CreateBatiments : Effect
{
    private CreateBatimentsEffectSO soData;

    public Effect_CreateBatiments(CreateBatimentsEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }
    public override void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        VillageManager.Instance.AddBuilding(soData.buildingData);
        DestroySelf();
    }
    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        return;
    }

}
