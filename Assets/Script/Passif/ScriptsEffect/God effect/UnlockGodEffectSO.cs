using UnityEngine;

[CreateAssetMenu(menuName = "Effects/God/UnlockGods")]
public class unlockedGodsEffectSO : EffectSO
{
    public GodDataSO godData;

    public override Effect CreateInstance()
    {
        return new Effect_UnlockGods(this);
    }
}

public class Effect_UnlockGods : Effect
{
    private unlockedGodsEffectSO soData;

    public Effect_UnlockGods(unlockedGodsEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }
    public override void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        soData.godData.unlocked = true;
        DestroySelf();
    }
    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        return;
    }

}
