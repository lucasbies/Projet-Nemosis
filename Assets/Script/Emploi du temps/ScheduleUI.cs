using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ScheduleUI : MonoBehaviour
{
    public static ScheduleUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject schedulePanel;
    [SerializeField] private GameObject daySlotPrefab;
    [SerializeField] private Transform scheduleGrid;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI weekNumberText;

    [Header("Navigation")]
    [SerializeField] private Button previousWeekButton;
    [SerializeField] private Button nextWeekButton;

    [Header("Apparence")]
    [SerializeField] private Color currentDayColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] private Color scheduledDayColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color pastDayColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color emptyDayColor = Color.white;

    private int currentWeekDisplay = 0; // 0-3 pour 4 semaines
    private readonly int daysPerWeek = 7;
    private List<GameObject> daySlots = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (schedulePanel != null)
            schedulePanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (previousWeekButton != null)
            previousWeekButton.onClick.AddListener(() => ChangeWeek(-1));

        if (nextWeekButton != null)
            nextWeekButton.onClick.AddListener(() => ChangeWeek(1));
    }

    /// <summary>
    /// Affiche l'emploi du temps
    /// </summary>
    public void Show()
    {
        if (schedulePanel == null)
        {
            Debug.LogWarning("[ScheduleUI] schedulePanel n'est pas assigné !");
            return;
        }

        if (ScheduleManager.Instance == null)
        {
            Debug.LogError("[ScheduleUI] ScheduleManager.Instance est null ! Assurez-vous qu'il existe dans la scène.");
            return;
        }

        // Déterminer la semaine actuelle basée sur le jour en cours
        if (GameManager.Instance != null)
        {
            currentWeekDisplay = (GameManager.Instance.currentDay - 1) / daysPerWeek;
        }

        schedulePanel.SetActive(true);
        RefreshScheduleDisplay();

        // Animation d'ouverture
        schedulePanel.transform.localScale = Vector3.zero;
        schedulePanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }


    /// Masque l'emploi du temps
    public void Hide()
    {
        if (schedulePanel == null)
            return;

        schedulePanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            schedulePanel.SetActive(false);
        });
    }


    /// Change de semaine affichée
    private void ChangeWeek(int direction)
    {
        // Vérification de sécurité
        if (ScheduleManager.Instance == null)
        {
            Debug.LogError("[ScheduleUI] ScheduleManager.Instance est null dans ChangeWeek !");
            return;
        }

        int maxWeeks = (ScheduleManager.Instance.maxScheduleDays - 1) / daysPerWeek;
        currentWeekDisplay = Mathf.Clamp(currentWeekDisplay + direction, 0, maxWeeks);
        RefreshScheduleDisplay();
    }


    /// Rafraîchit l'affichage de l'emploi du temps
    public void RefreshScheduleDisplay()
    {
        if (scheduleGrid == null || daySlotPrefab == null)
        {
            Debug.LogWarning("[ScheduleUI] scheduleGrid ou daySlotPrefab manquant !");
            return;
        }

        if (ScheduleManager.Instance == null)
        {
            Debug.LogError("[ScheduleUI] ScheduleManager.Instance est null dans RefreshScheduleDisplay !");
            return;
        }

        // Nettoyer les slots existants
        foreach (var slot in daySlots)
        {
            Destroy(slot);
        }
        daySlots.Clear();

        // Mettre à jour le numéro de semaine
        if (weekNumberText != null)
        {
            weekNumberText.text = $"Semaine {currentWeekDisplay + 1}";
        }

        // Créer les slots pour la semaine
        int startDay = currentWeekDisplay * daysPerWeek + 1;
        int endDay = Mathf.Min(startDay + daysPerWeek - 1, ScheduleManager.Instance.maxScheduleDays);

        for (int day = startDay; day <= endDay; day++)
        {
            CreateDaySlot(day);
        }

        // Mettre à jour les boutons de navigation
        if (previousWeekButton != null)
            previousWeekButton.interactable = currentWeekDisplay > 0;

        if (nextWeekButton != null)
        {
            int maxWeeks = (ScheduleManager.Instance.maxScheduleDays - 1) / daysPerWeek;
            nextWeekButton.interactable = currentWeekDisplay < maxWeeks;
        }
    }


    /// Crée un slot de jour dans l'emploi du temps
    private void CreateDaySlot(int day)
    {
        GameObject slotObj = Instantiate(daySlotPrefab, scheduleGrid);
        daySlots.Add(slotObj);

        slotObj.transform.localScale = Vector3.one * 0.5f;
        slotObj.transform.localPosition = Vector3.zero;

        DaySlotUI slotUI = slotObj.GetComponent<DaySlotUI>();
        if (slotUI == null)
            slotUI = slotObj.AddComponent<DaySlotUI>();

        // Récupérer le jour de la semaine
        string weekDay = GameManager.Instance != null
            ? GameManager.Instance.weekDays[(day - 1) % 7]
            : "";

        // Vérifier si c'est le jour actuel
        bool isCurrentDay = GameManager.Instance != null && GameManager.Instance.currentDay == day;

        // Vérifier si c'est un jour passé
        bool isPastDay = GameManager.Instance != null && day < GameManager.Instance.currentDay;

        // Récupérer les activités du jour
        List<ScheduleEntry> daySchedule = ScheduleManager.Instance != null
            ? ScheduleManager.Instance.GetDaySchedule(day)
            : new List<ScheduleEntry>();

        // 🆕 Déterminer la couleur selon l'état ET le type d'activité principale
        Color slotColor = DetermineSlotColor(day, isCurrentDay, isPastDay, daySchedule);

        slotUI.Setup(day, weekDay, daySchedule, slotColor, isCurrentDay);
    }

    /// <summary>
    /// 🆕 Détermine la couleur du slot selon son état et son contenu
    /// </summary>
    private Color DetermineSlotColor(int day, bool isCurrentDay, bool isPastDay, List<ScheduleEntry> schedule)
    {
        // Priorité 1 : Jour actuel (bleu clair)
        if (isCurrentDay)
            return currentDayColor;

        // Priorité 2 : Jour passé (gris)
        if (isPastDay)
            return pastDayColor;

        // Priorité 3 : Activités planifiées (vert ou orange selon le type)
        if (schedule != null && schedule.Count > 0)
        {
            // Vérifier si contient un événement spécial
            bool hasEvent = schedule.Exists(s => s.activityType == ScheduleActivityType.Event);
            bool hasMiniGame = schedule.Exists(s => s.activityType == ScheduleActivityType.MiniJeu);

            if (hasEvent)
                return new Color(1f, 0.4f, 0.4f); // Rouge pour événements
            if (hasMiniGame)
                return new Color(1f, 0.8f, 0.3f); // Orange pour mini-jeux
            
            return scheduledDayColor; // Vert pour activités normales
        }

        // Par défaut : vide (blanc)
        return emptyDayColor;
    }
}