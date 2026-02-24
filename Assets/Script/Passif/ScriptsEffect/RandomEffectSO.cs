using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Effects/Batiments/RandomEffect")]
public class RandomEffectSO : EffectSO
{
    [Tooltip("Liste des effets parmi lesquels un sera choisi au hasard")]
    public List<EffectSO> possibleEffects = new List<EffectSO>();

    public override Effect CreateInstance()
    {
        return new Effect_Random(this);
    }
}

public class Effect_Random : Effect
{
    private RandomEffectSO soData;
    private EffectSO currentEffectSO; // Garde une référence au SO actuel pour le retirer des interactions

    public Effect_Random(RandomEffectSO soData) : base(soData)
    {
        this.soData = soData;
    }

    public override void Activate()
    {
        if (IsActive) return;
        IsActive = true;

        SourceBuilding.interactionEffects = null; // Initialise la liste des effets d'interaction du bâtiment source
        // S'abonne à l'événement de fin de journée (nuit)
        GameEvents.OnDayEnd += OnNightArrived;

        // Ajoute un premier effet aléatoire aux interactions
        SetRandomInteractionEffect();
    }

    private void OnNightArrived()
    {
        // Change d'effet à chaque nuit
        SetRandomInteractionEffect();
    }

    private void SetRandomInteractionEffect()
    {
        Debug.Log("[RandomEffect] Choix d'un nouvel effet aléatoire pour les interactions...");
        if (soData.possibleEffects == null || soData.possibleEffects.Count == 0)
        {
            Debug.LogWarning("RandomEffectSO: Aucun effet dans la liste possibleEffects!");
            return;
        }

        if (SourceBuilding == null)
        {
            Debug.LogWarning("RandomEffectSO: Pas de bâtiment source lié!");
            return;
        }

        // Initialise la liste si nécessaire
        if (SourceBuilding.interactionEffects == null)
        {
            SourceBuilding.interactionEffects = new System.Collections.Generic.List<EffectSO>();
        }

        // Retire l'ancien effet des interactionEffects s'il existe
        if (currentEffectSO != null)
        {
            SourceBuilding.interactionEffects.Remove(currentEffectSO);
            Debug.Log($"[RandomEffect] Ancien effet retiré: {currentEffectSO.effectName}");
            currentEffectSO = null;
        }

        // Choisit un nouvel effet au hasard
        int randomIndex = Random.Range(0, soData.possibleEffects.Count);
        EffectSO randomEffectSO = soData.possibleEffects[randomIndex];

        if (randomEffectSO == null)
        {
            Debug.LogWarning("RandomEffectSO: L'effet sélectionné est null!");
            return;
        }

        // Sauvegarde la référence et ajoute aux interactions (sans activer)
        currentEffectSO = randomEffectSO;
        SourceBuilding.interactionEffects.Add(randomEffectSO);
        Debug.Log($"[RandomEffect] Nouvel effet ajouté aux interactionEffects: {randomEffectSO.effectName}");
    }

    public override void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;

        // Se désabonne de l'événement
        GameEvents.OnDayEnd -= OnNightArrived;

        // Retire l'effet des interactionEffects
        if (SourceBuilding != null && currentEffectSO != null && SourceBuilding.interactionEffects != null)
        {
            SourceBuilding.interactionEffects.Remove(currentEffectSO);
            currentEffectSO = null;
        }
    }

    public override void DestroySelf()
    {
        // Se désabonne avant destruction
        GameEvents.OnDayEnd -= OnNightArrived;

        // Retire l'effet des interactionEffects
        if (SourceBuilding != null && currentEffectSO != null && SourceBuilding.interactionEffects != null)
        {
            SourceBuilding.interactionEffects.Remove(currentEffectSO);
            currentEffectSO = null;
        }

        base.DestroySelf();
    }
}
