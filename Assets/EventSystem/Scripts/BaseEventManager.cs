using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Classe de base pour tous les managers de mini-jeux événementiels.
/// Chaque mini-jeu doit hériter de cette classe et implémenter CalculateScore().
/// </summary>
public abstract class BaseEventManager : MonoBehaviour
{
    protected BaseGameEvent currentEvent;
    protected int finalScore = 0;

    void Start()
    {
        // Récupérer l'événement actuel depuis le scheduler
        if (EventScheduler.Instance != null)
        {
            currentEvent = EventScheduler.Instance.GetCurrentEvent();
            if (currentEvent == null)
            {
                Debug.LogError("BaseEventManager: Aucun événement actif trouvé!");
            }
        }
        
        OnEventStart();
    }

    /// <summary>
    /// Appelé au démarrage du mini-jeu (override optionnel).
    /// </summary>
    protected virtual void OnEventStart()
    {
        Debug.Log($"Mini-jeu démarré : {currentEvent?.eventInfo.eventName}");
    }

    /// <summary>
    /// Méthode abstraite à implémenter : calcule le score du mini-jeu.
    /// Appelée quand le mini-jeu se termine.
    /// </summary>
    protected abstract int CalculateScore();

    /// <summary>
    /// Appelé quand le joueur termine le mini-jeu.
    /// Calcule le score, l'envoie au scheduler, et retourne au jeu principal.
    /// </summary>
    public void CompleteEvent()
    {
        if (finalScore != 0)
        {
            Debug.Log($"Mini-jeu terminé avec score : {finalScore}");
            finalScore = CalculateScore();
            if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.SetEventScore(finalScore);
        }

        }
        ReturnToBaseGame();
    }

    /// <summary>
    /// Retourne à la scène principale du jeu.
    /// </summary>
    protected virtual void ReturnToBaseGame()
    {
        // Charger la scène principale (à adapter selon votre projet)
        // Option 1: Si vous connaissez le nom de votre scène principale
        SceneManager.LoadScene("SampleScene"); // À remplacer par le nom de votre scène
        
        UIManager.Instance.ShowMainUI();
        
        Debug.Log("Retour à la scène principale");
    }

    /// <summary>
    /// Méthode utilitaire pour mettre à jour le score en cours de partie.
    /// </summary>
    protected void UpdateScore(int newScore)
    {
        finalScore = newScore;
    }

    /// <summary>
    /// Méthode utilitaire pour obtenir le score actuel.
    /// </summary>
    protected int GetCurrentScore()
    {
        return finalScore;
    }
}
