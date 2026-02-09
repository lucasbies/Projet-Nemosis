using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Card/VillageCard")]
public class VillageCard : Card
{
    public EffectSO effectSO;

    public void PlayCard()
    {
        var eff = effectSO.CreateInstance();
        if (eff != null)
        {
            PassiveManager.Instance.AddEffect(eff);
            eff.CheckConditions();
            Debug.Log($"[Effect] Applied {effectSO.effectName} via VillageCard.PlayCard().");
        }
    }

}
