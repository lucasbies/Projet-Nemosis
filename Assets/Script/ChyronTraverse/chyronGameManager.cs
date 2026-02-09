using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class chyronGameManager : MonoBehaviour
{
    public float scrollSpeed = 3f;
    private float _baseScrollSpeed;
    public float score = 0f;

    public int maxLives = 3;
    public int currentLives;

    public float invincibilityDuration = 3f;
    public bool isInvincible = false;

    public float obstaclePenalty = 5f;

    public TMP_Text scoreText;
    public TMP_Text lifeText;

    [Header("Pièces")]
    public int coinScore = 0;
    public TMP_Text coinText;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip boatHitClip;

    public bool isGameOver = false;

    public SpriteRenderer spriteRenderer;
    PlayerLaneMovement player;

    private Coroutine vibrationCoroutine;

    // dérivé de la carte
    private float _chaosLevel = 0f;
    private float _rewardMult = 1f;
    private float _rewardFlat = 0f;
    private bool _oneMistakeFail = false;

    [Header("Tutorial")]
    public MiniGameTutorialPanel tutorialPanel;
    public VideoClip tutorialClip;
    private bool tutorialValidated = false;

    [Header("Paliers étoiles")]
    public int[] starThresholds = new int[3] { 100, 200, 300 };
    private bool[] starGiven = new bool[3];

    [Header("UI Étoiles")]
    public UnityEngine.UI.Image[] starImages;
    public Sprite starOnSprite;
    public Sprite starOffSprite;

    public bool IsPlaying => tutorialValidated && !isGameOver;

    void Start()
    {
        ShowTutorialAndStart();
        starGiven = new bool[3];
        UpdateStarsUI();
    }

    public void ShowTutorialAndStart()
    {
        // Pour Chyron, on utilise seulement MoveLeft et MoveRight (2 touches)
        InputAction[] actionsKeyboard = null;
        InputAction[] actionsGamepad = null;

        if (InputManager.Instance != null)
        {
            var kb = InputManager.Instance.keyboardControls;
            var gp = InputManager.Instance.gamepadControls;

            // IMPORTANT: Chyron n'utilise que 2 contrôles (Gauche/Droite)
            // On va créer un tableau avec seulement 2 actions
            if (kb != null)
            {
                try
                {
                    actionsKeyboard = new InputAction[]
                    {
                        kb.Gameplay.MoveLeft,
                        kb.Gameplay.MoveRight
                    };
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Chyron] Erreur lors de la récupération des actions clavier: {e.Message}");
                    actionsKeyboard = null;
                }
            }

            if (gp != null)
            {
                try
                {
                    actionsGamepad = new InputAction[]
                    {
                        gp.Gameplay.MoveLeft,
                        gp.Gameplay.MoveRight
                    };
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Chyron] Erreur lors de la récupération des actions gamepad: {e.Message}");
                    actionsGamepad = null;
                }
            }
        }

        // Affiche le panel de tutoriel
        if (tutorialPanel != null)
        {
            tutorialPanel.ShowChyron(
                "La traversée de Chyron",
                actionsKeyboard,
                actionsGamepad,
                tutorialClip,
                "Conseil : Les marteaux et boucliers sont des bonus vous permettant de récupérer de la vie."
            );

            // Configurer le bouton continuer
            tutorialPanel.continueButton.onClick.RemoveAllListeners();
            tutorialPanel.continueButton.onClick.AddListener(() =>
            {
                tutorialPanel.Hide();
                tutorialValidated = true;
                StartGameAfterTutorial();
            });
        }
        else
        {
            Debug.LogError("[Chyron] TutorialPanel non assigné!");
            // Démarrer quand même le jeu
            tutorialValidated = true;
            StartGameAfterTutorial();
        }
    }

    private void StartGameAfterTutorial()
    {
        _baseScrollSpeed = scrollSpeed;
        ApplyMiniGameCardIfAny();

        currentLives = maxLives;
        player = FindFirstObjectByType<PlayerLaneMovement>();

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        starGiven = new bool[3];
        UpdateStarsUI();

        Debug.Log("[Chyron] Jeu démarré après tutoriel!");
    }

    void Update()
    {
        if (!tutorialValidated) return;
        if (isGameOver) return;

        score += scrollSpeed * Time.deltaTime;
        scoreText.text = "Score : " + Mathf.FloorToInt(score);
        lifeText.text = "Vies : " + currentLives + "/" + maxLives;

        if (coinText != null)
            coinText.text = "Pièces : " + coinScore;

        // Gestion des étoiles
        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (!starGiven[i] && score >= starThresholds[i])
            {
                starGiven[i] = true;
                if (GameManager.Instance != null)
                    GameManager.Instance.changeStat(StatType.Foi, 5f);
            }
        }
        UpdateStarsUI();
    }

    public void HitObstacle()
    {
        if (!isInvincible)
        {
            Vibrate(0.3f, 0.3f, 0.5f);
            PlayBoatHitSfx();
        }

        if (isInvincible || isGameOver) return;

        // Mode one mistake fail
        if (_oneMistakeFail)
        {
            Debug.Log("[Chyron] Mode oneMistakeFail : obstacle touché -> GameOver immédiat.");
            GameOver();
            return;
        }

        currentLives--;

        if (currentLives <= 0)
        {
            GameOver();
            return;
        }

        float chaosFactor = 1f + Random.Range(-_chaosLevel, _chaosLevel);
        float actualPenalty = obstaclePenalty * chaosFactor;

        scrollSpeed -= actualPenalty;
        if (scrollSpeed < 0) scrollSpeed = 0;

        StartCoroutine(InvincibilityRoutine());
    }

    private void PlayBoatHitSfx()
    {
        if (sfxSource == null || boatHitClip == null) return;
        sfxSource.PlayOneShot(boatHitClip);
    }

    void GameOver()
    {
        isGameOver = true;
        scrollSpeed = 0;
        Debug.Log("[Chyron] GAME OVER!");

        int finalScore = Mathf.FloorToInt(score);

        // TODO: Afficher un écran de game over avant de retourner au menu
        SceneManager.LoadScene("SampleScene");
    }

    public void UpdateStarsUI()
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (i < starImages.Length && starImages[i] != null)
            {
                starImages[i].sprite = starGiven[i] ? starOnSprite : starOffSprite;
            }
        }
    }

    // Invincibilité avec clignotement
    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerLaneMovement>();
        }

        if (player == null || player.spriteRenderer == null)
        {
            Debug.LogWarning("[Chyron] Player ou SpriteRenderer non trouvé pour l'invincibilité!");
            yield return new WaitForSeconds(invincibilityDuration);
            isInvincible = false;
            yield break;
        }

        SpriteRenderer sr = player.spriteRenderer;
        float elapsed = 0f;
        float blinkInterval = 0.15f;

        while (elapsed < invincibilityDuration)
        {
            sr.enabled = !sr.enabled;
            elapsed += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }

        sr.enabled = true;
        isInvincible = false;
    }

    // Système de vibration
    public void Vibrate(float left, float right, float duration)
    {
        if (Gamepad.current == null)
            return;

        if (vibrationCoroutine != null)
            StopCoroutine(vibrationCoroutine);

        vibrationCoroutine = StartCoroutine(VibrationRoutine(left, right, duration));
    }

    private IEnumerator VibrationRoutine(float left, float right, float duration)
    {
        Gamepad.current.SetMotorSpeeds(left, right);
        yield return new WaitForSeconds(duration);
        StopVibration();
    }

    private void StopVibration()
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);

        vibrationCoroutine = null;
    }

    public void HealPlayer(int amount)
    {
        if (currentLives >= maxLives) return;

        currentLives += amount;
        if (currentLives > maxLives)
            currentLives = maxLives;

        Debug.Log($"[Chyron] Soin +{amount} PV, Vies : {currentLives}/{maxLives}");
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxLives += amount;
        currentLives += amount;
        if (currentLives > maxLives)
            currentLives = maxLives;

        Debug.Log($"[Chyron] Bouclier +{amount} PV max, Vies : {currentLives}/{maxLives}");
    }

    public void AddCoin(int amount)
    {
        coinScore += amount;
        Debug.Log($"[Chyron] +{amount} pièce(s), Total : {coinScore}");
    }

    private void ApplyMiniGameCardIfAny()
    {
        var runtime = MiniGameCardRuntime.Instance;
        if (runtime == null || runtime.SelectedCard == null)
        {
            Debug.Log("[Chyron] Aucune carte de mini-jeu à appliquer.");
            return;
        }

        var card = runtime.SelectedCard;
        if (card.targetMiniGame != MiniGameType.Any && card.targetMiniGame != MiniGameType.Chyron)
        {
            Debug.Log($"[Chyron] Carte ignorée (cible: {card.targetMiniGame})");
            return;
        }

        float speedMult = Mathf.Max(0.1f, card.speedMultiplier);
        scrollSpeed = _baseScrollSpeed * speedMult;

        float diffMult = Mathf.Max(0.5f, card.difficultyMultiplier);

        if (diffMult > 1f)
        {
            maxLives = Mathf.Max(1, Mathf.RoundToInt(maxLives / diffMult));
        }
        else if (diffMult < 1f)
        {
            maxLives = Mathf.RoundToInt(maxLives / diffMult);
        }
        currentLives = maxLives;

        _chaosLevel = Mathf.Clamp01(card.chaosLevel);
        _rewardMult = Mathf.Max(0.1f, card.rewardMultiplier);
        _rewardFlat = card.rewardFlatBonus;
        _oneMistakeFail = card.oneMistakeFail;

        Debug.Log($"[Chyron] Carte appliquée : {card.cardName}, scroll x{speedMult}, maxLives={maxLives}, chaos={_chaosLevel}, rewardMult={_rewardMult}, rewardFlat={_rewardFlat}, oneMistakeFail={_oneMistakeFail}");

        runtime.Clear();
    }

    private void OnDestroy()
    {
        // Arrêter la vibration si le jeu est détruit
        StopVibration();
    }
}