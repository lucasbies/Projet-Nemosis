using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DaySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dayNumberText;
    [SerializeField] private TextMeshProUGUI weekDayText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Transform activitiesContainer;
    [SerializeField] private GameObject activityIconPrefab;

    private int day;
    private List<ScheduleEntry> activities;

    /// Configure le slot de jour
    public void Setup(int day, string weekDay, List<ScheduleEntry> activities, Color bgColor, bool isCurrentDay)
    {
        this.day = day;
        this.activities = activities;

        // Afficher le numéro du jour
        if (dayNumberText != null)
        {
            dayNumberText.text = day.ToString();
            if (isCurrentDay)
                dayNumberText.fontStyle = FontStyles.Bold;
        }

        // Afficher le jour de la semaine
        if (weekDayText != null)
            weekDayText.text = weekDay;

        // Couleur de fond
        if (backgroundImage != null)
            backgroundImage.color = bgColor;

        // Afficher les icônes d'activités
        DisplayActivities();

        // Ajouter un bouton pour voir les détails
        Button btn = GetComponent<Button>();
        if (btn == null)
            btn = gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnSlotClicked);

        // Animation d'apparition
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).SetDelay(day * 0.02f);
    }

    /// Affiche les icônes des activités du jour
    private void DisplayActivities()
    {
        if (activitiesContainer == null || activities == null)
            return;

        // Nettoyer les icônes existantes
        foreach (Transform child in activitiesContainer)
        {
            Destroy(child.gameObject);
        }

        // Créer les icônes
        foreach (var activity in activities)
        {
            if (activityIconPrefab != null && activity.icon != null)
            {
                GameObject iconObj = Instantiate(activityIconPrefab, activitiesContainer);
                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = activity.icon;
                }
            }
            else
            {
                // Indicateur simple si pas d'icône
                GameObject indicator = new GameObject("ActivityIndicator");
                indicator.transform.SetParent(activitiesContainer);
                Image img = indicator.AddComponent<Image>();
                img.color = GetColorForActivityType(activity.activityType);
                RectTransform rt = indicator.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(8, 8);
            }
        }
    }

    /// Retourne une couleur selon le type d'activité
    private Color GetColorForActivityType(ScheduleActivityType type)
    {
        switch (type)
        {
            case ScheduleActivityType.Village: return new Color(0.2f, 0.8f, 0.2f);
            case ScheduleActivityType.MiniJeu: return new Color(1f, 0.6f, 0.2f);
            case ScheduleActivityType.Relation: return new Color(1f, 0.3f, 0.8f);
            case ScheduleActivityType.Event: return new Color(0.8f, 0.2f, 0.2f);
            default: return Color.gray;
        }
    }

    /// Appelé quand on clique sur le slot
    private void OnSlotClicked()
    {
        Debug.Log($"[DaySlotUI] Jour {day} cliqué");

        // Animation de feedback
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5);

        // Afficher les détails du jour (à implémenter selon vos besoins)
        ShowDayDetails();
    }

    /// Affiche les détails des activités du jour
    private void ShowDayDetails()
    {
        if (activities == null || activities.Count == 0)
        {
            Debug.Log($"Jour {day} : Aucune activité planifiée");
            // 🆕 TODO: Afficher un panel vide élégant
            return;
        }

        string details = $"<b>📅 Jour {day}</b>\n\n";
        
        foreach (var activity in activities)
        {
            // 🆕 Icônes selon le type
            string icon = activity.activityType switch
            {
                ScheduleActivityType.Village => "🏘️",
                ScheduleActivityType.MiniJeu => "🎮",
                ScheduleActivityType.Relation => "💬",
                ScheduleActivityType.Event => "📢",
                _ => "📌"
            };

            details += $"{icon} <color=yellow>{activity.timeOfDay}</color> : <b>{activity.activityName}</b>\n";
            
            if (!string.IsNullOrEmpty(activity.description))
                details += $"  <i>{activity.description}</i>\n";
            
            details += "\n";
        }

        Debug.Log(details);
        
        // 🆕 TODO: Créer un vrai panel UI pour afficher ces détails
        // UIManager.Instance.ShowDayDetailsPanel(day, activities);
    }
}