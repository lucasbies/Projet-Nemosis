using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using DG.Tweening;
using TMPro;
using System.Collections;

public class TriGameManager : MonoBehaviour
{
    public static TriGameManager Instance;

    [Header("Game Settings")]
    public float gameDuration = 60f;
    private float remainingTime;
    private int score = 0;

    public bool IsPlaying { get; private set; } = false;

    [Header("References")]
    public Spawner spawner;
    public UIManagerTri uiManager;

    // Bases pour les modifs de carte
    private float _baseGameDuration;
    private float _baseSpawnInterval;
    private int _baseScorePerSoul;

    // paramètres dérivés de la carte
    private float _spawnChaos = 0f;
    private float _rewardMult = 1f;
    private float _rewardFlat = 0f;
    private bool _oneMistakeFail = false;

    [Header("Tutorial")]
    public MiniGameTutorialPanel tutorialPanel; // à assigner dans l'inspector
    public VideoClip tutorialClip; // à assigner dans l'inspector
    public bool tutorialValidated = false; // rendu public pour que Spawner puisse y accéder

    [Header("Paliers étoiles")]
    public int[] starThresholds = new int[3] { 30, 60, 100 };
    private bool[] starGiven = new bool[3];
    [Header("UI Étoiles")]
    public UnityEngine.UI.Image[] starImages;
    public Sprite starOnSprite;
    public Sprite starOffSprite;

    [Header("SFX & Feedback")]
    public AudioSource sfxSource;
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;

    // Tweens / états
    private Tween timerTween = null;
    private bool timerWarningPlaying = false;

