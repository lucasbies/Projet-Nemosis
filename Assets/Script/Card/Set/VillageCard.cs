using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Card/VillageCard")]
public class VillageCard : Card
{
    public EffectSO effectSO;

    public void PlayCard()
    {
        if (effectSO == null)
        {
            Debug.LogWarning($"[VillageCard] '{cardName}' n'a pas d'effet assigné.");
            return;
        }

        Effect effect = effectSO.CreateInstance();
        if (effect != null)
        {
            effect.Activate();

            // 🆕 Animation de la stat après activation
            if (CheckIfIncreasesMaxStat() && UIManager.Instance != null)
            {
                StatType affectedStat = GetAffectedStatType();
                UIManager.Instance.PlayMaxStatIncreaseAnimation(affectedStat);
            }
        }
    }

    /// <summary>
    /// 🆕 Appelé AVANT PlayCard() pour déclencher l'animation de la CARTE
    /// (à appeler depuis CardUI.cs)
    /// </summary>
    public bool NeedsMaxStatAnimation()
    {
        return CheckIfIncreasesMaxStat();
    }

    private bool CheckIfIncreasesMaxStat()
    {
        if (effectSO == null) return false;

        string typeName = effectSO.GetType().Name;
        return typeName.Contains("ChangeStatMax") ||
               typeName.Contains("MaxStat") ||
               effectSO.effectName.ToLower().Contains("maximum");
    }

    private StatType GetAffectedStatType()
    {
        if (effectSO == null) return StatType.Foi;

        string desc = effectSO.description.ToLower();

        if (desc.Contains("foi")) return StatType.Foi;
        if (desc.Contains("or") || desc.Contains("argent")) return StatType.Or;
        if (desc.Contains("nourriture") || desc.Contains("food")) return StatType.Food;
        if (desc.Contains("humain") || desc.Contains("population")) return StatType.Human;
        if (desc.Contains("nemosis")) return StatType.Nemosis;

        return StatType.Foi;
    }
}