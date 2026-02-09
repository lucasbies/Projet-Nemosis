using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum DayTime { Matin, Aprem }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float statLossMultiplier = 1.0f;

    [Tooltip("Multiplicateur appliqué aux stats")]
    public Dictionary<StatType, float> Multiplicateur = new Dictionary<StatType, float>();
    public Dictionary<StatType, float> Valeurs = new Dictionary<StatType, float>();

    // Valeur maximale des stats (ajoutée pour conserver la fonctionnalité changeStatMax)
    public Dictionary<StatType, float> MaxValeurs = new Dictionary<StatType, float>();

    [Tooltip("Nombre de cartes à piocher par set")]
    public int cardsToDraw = 3; // separer en set apres les tests ?
    public int RerollMax = 1;
    public int RerollsRemaining = 1;

    [Header("Jour actuel")]
    public int currentDay = 1;
    public DayTime currentTime = DayTime.Matin;
    public string currentWeekDay = "Lundi";
    public enum GameMode { village, VillageCard, Relation, }
    public GameMode currentGameMode;

    [Header("Event System")]
    public EventScheduler eventScheduler;

    [Header("Cartes disponibles (Set Village)")]
    public VillageCardCollectionSO villageCardCollection;

    [Header("Mini-jeu du dimanche")]
    public MiniGameLauncher miniGameLauncher;

    [Header("Fallback mini-jeux (utilisé si MiniGameLauncher non assigné)")]
    [Tooltip("Liste de scènes de mini-jeux (noms exacts Build Settings). Si vide, 'RhythmScene' sera utilisée.")]
    public string[] sundayMiniGameFallbackScenes = { "RhythmScene" };

    [Header("Durée de la campagne")]
    [Tooltip("Nombre total de jours dans une partie (1 mois = 28 jours)")]
    public int totalDays = 28;
    [Tooltip("Scène chargée quand la campagne est terminée")]
    public string endSceneName = "EndScene";

   
    private bool campaignFinished = false;

    public EffectSO effet;
    public readonly string[] weekDays = { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Deuxième instance détectée, je me détruis.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameManager] Instance initialisée et marquée DontDestroyOnLoad.");
    }

    // Assets\Script\Manager\GameManager.cs
    private const string SAVE_KEY = "GAME_SAVE_v1";
    private const string NEW_GAME_FLAG_KEY = "NEW_GAME_REQUESTED";

    private void Start()
    {
        // Si une nouvelle partie a été demandée depuis le menu, on force la réinit
        int newGameRequested = PlayerPrefs.GetInt(NEW_GAME_FLAG_KEY, 0);
        if (newGameRequested == 1)
        {
            Debug.Log("[GameManager] Nouvelle partie demandée depuis le menu, réinitialisation complète.");
            PlayerPrefs.SetInt(NEW_GAME_FLAG_KEY, 0); // consommer le flag
            PlayerPrefs.Save();

            InitDefaultState();
        }
        else
        {
            // Cas normal : si aucune sauvegarde, on initialise par défaut
            if (!PlayerPrefs.HasKey(SAVE_KEY))
            {
                Debug.Log("[GameManager] Aucune sauvegarde détectée, initialisation par défaut.");
                InitDefaultState();
            }
            else
            {
                Debug.Log("[GameManager] Sauvegarde présente. L'état sera chargé uniquement via LoadGame().");
                // Ne pas appeler LoadGame() ici : il n'est appelé que depuis "Continuer" ou un bouton.
            }
        }

        if (SceneManager.GetActiveScene().name == "SampleScene" && UIManager.Instance != null)
        {
            ChooseGameMode();
        }
    }

    /// <summary>
    /// Initialise totalement la partie à l'état "nouvelle partie".
    /// Appelé quand il n'y a PAS de sauvegarde (ou après avoir tout reset).
    /// </summary>
    /// 
    #region Initialisation
    public void InitDefaultState()
    {
        Multiplicateur.Clear();
        Valeurs.Clear();
        MaxValeurs.Clear();

        Debug.Log("[GameManager] Initialisation de la nouvelle partie (état par défaut).");

        foreach (StatType stat in StatType.GetValues(typeof(StatType)))
        {
            Multiplicateur[stat] = 1f;
            Valeurs[stat] = stat == StatType.Nemosis ? 0f : 50f;
            MaxValeurs[stat] = 100f; // valeur par défaut max
            changeStat(stat, 0f);
        }

        //changeStat(StatType.Nemosis, -50f);

        currentDay = 1;
        currentTime = DayTime.Matin;
        currentWeekDay = "Lundi";
        campaignFinished = false;

        UIManager.Instance?.changeDateUI();
    }

    #endregion
    #region Set stats/Max
    public void changeStat(StatType type, float amount)
    {
        if (!Multiplicateur.ContainsKey(type))
        {
            Multiplicateur[type] = 1f;
        }
        if (!Valeurs.ContainsKey(type))
        {
            Valeurs[type] = 0f;
        }
        if (!MaxValeurs.ContainsKey(type))
        {
            MaxValeurs[type] = 100f;
        }
        float ancienvaleur = Valeurs[type];
        // Si amount positif, applique le multiplicateur ; si négatif, pas de multiplier
        float proposedDelta = (amount > 0) ? amount * Multiplicateur[type] : amount;
        // Clamp pour garder la valeur entre -MaxValeurs[type] et MaxValeurs[type]
        float clampedDelta = (ancienvaleur + proposedDelta) > MaxValeurs[type]
            ? MaxValeurs[type] - ancienvaleur
            : proposedDelta;
        Valeurs[type] += clampedDelta;
        Debug.Log($"Stat {type} etais a {ancienvaleur}. + {clampedDelta} = {Valeurs[type]}");
        GameEvents.TriggerStatChanged(type, Valeurs[type]);

        // Nouvelle vérification : si Nemosis atteint >= 100 -> fin de la partie
        if (type == StatType.Nemosis && Valeurs[type] >= 100f)
        {
            Debug.Log("[GameManager] Nemosis >= 100 : défaite détectée -> chargement de la scène de fin.");
            EndCampaign();
        }
    }

    /// <summary>
    /// Modifie la valeur maximale d'une stat (utilisé par certains effets).
    /// Retourne la nouvelle valeur maximale.
    /// </summary>
    public float changeStatMax(StatType type, float amount)
    {
        if (!MaxValeurs.ContainsKey(type))
            MaxValeurs[type] = 100f;
        MaxValeurs[type] += amount;
        // si on réduit max en dessous de la valeur courante, on clamp la valeur courante
        if (Valeurs.ContainsKey(type) && Valeurs[type] > MaxValeurs[type])
        {
            Valeurs[type] = MaxValeurs[type];
            GameEvents.TriggerStatChanged(type, Valeurs[type]);
        }
        // Debug.Log($"Max value for {type} changed by {amount}, new max: {MaxValeurs[type]}");
        return MaxValeurs[type];
    }
    #endregion
    /// <summary>
    /// Choix du mode de jeu "normal".
    /// - Si dimanche matin -> mini-jeu aléatoire automatiquement
    /// - Sinon -> affiche le ModeChoiceUI (panel jour/mode)
    /// </summary>
    #region Game Mode Choice
    public void ChooseGameMode()
    {
        if (campaignFinished) return;

        if (currentWeekDay == "Dimanche" && currentTime == DayTime.Matin)
        {
            LaunchSundayMiniGame();
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIActive(true);
            UIManager.Instance.GameModeChoice();   // c’est UIManager qui gère le focus
        }
    }

    /// <summary>
    /// Appelé depuis un bouton du ModeChoiceUI pour ouvrir le panel des cartes mini-jeu.
    /// (Ne dépend plus de ChooseGameMode)
    /// </summary>


    private void LaunchSundayMiniGame()
    {
        Debug.Log("[GameManager] Dimanche matin : tentative de lancement d'un mini-jeu aléatoire !");

        // Ne pas désactiver le GameObject UIManager (corrige l'erreur Coroutine couldn't be started...)
        if (UIManager.Instance != null)
        {
            // Masque proprement les panels et marque qu'on lance un mini‑jeu
            UIManager.Instance.HideAllUIForMiniGame();
            UIManager.Instance.MarkMiniGameLaunch();
        }

        // Si un MiniGameLauncher est assigné, déléguer le choix
        if (miniGameLauncher != null)
        {
            miniGameLauncher.LaunchRandomSundayMiniGame();
            Debug.Log("[GameManager] MiniGameLauncher utilisé pour lancer un mini‑jeu.");
            return;
        }

        // Sinon, fallback : choisir une scène aléatoire dans la liste publique
        if (sundayMiniGameFallbackScenes != null && sundayMiniGameFallbackScenes.Length > 0)
        {
            string scene = sundayMiniGameFallbackScenes[Random.Range(0, sundayMiniGameFallbackScenes.Length)];
            Debug.Log($"[GameManager] Fallback : chargement de la scène mini‑jeu '{scene}'.");
            SceneManager.LoadScene(scene);
            return;
        }

        // Dernier recours : charger RhythmScene
        Debug.LogWarning("[GameManager] Aucun MiniGameLauncher ni fallback défini, chargement de 'RhythmScene'.");
        SceneManager.LoadScene("RhythmScene");
    }

    public void MiniJeuCardPanelAnimation()
    {
        if(DOTweenManager.Instance.IsAnimating) return;
        StartCoroutine(DOTweenManager.Instance.transitionChoixJeu(OpenMiniJeuCardPanel));
    }
    public void OpenMiniJeuCardPanel()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.SetUIActive(true);
        UIManager.Instance.ShowMiniJeuCardPanel();
    }

    public void ChooseVillageAnimation()
    {
         if(DOTweenManager.Instance.IsAnimating) return;
        StartCoroutine(DOTweenManager.Instance.transitionChoixJeu(ChooseVillage));
    }

    public void ChooseVillage()
    {
        currentGameMode = GameMode.village;
        VillageManager.Instance.AfficheBuildings();
    }

    public void ChooseVillageCardsTransition()
    {
        if(DOTweenManager.Instance.IsAnimating) return;
        StartCoroutine(DOTweenManager.Instance.transitionChoixJeu(ChooseVillageCards));
    }

     public void ChooseVillageCards()
    {
        currentGameMode = GameMode.VillageCard;
        UIManager.Instance.VillageCardChoice(villageCardCollection, cardsToDraw);
    }

    public void RelationTransitionAnimation()
    {
        if(DOTweenManager.Instance.IsAnimating) return;
        StartCoroutine(DOTweenManager.Instance.transitionChoixJeu(ChooseRelationTransition));
    }

    public void ChooseRelationTransition()
    {
        currentGameMode = GameMode.Relation;
        ChooseRelationUI.Instance.Open();
    }
    #endregion
    #region Time Management

    private bool isProcessingHalfDay = false; 

    public void EndHalfDay()
    {
        if (campaignFinished) return;
        
        if (isProcessingHalfDay)
        {
            Debug.LogWarning("[GameManager] EndHalfDay déjà en cours, appel ignoré.");
            return;
        }
        
        isProcessingHalfDay = true; 

        RerollsRemaining = RerollMax;

        if (currentTime == DayTime.Matin)
        {
            currentTime = DayTime.Aprem;
            GameEvents.TriggerMorningEnd();
        }
        else
        {
            currentTime = DayTime.Matin;
            currentDay++;
            currentWeekDay = weekDays[(currentDay - 1) % 7];
            StartCoroutine(EndDay());
        }

        Debug.Log($"[GameManager] EndHalfDay : {currentTime} du jour {currentDay} ({currentWeekDay})");

        if (currentDay > totalDays)
        {
            EndCampaign();
            isProcessingHalfDay = false; // 🆕 Déverrouille
            return;
        }

        if (eventScheduler != null)
        {
            bool eventActive = eventScheduler.CheckAndTriggerEvent(currentDay, currentTime);
            if (eventActive)
            {
                Debug.Log("[GameManager] Événement actif, gameplay normal en pause.");
                isProcessingHalfDay = false; // 🆕 Déverrouille
                return;
            }
        }

        ChooseGameMode();
        isProcessingHalfDay = false; // 🆕 Déverrouille
    }

    public IEnumerator EndDay()
    {
        yield return new WaitForSeconds(2.8f);
        if (campaignFinished) yield break;

        currentWeekDay = weekDays[(currentDay - 1) % 7];

        float foodLoss = Mathf.Round(Valeurs[StatType.Human] * 0.1f);
        if (Valeurs[StatType.Food] >= foodLoss)
        {
            changeStat(StatType.Food, -foodLoss);
        }
        else
        {
            changeStat(StatType.Human, -10);
            changeStat(StatType.Food, -Valeurs[StatType.Food]); 
        }

        Debug.Log($"[GameManager] Fin de journée : Perte de nourriture de {foodLoss}, Nourriture restante : {Valeurs[StatType.Food]}");

        GameEvents.TriggerDayEnd();

        if (currentDay >= totalDays && currentTime == DayTime.Aprem)
        {
            EndCampaign();
        }
    }
    #endregion

    private void EndCampaign()
    {
        if (campaignFinished) return;
        campaignFinished = true;

        Debug.Log($"[GameManager] Fin de campagne : {totalDays} jours écoulés.");

        UIManager.Instance?.SetUIActive(false);

        if (!string.IsNullOrEmpty(endSceneName))
            SceneManager.LoadScene(endSceneName);
    }

    #region DEBUG

    public void DebugSkipToSunday()
    {
        if (campaignFinished) return;

        int currentDayIndex = (currentDay - 1) % 7; // 0 = Lundi, 6 = Dimanche
        int daysUntilSunday = (6 - currentDayIndex + 7) % 7;

        if (daysUntilSunday == 0 && currentTime == DayTime.Aprem)
            daysUntilSunday = 7;

        currentDay += daysUntilSunday;
        currentTime = DayTime.Matin;
        currentWeekDay = "Dimanche";

        Debug.Log($"[DEBUG] Saut au dimanche matin ! Jour {currentDay}");

        UIManager.Instance?.changeDateUI();
        ChooseGameMode();
    }

    #endregion
    #region Sauvegarde / Chargement
    // --- SAUVEGARDE / CHARGEMENT ---
    [System.Serializable]
    private class SaveData
    {
        public int currentDay;
        public int currentTime;      // cast de l'enum DayTime
        public string currentWeekDay;

        public Dictionary<StatType, float> stats;
    }


    public void SaveGame()
    {
        var data = new SaveData
        {
            currentDay = currentDay,
            currentTime = (int)currentTime,
            currentWeekDay = currentWeekDay,
            stats = new Dictionary<StatType, float>(Valeurs)
        };

        var wrapper = new SaveWrapper
        {
            currentDay = data.currentDay,
            currentTime = data.currentTime,
            currentWeekDay = data.currentWeekDay,
            statKeys = new List<string>(),
            statValues = new List<float>()
        };

        foreach (var kvp in data.stats)
        {
            wrapper.statKeys.Add(kvp.Key.ToString());
            wrapper.statValues.Add(kvp.Value);
        }

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("[GameManager] Partie sauvegardée : " + json);
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.LogWarning("[GameManager] Aucune sauvegarde trouvée.");
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        if (wrapper == null)
        {
            Debug.LogError("[GameManager] Échec du chargement de la sauvegarde.");
            return;
        }

        currentDay = wrapper.currentDay;
        currentTime = (DayTime)wrapper.currentTime;
        currentWeekDay = wrapper.currentWeekDay;

        Valeurs.Clear();
        Multiplicateur.Clear();
        MaxValeurs.Clear();

        for (int i = 0; i < wrapper.statKeys.Count; i++)
        {
            if (System.Enum.TryParse(wrapper.statKeys[i], out StatType stat))
            {
                float value = wrapper.statValues[i];
                Valeurs[stat] = value;
                if (!Multiplicateur.ContainsKey(stat))
                    Multiplicateur[stat] = 1f;
                if (!MaxValeurs.ContainsKey(stat))
                    MaxValeurs[stat] = 100f;

                GameEvents.TriggerStatChanged(stat, value);
            }
        }

        UIManager.Instance?.changeDateUI();

        Debug.Log("[GameManager] Partie chargée : " + json);
    }

    [System.Serializable]
    private class SaveWrapper
    {
        public int currentDay;
        public int currentTime;
        public string currentWeekDay;

        public List<string> statKeys;
        public List<float> statValues;
    }
    #endregion
}