    private void Awake()
    {
        // on vérifie qu'il n'y a qu'une instance de ce GameManager si il y en a plusieurs on détruit le nouveau
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    private void Start()
    {
        _baseGameDuration = gameDuration;

        // récup les valeurs de base du Spawner
        if (spawner != null)
        {
            _baseSpawnInterval = spawner.spawnInterval;
            // hypothèse : 1 point de score par âme de base
            _baseScorePerSoul = 1;
        }

        ApplyMiniGameCardIfAny();

        // Sécurité : s'assurer que rien ne tourne avant validation du tutoriel
        IsPlaying = false;
        if (spawner != null)
            spawner.StopSpawning();

        ShowTutorialAndStart();

        starGiven = new bool[3];
        // s'assurer que les étoiles sont prêtes à pop
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    // rendre la state "off" visible : sprite off + scale 1 + alpha réduit
                    starImages[i].transform.localScale = Vector3.one;
                    starImages[i].sprite = starOffSprite;
                    starImages[i].color = new Color(1f, 1f, 1f, 0.45f);
                }
            }
        }
        UpdateStarsUI();
    }

    private void Update()
    {
        // si tuto pas validé, on ne lance pas le timer
        if (!tutorialValidated) return;

        if (!IsPlaying) return;
        // Maj le timer
        remainingTime -= Time.deltaTime;
        uiManager.UpdateTimer(remainingTime);

        // Warning timer (ex: 10 secondes)
        HandleTimerWarning();

        if (remainingTime <= 0)
            EndGame();
    }

    public void StartGame()
    {
        // Protection : ne pas démarrer si le tutoriel n'est pas validé
        if (!tutorialValidated)
        {
            Debug.LogWarning("[Tri] StartGame ignoré : tutoriel non validé.");
            return;
        }

        // Réinitialisation des variables
        score = 0;
        remainingTime = gameDuration;
        IsPlaying = true;

        uiManager.HideEndScreen();
        uiManager.UpdateScore(score);
        spawner.StartSpawning();

        // petit effet d'entrée du timer/score
        if (uiManager.scoreText != null)
        {
            uiManager.scoreText.transform.localScale = Vector3.one * 0.8f;
            uiManager.scoreText.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
        }
        if (uiManager.timerText != null)
        {
            uiManager.timerText.transform.localScale = Vector3.one * 0.8f;
            uiManager.timerText.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.05f);
        }
    }


    public void AddScore(int soulsCount)
    {
        // Calcul score avec modifs de carte
        int prevScore = score;
        score += soulsCount * _baseScorePerSoul;
        uiManager.UpdateScore(score);

        // animation du texte score — couleur bleue pour réussite + SFX
        PlayScorePop(Color.cyan);
        if (sfxSource != null && sfxCorrect != null) sfxSource.PlayOneShot(sfxCorrect);

        // Paliers étoiles
        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (!starGiven[i] && score >= starThresholds[i])
            {
                starGiven[i] = true;
                if (GameManager.Instance != null)
                    GameManager.Instance.changeStat(StatType.Foi, 5f); // ou autre stat

                // joue animation étoile
                PlayStarPop(i);
            }
        }
        UpdateStarsUI();
    }

    public void UpdateStarsUI()
    {
        // Met à jour l'affichage des étoiles
        if (starImages == null) return;
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            bool on = i < starGiven.Length && starGiven[i];
            starImages[i].sprite = on ? starOnSprite : starOffSprite;
            starImages[i].color = on ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            starImages[i].transform.localScale = on ? Vector3.one : Vector3.one;
        }
    }

    public void EndGame()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        spawner.StopSpawning();

        GameObject[] souls = GameObject.FindGameObjectsWithTag("Soul");
        foreach (GameObject soul in souls)
        {
            Destroy(soul);
        }

        // afficher écran de fin puis animer avant de charger la scène
        uiManager.ShowEndScreen(score);
        StartCoroutine(EndGameSequence());
    }

    IEnumerator EndGameSequence()
    {
        // Si l'endScreen a un objet GameObject, on essaie d'animer son scale et le texte
        if (uiManager.endScreen != null)
        {
            var endGO = uiManager.endScreen;
            endGO.SetActive(true);
            RectTransform rt = endGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.zero;
                rt.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack);
            }
        }

        if (uiManager.endScoreText != null)
        {
            uiManager.endScoreText.alpha = 0f;
            uiManager.endScoreText.DOFade(1f, 0.7f).SetDelay(0.25f);
            // compter le score visuellement
            int startVal = 0;
            DOTween.To(() => startVal, x => {
                startVal = x;
                uiManager.endScoreText.text = $"Score : {startVal}";
            }, score, 1.0f).SetEase(Ease.OutCubic);
        }

        if (uiManager.endMessageText != null)
        {
            uiManager.endMessageText.canvasRenderer.SetAlpha(0f);
            uiManager.endMessageText.CrossFadeAlpha(1f, 0.8f, false);
        }

        // Laisser le temps à l'animation d'être visible
        yield return new WaitForSeconds(1.4f);

        // Retour à la scene principale
        SceneManager.LoadScene("SampleScene");
    }

    public void OnQuitMiniGame()
    {
        SceneManager.LoadScene("SampleScene");
    }


    private void ApplyMiniGameCardIfAny()
    {
        // vérifie si une carte est sélectionnée et applique ses effets
        var runtime = MiniGameCardRuntime.Instance;
        if (runtime == null || runtime.SelectedCard == null)
            return;

        var card = runtime.SelectedCard;
        if (card.targetMiniGame != MiniGameType.Any && card.targetMiniGame != MiniGameType.Tri)
            return;

        // applique les modifs de la carte
        float speedMult = Mathf.Max(0.1f, card.speedMultiplier);
        float diffMult = Mathf.Max(0.5f, card.difficultyMultiplier);
        float spawnMult = Mathf.Max(0.1f, card.spawnRateMultiplier);

        // plus de vitesse = partie plus courte
        gameDuration = _baseGameDuration / speedMult;

        if (spawner != null)
        {
            // vitesse + densité
            float interval = _baseSpawnInterval / speedMult;
            interval /= spawnMult;

            // applique du chaos(plein d'effets différents) au spawn des âmes
            _spawnChaos = Mathf.Clamp01(card.chaosLevel);

            spawner.spawnInterval = interval;
        }

        // score par âme augmenter avec la difficulté
        _baseScorePerSoul = Mathf.Max(1, Mathf.RoundToInt(_baseScorePerSoul * diffMult));

        // gains de stats globaux
        _rewardMult = Mathf.Max(0.1f, card.rewardMultiplier);
        _rewardFlat = card.rewardFlatBonus;
        _oneMistakeFail = card.oneMistakeFail;

        Debug.Log($"[Tri] Carte appliquée : {card.cardName}, duration={gameDuration}," +
            $" spawnInterval={spawner.spawnInterval}, scorePerSoul={_baseScorePerSoul}," +
            $" chaos={_spawnChaos}, rewardMult={_rewardMult}, rewardFlat={_rewardFlat}, " +
            $"oneMistakeFail={_oneMistakeFail}");

        runtime.Clear();
    }

    // Affiche le tuto et lance la partie après validation
    public void ShowTutorialAndStart()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.ShowSimple(
                "Tri des Âmes",
                tutorialClip,
                "Conseil : Glissez les âmes dans les bonnes zones !\n" +
                " Bonnes âmes → Zone bleue\n" +
                " Âmes neutres → Zone Jaune\n" +
                " Mauvaises âmes → Zone rouge"
            );

            tutorialPanel.continueButton.onClick.RemoveAllListeners();
            tutorialPanel.continueButton.onClick.AddListener(() =>
            {
                tutorialPanel.Hide();
                tutorialValidated = true;
                StartGame();
            });
        }
        else
        {
            tutorialValidated = true;
            StartGame();
        }
    }

    // ----------------------
    // Animations DOTween
    // ----------------------
    void PlayScorePop(Color flashColor)
    {
        if (uiManager == null || uiManager.scoreText == null) return;

        var t = uiManager.scoreText.transform;
        t.DOKill();
        Sequence s = DOTween.Sequence();
        s.Append(t.DOScale(1.25f, 0.12f).SetEase(Ease.OutBack));
        s.Append(t.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
        s.Play();

        // couleur rapide (paramétrable)
        Color original = uiManager.scoreText.color;
        uiManager.scoreText.DOColor(flashColor, 0.12f).OnComplete(() => uiManager.scoreText.DOColor(original, 0.2f));
    }

    void PlayStarPop(int index)
    {
        if (starImages == null || index < 0 || index >= starImages.Length) return;
        var img = starImages[index];
        if (img == null) return;

        img.DOKill();
        // scale + rotation + petit bounce
        Sequence s = DOTween.Sequence();
        s.Append(img.transform.DOScale(1.4f, 0.25f).SetEase(Ease.OutBack));
        s.Append(img.transform.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
        // rotation rapide et continue légère
        img.transform.DORotate(new Vector3(0, 0, 20f), 0.4f, RotateMode.Fast).SetLoops(2, LoopType.Yoyo);
        s.Play();
    }

    void HandleTimerWarning()
    {
        if (uiManager == null || uiManager.timerText == null) return;

        float warnThreshold = Mathf.Min(10f, gameDuration * 0.25f); // par défaut 10s ou 25% du temps
        if (remainingTime <= warnThreshold && !timerWarningPlaying)
        {
            timerWarningPlaying = true;
            // clignotement rouge
            timerTween?.Kill();
            timerTween = uiManager.timerText.DOColor(Color.red, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        else if (remainingTime > warnThreshold && timerWarningPlaying)
        {
            timerWarningPlaying = false;
            if (timerTween != null)
            {
                timerTween.Kill();
                timerTween = null;
            }
            uiManager.timerText.DOColor(Color.white, 0.25f);
        }
    }
}