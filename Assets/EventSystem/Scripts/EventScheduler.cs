using System.Collections.Generic;
using UnityEngine;

public class EventScheduler : MonoBehaviour
{
    public static EventScheduler Instance { get; private set; }

    [Header("Calendrier d'événements (28 jours)")]
    [Tooltip("Associe un jour (1-28) à un événement. Laissez vide les jours sans événement.")]
    public List<EventDay> scheduledEvents = new List<EventDay>();

    // Événement actif
    private BaseGameEvent currentEvent;
    private int eventEndDay = -1;  // -1 = pas d'événement actif
    private DayTime eventEndTime;
    private int eventScore = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Vérifie et déclenche un événement si nécessaire.
    /// Retourne true si un événement est actif (bloque le gameplay normal).
    /// </summary>
    public bool CheckAndTriggerEvent(int currentDay, DayTime currentTime)
    {
        // Si un événement est actif, vérifier si on a atteint la fin
        if (IsEventActive())
        {
            // Comparer le temps actuel avec la fin de l'événement
            if (HasEventEnded(currentDay, currentTime))
            {
                EndCurrentEvent();
                return false; // Événement terminé, on peut continuer normalement
            }
            return true; // Événement toujours actif, on bloque le gameplay
        }

        // Chercher un événement pour ce jour
        Debug.Log("Recherche d'un événement pour le jour " + currentDay);   
        BaseGameEvent eventToTrigger = GetEventForDay(currentDay);
        if (eventToTrigger != null)
        {
            UIManager.Instance.HideAllUIForMiniGame(); // S'assurer que le panel est caché avant de démarrer un nouvel événement
            StartEvent(eventToTrigger, currentDay, currentTime);
            return true; // Événement démarré, on bloque le gameplay
        }
        Debug.Log("Aucun événement prévu pour le jour " + currentDay);
        return false; // Pas d'événement
    }

    /// <summary>
    /// Vérifie si un événement est actuellement actif.
    /// </summary>
    public bool IsEventActive()
    {
        return eventEndDay != -1 && currentEvent != null;
    }

    /// <summary>
    /// Vérifie si le temps actuel a dépassé la fin de l'événement.
    /// </summary>
    private bool HasEventEnded(int currentDay, DayTime currentTime)
    {
        if (currentDay > eventEndDay) return true;
        if (currentDay == eventEndDay && currentTime >= eventEndTime) return true;
        return false;
    }

    /// <summary>
    /// Récupère l'événement prévu pour un jour donné.
    /// </summary>
    private BaseGameEvent GetEventForDay(int day)
    {
        foreach (var eventDay in scheduledEvents)
        {
            if (eventDay.day == day)
            {
                return eventDay.gameEvent;
            }
        }
        return null;
    }

    /// <summary>
    /// Démarre un événement : affiche le panel, lance la scène.
    /// </summary>
    private void StartEvent(BaseGameEvent gameEvent, int currentDay, DayTime currentTime)
    {
        currentEvent = gameEvent;
        EventInfo info = gameEvent.GetEventInfo();

        // Calculer la fin de l'événement
        CalculateEventEnd(currentDay, currentTime, info.durationHalfDays);

        // Afficher le panel UI avec l'image et la description
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEventPanel(info.eventImage, info.eventName, info.description);
        }

        // Lancer l'événement (charge la scène du mini-jeu)
        gameEvent.StartEvent();

        Debug.Log($"Événement '{info.eventName}' démarré. Durée : {info.durationHalfDays} demi-journées. Fin prévue : Jour {eventEndDay} {eventEndTime}");
    }

    /// <summary>
    /// Calcule le jour et l'heure de fin de l'événement.
    /// </summary>
    private void CalculateEventEnd(int startDay, DayTime startTime, int durationHalfDays)
    {
        int totalHalfDays = (startTime == DayTime.Matin ? 0 : 1) + durationHalfDays;
        eventEndDay = startDay + (totalHalfDays / 2);
        eventEndTime = (totalHalfDays % 2 == 0) ? DayTime.Matin : DayTime.Aprem;
    }

    /// <summary>
    /// Termine l'événement actuel et applique les récompenses.
    /// </summary>
    private void EndCurrentEvent()
    {
        if (currentEvent != null)
        {
            Debug.Log($"Événement '{currentEvent.eventInfo.eventName}' terminé avec score : {eventScore}");
            
            // Appliquer les récompenses selon le score
            currentEvent.ApplyRewards(eventScore);

            // Réinitialiser
            currentEvent = null;
            eventEndDay = -1;
            eventScore = 0;
        }
    }

    /// <summary>
    /// Appelé par BaseEventManager pour enregistrer le score final.
    /// </summary>
    public void SetEventScore(int score)
    {
        eventScore = score;
    }

    /// <summary>
    /// Retourne l'événement actuellement actif (ou null).
    /// </summary>
    public BaseGameEvent GetCurrentEvent()
    {
        return currentEvent;
    }

    /// <summary>
    /// Ajoute un événement programmé pour un jour et un moment spécifiques.
    /// </summary>
    public void AddEvent(BaseGameEvent gameEvent, int day, DayTime triggerTime)
    {
        EventDay newEventDay = new EventDay
        {
            day = day,
            triggerTime = triggerTime,
            gameEvent = gameEvent
        };
        scheduledEvents.Add(newEventDay);
        Debug.Log($"Événement '{gameEvent.eventInfo.eventName}' ajouté pour le jour {day} ({triggerTime})");
    }
}

/// <summary>
/// Structure pour associer un jour à un événement dans l'Inspector.
/// </summary>
[System.Serializable]
public class EventDay
{
    [Range(1, 28)]
    public int day;
    public DayTime triggerTime = DayTime.Matin; // Ajouter pour choisir matin ou après-midi
    public BaseGameEvent gameEvent;

}
