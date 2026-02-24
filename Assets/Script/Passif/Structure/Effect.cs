using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
public abstract class Effect
{

    public bool IsActive = false;
    protected EffectSO data;
    private bool OnchechConditions = false;
    protected List<Condition> conditions = new List<Condition>();

    // Référence au bâtiment source (si l'effet vient d'un bâtiment)
    public BuildingData SourceBuilding { get; set; }

    public bool IsInstant => data.isInstant;

    public Effect(EffectSO data)
    {
        this.data = data;

        // Crée les conditions si le SO en contient
        foreach (var conditionSO in data.conditions)
        {
            var condition = conditionSO.CreateInstance(this);
            conditions.Add(condition);
        }
        
    }

    public void OnConditionChanged()
    {
        if (!OnchechConditions) CheckConditions();
    }
    public void CheckConditions()
    {
        OnchechConditions = true;
        foreach (var c in conditions)
        {
            c.Evaluate();
            Debug.Log($"[CheckCondition] Vérification de la condition : {c.GetType().Name}, IsTrue = {c.IsTrue}");
        }
        bool allTrue = conditions.All(c => c.IsTrue);


        if (conditions.Count == 0)
        {
            Activate();
            if (IsInstant) DestroySelf(); // va surement disparaitre
            return;
        }
        Debug.Log($"[Effect] allTrue = {allTrue}, IsActive = {IsActive}");
        if (allTrue && !IsActive)
        {
            Debug.Log("Effet activé via CheckConditions.\n");
            Activate();
        }
        else if (!allTrue && IsActive)
        {
            Deactivate();
        }
        OnchechConditions = false;
    }
    public abstract void Activate();

    public abstract void Deactivate();

    public virtual void DestroySelf()
    {
        if (IsActive && !IsInstant) Deactivate();

        Debug.Log($"Effet {this} détruit.");
        foreach (var c in conditions)
            c.Unsubscribe();

        conditions.Clear();
        PassiveManager.Instance.RemoveEffect(this);
    }

}


public abstract class EffectSO : ScriptableObject
{
    public string effectName;
    [TextArea]public string description;
    public bool isInstant;
    public ConditionSO[] conditions;

    public virtual Effect CreateInstance()
    {
        Debug.LogWarning("EffectSO.CreateInstance() la base est appeler. Use a subclass SO instead.");
        return null;
    }

}