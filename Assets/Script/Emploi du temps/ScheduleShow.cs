using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScheduleShow : MonoBehaviour
{
    [Header("Icônes")]
    public Sprite EventIcon;
    public Sprite MiniGameIcon;

    public DayTime currentTime;
    public GameManager gameManager;

    [Header("UI Texte Simple")]
    [SerializeField] private TextMeshProUGUI weekText;

    [Header("UI Avancée (optionnel)")]
    [SerializeField] private GameObject dayCardPrefab;
    [SerializeField] private Transform dayCardsContainer;
    [SerializeField] private bool useAdvancedUI = false;

    [Header("Couleurs")]
    [SerializeField] private Color todayColor = new Color(1f, 0.9f, 0.6f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color eventColor = new Color(0.6f, 0.8f, 1f, 1f);

    private EventScheduler eventScheduler;
    private List<DayCard> dayCards = new List<DayCard>();

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }
        eventScheduler = EventScheduler.Instance;

        if (useAdvancedUI && dayCardPrefab != null && dayCardsContainer != null)
        {
            InitializeAdvancedUI();
        }
        else
        {
            UpdateWeekText();
        }
    }

    // ============================================
    // MODE TEXTE AMÉLIORÉ
    // ============================================
    public void UpdateWeekText()
    {
        if (gameManager == null || weekText == null)
        {
            Debug.LogWarning("[ScheduleShow] gameManager ou weekText est null.");
            return;
        }

        if (eventScheduler == null)
        {
            BuildSimpleWeekText();
            return;
        }

        var scheduled = eventScheduler.scheduledEvents;
        StringBuilder sb = new StringBuilder();

        // En-tête stylisé
        sb.AppendLine("<size=120%><b>═══ PLANNING DE LA SEMAINE ═══</b></size>");
        sb.AppendLine();

        for (int i = 0; i < gameManager.weekDays.Length; i++)
        {
            int absoluteDay = gameManager.currentDay - ((int)((gameManager.currentDay - 1) % 7)) + i;
            string weekDayName = gameManager.weekDays[i];

            // Recherche événement
            string eventName = null;
            bool hasEvent = false;
            if (scheduled != null)
            {
                foreach (var eventDay in scheduled)
                {
                    if (eventDay.day == absoluteDay && eventDay.gameEvent != null)
                    {
                        var info = eventDay.gameEvent.GetEventInfo();
                        eventName = !string.IsNullOrEmpty(info.eventName)
                            ? info.eventName
                            : eventDay.gameEvent.name;
                        hasEvent = true;
                        break;
                    }
                }
            }

            // Mini-jeu dimanche
            bool isSunday = weekDayName == "Dimanche";
            bool hasMiniGame = isSunday;

            bool isToday = weekDayName == gameManager.currentWeekDay;

            // Construction de la ligne
            if (isToday)
            {
                sb.Append("<color=#FFD700>▶ </color><b><color=#FFE680>");
            }
            else
            {
                sb.Append("  ");
            }

            // Nom du jour
            sb.Append($"<size=110%>{weekDayName}</size>");

            // Contenu
            if (hasEvent || hasMiniGame)
            {
                sb.Append(" │ ");

                if (hasEvent)
                {
                    sb.Append($"<color=#87CEEB>📅 {eventName}</color>");
                }

                if (hasEvent && hasMiniGame)
                {
                    sb.Append(" + ");
                }

                if (hasMiniGame)
                {
                    sb.Append("<color=#98FB98>🎮 Mini-jeu</color>");
                }
            }
            else
            {
                sb.Append(" │ <color=#888888>─ Libre ─</color>");
            }

            if (isToday)
            {
                sb.Append("</color></b>");
            }

            if (i < gameManager.weekDays.Length - 1)
                sb.AppendLine();
        }

        weekText.text = sb.ToString();
    }

    private void BuildSimpleWeekText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>SEMAINE</b>");

        for (int i = 0; i < gameManager.weekDays.Length; i++)
        {
            string weekDayName = gameManager.weekDays[i];
            bool isToday = weekDayName == gameManager.currentWeekDay;

            if (isToday)
                sb.Append("<color=#FFD700>▶ <b>");
            else
                sb.Append("  ");

            sb.Append(weekDayName);

            if (isToday)
                sb.Append("</b></color>");

            if (i < gameManager.weekDays.Length - 1)
                sb.AppendLine();
        }

        weekText.text = sb.ToString();
    }

    // ============================================
    // MODE UI AVANCÉE AVEC CARTES
    // ============================================
    private void InitializeAdvancedUI()
    {
        // Nettoyer les cartes existantes
        foreach (var card in dayCards)
        {
            if (card != null && card.gameObject != null)
                Destroy(card.gameObject);
        }
        dayCards.Clear();

        // Créer une carte par jour
        for (int i = 0; i < gameManager.weekDays.Length; i++)
        {
            GameObject cardObj = Instantiate(dayCardPrefab, dayCardsContainer);
            DayCard card = cardObj.GetComponent<DayCard>();

            if (card != null)
            {
                dayCards.Add(card);
            }
        }

        UpdateAdvancedUI();
    }

    public void UpdateAdvancedUI()
    {
        if (!useAdvancedUI || dayCards.Count == 0) return;

        var scheduled = eventScheduler?.scheduledEvents;

        for (int i = 0; i < gameManager.weekDays.Length && i < dayCards.Count; i++)
        {
            int absoluteDay = gameManager.currentDay - ((int)((gameManager.currentDay - 1) % 7)) + i;
            string weekDayName = gameManager.weekDays[i];
            bool isToday = weekDayName == gameManager.currentWeekDay;

            // Recherche événement
            string eventName = "Libre";
            Sprite eventSprite = null;
            bool hasEvent = false;

            if (scheduled != null)
            {
                foreach (var eventDay in scheduled)
                {
                    if (eventDay.day == absoluteDay && eventDay.gameEvent != null)
                    {
                        var info = eventDay.gameEvent.GetEventInfo();
                        eventName = !string.IsNullOrEmpty(info.eventName)
                            ? info.eventName
                            : eventDay.gameEvent.name;
                        eventSprite = info.eventImage != null ? info.eventImage : EventIcon;
                        hasEvent = true;
                        break;
                    }
                }
            }

            // Mini-jeu
            bool hasMiniGame = weekDayName == "Dimanche";

            // Mettre à jour la carte
            DayCard card = dayCards[i];
            card.Setup(
                weekDayName,
                eventName,
                eventSprite,
                hasMiniGame ? MiniGameIcon : null,
                isToday,
                hasEvent,
                isToday ? todayColor : (hasEvent ? eventColor : normalColor)
            );
        }
    }

    // Appeler cette fonction quand le jour change
    public void OnDayChanged()
    {
        if (useAdvancedUI)
            UpdateAdvancedUI();
        else
            UpdateWeekText();
    }
}

// ============================================
// COMPOSANT POUR LES CARTES DE JOUR
// ============================================
public class DayCard : MonoBehaviour
{
    [Header("Références UI")]
    public TextMeshProUGUI dayNameText;
    public TextMeshProUGUI eventNameText;
    public Image eventIcon;
    public Image miniGameIcon;
    public Image backgroundImage;
    public GameObject todayIndicator;

    public void Setup(string dayName, string eventName, Sprite eventSprite,
                     Sprite miniGameSprite, bool isToday, bool hasEvent, Color bgColor)
    {
        if (dayNameText != null)
            dayNameText.text = dayName;

        if (eventNameText != null)
            eventNameText.text = eventName;

        if (eventIcon != null)
        {
            eventIcon.gameObject.SetActive(eventSprite != null);
            if (eventSprite != null)
                eventIcon.sprite = eventSprite;
        }

        if (miniGameIcon != null)
        {
            miniGameIcon.gameObject.SetActive(miniGameSprite != null);
            if (miniGameSprite != null)
                miniGameIcon.sprite = miniGameSprite;
        }

        if (backgroundImage != null)
            backgroundImage.color = bgColor;

        if (todayIndicator != null)
            todayIndicator.SetActive(isToday);
    }
}