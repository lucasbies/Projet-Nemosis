using System.Collections.Generic;
using UnityEngine;

public class ScheduleManager : MonoBehaviour
{
    public static ScheduleManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Nombre maximum de jours affichables dans l'emploi du temps")]
    public int maxScheduleDays = 28;

    [Header("Emploi du temps")]
    public List<ScheduleEntry> schedule = new List<ScheduleEntry>();

    // Dictionnaire pour accès rapide : key = "jour_moment"
    private Dictionary<string, ScheduleEntry> scheduleDict = new Dictionary<string, ScheduleEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RebuildScheduleDictionary();

        // NE PAS APPELER DE MÉTHODE QUI REMPLIT AUTOMATIQUEMENT ICI
        // Le schedule doit rester vide au démarrage

        Debug.Log($"[ScheduleManager] Initialisé avec {schedule.Count} entrées dans le schedule");
    }


    public void AddScheduleEntry(int day, DayTime time, string activityName, ScheduleActivityType type, string description = "", Sprite icon = null)
    {
        if (day < 1 || day > maxScheduleDays)
        {
            Debug.LogWarning($"[ScheduleManager] Jour {day} hors limites (1-{maxScheduleDays})");
            return;
        }

        string key = GetKey(day, time);

        // Si une entrée existe déjà, la remplacer
        if (scheduleDict.ContainsKey(key))
        {
            RemoveScheduleEntry(day, time);
        }

        ScheduleEntry entry = new ScheduleEntry(day, time, activityName, type)
        {
            description = description,
            icon = icon
        };

        schedule.Add(entry);
        scheduleDict[key] = entry;

        Debug.Log($"[ScheduleManager] Activité '{activityName}' ajoutée : Jour {day} ({time})");
    }


    public void RemoveScheduleEntry(int day, DayTime time)
    {
        string key = GetKey(day, time);

        if (scheduleDict.TryGetValue(key, out ScheduleEntry entry))
        {
            schedule.Remove(entry);
            scheduleDict.Remove(key);
            Debug.Log($"[ScheduleManager] Activité supprimée : Jour {day} ({time})");
        }
    }


    public ScheduleEntry GetScheduleEntry(int day, DayTime time)
    {
        string key = GetKey(day, time);
        scheduleDict.TryGetValue(key, out ScheduleEntry entry);
        return entry;
    }


    /// Vérifie si une activité est planifiée pour ce moment
    public bool HasScheduledActivity(int day, DayTime time)
    {
        return scheduleDict.ContainsKey(GetKey(day, time));
    }

    /// Récupère toutes les activités d'un jour donné
    public List<ScheduleEntry> GetDaySchedule(int day)
    {
        List<ScheduleEntry> dayEntries = new List<ScheduleEntry>();

        foreach (var entry in schedule)
        {
            if (entry.day == day)
            {
                dayEntries.Add(entry);
            }
        }

        return dayEntries;
    }

    /// Efface tout l'emploi du temps
    public void ClearSchedule()
    {
        schedule.Clear();
        scheduleDict.Clear();
        Debug.Log("[ScheduleManager] Emploi du temps effacé");
    }

    /// Reconstruit le dictionnaire à partir de la liste (utile après désérialisation)
    private void RebuildScheduleDictionary()
    {
        scheduleDict.Clear();
        foreach (var entry in schedule)
        {
            string key = GetKey(entry.day, entry.timeOfDay);
            scheduleDict[key] = entry;
        }
    }

    /// Génère une clé unique pour jour + moment
    private string GetKey(int day, DayTime time)
    {
        return $"{day}_{(int)time}";
    }

    /// Applique l'effet d'une entrée planifiée (appelé depuis GameManager.EndHalfDay)
    public void ExecuteScheduledActivity(int day, DayTime time)
    {
        ScheduleEntry entry = GetScheduleEntry(day, time);

        if (entry == null)
            return;

        Debug.Log($"[ScheduleManager] Exécution de l'activité : {entry.activityName}");

        // Appliquer l'effet si défini
        if (entry.effectToApply != null)
        {
            Effect effect = entry.effectToApply.CreateInstance();
            if (effect != null)
            {
                // Ajouter l'effet au PassiveManager pour qu'il soit géré correctement
                if (PassiveManager.Instance != null)
                {
                    PassiveManager.Instance.AddEffect(effect);
                }

                // Vérifier les conditions et activer l'effet
                effect.CheckConditions();

                Debug.Log($"[ScheduleManager] Effet '{entry.effectToApply.effectName}' appliqué");
            }
        }
    }
}