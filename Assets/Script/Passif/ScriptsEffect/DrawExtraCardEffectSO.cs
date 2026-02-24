using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Effects/DrawwExtraCard")]
public class DrawExtraCardEffectSO : EffectSO
{
    public int extraCards;

    public override Effect CreateInstance()
    {
        return new Effect_DrawExtraCard(this);
    }
}
// Effet PERSISTANT : reste actif tant que les conditions sont remplies
public class Effect_DrawExtraCard : Effect
{
    private DrawExtraCardEffectSO soData;

    public Effect_DrawExtraCard(DrawExtraCardEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }
    public override void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        GameManager.Instance.cardsToDraw += soData.extraCards;
        Debug.Log($"[Effect] DrawExtraCard activated: +{soData.extraCards} cards (total: {GameManager.Instance.cardsToDraw})");
    }
    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        GameManager.Instance.cardsToDraw -= soData.extraCards;
        Debug.Log($"[Effect] DrawExtraCard deactivated: -{soData.extraCards} cards (total: {GameManager.Instance.cardsToDraw})");
    }

}


