using UnityEngine;

[CreateAssetMenu(menuName = "Effects/CreateEvents")]
public class CreateEventsEffectSO : EffectSO
{
    public BaseGameEvent EventData;
    public int eventday = 0;
    public DayTime time;

    public override Effect CreateInstance()
    {
        return new Effect_CreateEvents(this);
    }
}

public class Effect_CreateEvents : Effect
{
    private CreateEventsEffectSO soData;

    public Effect_CreateEvents(CreateEventsEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }
    public override void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        EventScheduler.Instance.AddEvent(soData.EventData, soData.eventday, soData.time);
        DestroySelf();
    }
    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        return;
    }

}
