using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Video;

public class ChronosGameManager : MonoBehaviour
{
    public static ChronosGameManager Instance;

    [Header("Player")]
    public int playerMaxHP = 92;
    public int playerHP;
    public Image playerHPBar;
    public TMP_Text playerHPText;

    [Header("Boss HP")]
    public int bossMaxHearts = 6;
    public int bossHeartHP = 4;
    public int bossCurrentHearts;
    public int bossCurrentHP;

    [Header("Tutorial")]
    public MiniGameTutorialPanel tutorialPanel;
    public VideoClip tutorialClip;
    public bool tutorialValidated = false;
    public int BossPhase => bossMaxHearts - bossCurrentHearts + 1;

    [Header("Music")]
    public AudioClip preCombatMusic;
    public AudioClip bossMusic;

    [Header("HP Bar Sprites")]
    public Sprite hpFull;
    public Sprite hpHalf;
    public Sprite hpLow;
    public Sprite hpEmpty;

    [Header("Boss Hearts UI")]
    public Image[] bossHeartImages;
    private Tween[] heartRotateTweens;
    public Sprite heartEmpty;
    public Sprite heartQuarter;
    public Sprite heartHalf;
    public Sprite heartThreeQuarters;
    public Sprite heartFull;

    [Header("Boss Image (fade)")]
    public Image bossImage; // <-- Référence à l'image du boss à placer dans l'inspecteur

    [Header("Boss Movement (infinite)")]
    public bool enableBossInfinityMovement = true;
    [Tooltip("Vitesse globale du mouvement")]
    public float infinitySpeed = 1f;
    [Tooltip("Amplitude X en pixels (anchoredPosition)")]
    public float infinityRadiusX = 120f;
    [Tooltip("Amplitude Y en pixels (anchoredPosition)")]
    public float infinityRadiusY = 60f;

    [Header("Boss Trail (rémanence)")]
    [Tooltip("Intervalle entre spawn de traînée en secondes")]
    public float trailSpawnInterval = 0.06f;
    [Tooltip("Nombre maximum d'images rémanentes (pool)")]
    public int trailPoolSize = 12;
    [Tooltip("Durée du fondu des rémanences")]
    public float trailFadeDuration = 0.8f;
    [Tooltip("Échelle appliquée aux rémanences")]
    public float trailScaleMultiplier = 0.95f;

    [Header("UI")]
    public TMP_Text dialogueText;

    public bool isPausedForJewel = false;
    public GameObject gamepadCursor;

    private Coroutine hpAnimCoroutine;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip sfxDamage;
    public AudioClip sfxHeal;
    public AudioClip sfxAttack;

    // Cache
    private RectTransform playerHPBarRect;
    private RectTransform playerHPTextRect;
    private RectTransform dialogueTextRect;
    private Camera mainCamera;

    // Boss movement cache
    private RectTransform bossRect;
    private Vector2 bossCenterAnchored;
    private float infinityT = 0f;
    private float trailTimer = 0f;

    // Trail pool
    private Image[] trailPool;
    private int trailIndex = 0;

    // Constantes pré-calculées
    private const float HP_ANIM_DURATION = 0.5f;
    private const float SHAKE_DURATION = 0.2f;
    private const float SCALE_DURATION = 0.2f;
    private const float LOW_HP_THRESHOLD = 0.2f;

    // Colors cache
    private static readonly Color colorRed = Color.red;
    private static readonly Color colorGreen = Color.green;
    private static readonly Color colorWhite = Color.white;

    void Awake()
    {
        Instance = this;

        // Cache RectTransforms
        if (playerHPBar != null)
            playerHPBarRect = playerHPBar.rectTransform;

        if (playerHPText != null)
            playerHPTextRect = playerHPText.rectTransform;

        if (dialogueText != null)
            dialogueTextRect = dialogueText.rectTransform;

        mainCamera = Camera.main;
    }

    void Start()
    {


        heartRotateTweens = new Tween[bossMaxHearts];
        playerHP = playerMaxHP;
        bossCurrentHearts = bossMaxHearts;
        bossCurrentHP = bossHeartHP;
        UpdateUI();

        // Jouer la musique pré-combat
        if (preCombatMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLoopMusic(preCombatMusic);
        }

        ShowTutorialAndStart();
        // si non validé, ne pas démarrer le combat
        if (!tutorialValidated) return;
        // Initialiser l'alpha de l'image du boss à pleine opacité
        UpdateBossImageAlpha(instant: true);

        // Boss rect + centre pour le mouvement
        if (bossImage != null)
        {
            bossRect = bossImage.rectTransform;
            bossCenterAnchored = bossRect.anchoredPosition;
            InitializeTrailPool();
        }

        // ROUTER la source SFX vers le groupe "SFX" du mixer (si présent)
        if (sfxSource != null)
        {
            AudioMixerGroup target = null;

            if (AudioManager.Instance != null && AudioManager.Instance.masterMixer != null)
            {
                var groups = AudioManager.Instance.masterMixer.FindMatchingGroups("SFX");
                if (groups != null && groups.Length > 0)
                    target = groups[0];
            }

            if (target == null)
            {
                var all = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();
                foreach (var g in all)
                {
                    if (g != null && g.name.ToLowerInvariant().Contains("sfx"))
                    {
                        target = g;
                        break;
                    }
                }
            }

            if (target != null)
                sfxSource.outputAudioMixerGroup = target;
        }

        dialogueText.text = "* Chronos t'observe avec un sourire.";
    }

    void OnDestroy()
    {
        // Cleanup tweens
        if (heartRotateTweens != null)
        {
            for (int i = 0; i < heartRotateTweens.Length; i++)
            {
                if (heartRotateTweens[i] != null && heartRotateTweens[i].IsActive())
                    heartRotateTweens[i].Kill();
            }
        }

        // Kill any active tweens on pool
        if (trailPool != null)
        {
            foreach (var t in trailPool)
                if (t != null) t.DOKill();
        }

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (enableBossInfinityMovement && bossImage != null && bossRect != null)
            AnimateBossInfinity();
    }

    private void ShowTutorialAndStart()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.ShowChronos(
                "Chronos - Combat Épique",
            tutorialClip,
            "C'EST LE COMBAT FINAL\n\n" +
            "DÉPLACEMENT :\n" +
            "• ZQSD / Flèches / Stick : Se déplacer\n" +
            "• Esquivez les projectiles du boss\n\n" +
            "ATTAQUES SPÉCIALES :\n" +
            "• Mode Justice : 4 directions (↑↓←→)\n" +
            "• Boucliers : ZQSD pour bloquer\n\n" +
            "JOYAUX :\n" +
            "• Collectez-les pour choisir :\n" +
            "  Clic/Bouton A → Attaquer ou Soigner\n\n" +
            "OBJECTIF :\n" +
            "• Détruisez les 6 sabliers de Chronos\n" +
            "• 4 coups par sabliers !\n" +
            "• Chaque phase est plus difficile !"
            );

            tutorialPanel.continueButton.onClick.RemoveAllListeners();
            tutorialPanel.continueButton.onClick.AddListener(() =>
            {
                tutorialPanel.Hide();
                tutorialValidated = true;

                // Arrêter la musique pré-combat et lancer la musique du boss
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopLoopMusic();

                    if (bossMusic != null)
                    {
                        AudioManager.Instance.PlayLoopMusic(bossMusic);
                    }
                }

                StartBossFight(); // Votre méthode pour démarrer le combat
            });
        }
        else
        {
            tutorialValidated = true;
            StartBossFight();
        }
    }

    // -----------------------------
    // Nouvelle méthode pour démarrer le combat
    // -----------------------------
    public void StartBossFight()
    {

        // Flag tutoriel
        tutorialValidated = true;
        isPausedForJewel = false;

        // Réinitialisation des PV / UI
        playerHP = playerMaxHP;
        bossCurrentHearts = bossMaxHearts;
        bossCurrentHP = bossHeartHP;
        UpdateUI();

        // Remettre le boss visible et actif
        enableBossInfinityMovement = true;
        if (bossImage != null)
        {
            bossImage.gameObject.SetActive(true);
            UpdateBossImageAlpha(instant: true);
            bossImage.raycastTarget = true;
        }

        // Trouver ou spawner le PlayerSoul
        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerSoul");
        ChronosAttackController ac = FindFirstObjectByType<ChronosAttackController>();

        Vector3 spawnPos = Vector3.zero;
        if (ac != null && ac.arena != null)
        {
            var box = ac.arena.GetComponent<BoxCollider2D>();
            if (box != null) spawnPos = box.bounds.center;
            else spawnPos = ac.arena.position;
        }

        if (playerObj == null)
        {
            PlayerSoul spawned = null;
            try
            {
                spawned = PlayerSoul.Spawn(spawnPos, Quaternion.identity);
            }
            catch
            {
                spawned = null;
            }

            if (spawned != null)
                playerObj = spawned.gameObject;
        }

        if (playerObj != null)
        {
            var ps = playerObj.GetComponent<PlayerSoul>();
            if (ps != null)
            {
                ps.SetMovementEnabled(true);
                ps.ExitJusticeMode();
            }

            // s'assurer du tag pour que ChronosAttackController puisse le retrouver
            playerObj.tag = "PlayerSoul";
        }

        // Activer le contrôleur d'attaques (il fera son Init et démarrera sa boucle)
        if (ac != null)
        {
            // Propager éventuellement le curseur gamepad si nécessaire
            if (ac.gamepadCursor == null)
                ac.gamepadCursor = gamepadCursor;

            ac.enabled = true;
        }

        // SFX / audio defaults
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        // Initialiser trail pool si pas encore fait
        if (bossImage != null && bossRect == null)
        {
            bossRect = bossImage.rectTransform;
            bossCenterAnchored = bossRect.anchoredPosition;
            InitializeTrailPool();
        }

        if (dialogueText != null)
            dialogueText.text = "* Le combat commence !";

        // Mise à jour finale de l'UI
        UpdateUI();
    }

    public void DamagePlayer(int dmg)
    {
        int oldHP = playerHP;
        playerHP = Mathf.Max(playerHP - dmg, 0);

        PlayHPBarEffect(true);
        PlayPlayerDamageEffects();

        UpdateHPBar();
        UpdatePlayerHPText();

        if (playerHP <= 0)
        {
            dialogueText.text = "* Chronos à gagné. tu es mort.";
            StopAllCoroutines();
        }
    }

    private IEnumerator AnimateHPBarAndText(int fromHP, int toHP)
    {
        float elapsed = 0f;
        float startFill = (float)fromHP / playerMaxHP;
        float endFill = (float)toHP / playerMaxHP;

        while (elapsed < HP_ANIM_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / HP_ANIM_DURATION;

            float currentFill = Mathf.Lerp(startFill, endFill, t);
            int currentHP = Mathf.RoundToInt(Mathf.Lerp(fromHP, toHP, t));

            playerHPBar.fillAmount = currentFill;
            UpdatePlayerHPText(currentHP);

            yield return null;
        }

        // Valeur finale
        playerHPBar.fillAmount = endFill;
        UpdatePlayerHPText(toHP);

        // Low HP effect
        if (playerHP <= playerMaxHP * LOW_HP_THRESHOLD)
        {
            playerHPBar.DOColor(colorRed, SHAKE_DURATION).SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            playerHPBar.DOKill();
            playerHPBar.color = colorWhite;
        }

        UpdateUIBossHearts();
    }

    void UpdateUIBossHearts()
    {
        for (int i = 0; i < bossMaxHearts; i++)
        {
            if (i < bossCurrentHearts - 1)
            {
                bossHeartImages[i].sprite = heartFull;
            }
            else if (i == bossCurrentHearts - 1)
            {
                bossHeartImages[i].sprite = GetHeartSprite(bossCurrentHP);
            }
            else
            {
                bossHeartImages[i].sprite = heartEmpty;
            }
        }
    }

    public void Attack()
    {
        if (bossCurrentHearts <= 0) return;

        if (sfxAttack != null && sfxSource != null)
        {
            sfxSource.pitch = Random.Range(0.85f, 1.25f);
            sfxSource.PlayOneShot(sfxAttack);
            sfxSource.pitch = 1f;
        }

        bossCurrentHP--;

        if (bossCurrentHP <= 0)
        {
            bossCurrentHearts--;
            bossCurrentHP = bossHeartHP;

            dialogueText.text = $"* Phase {BossPhase} !";
            PlayDialogueEffect();

            if (mainCamera != null)
                mainCamera.transform.DOShakePosition(0.3f, 0.3f);
        }
        else
        {
            dialogueText.text = "* Tu enlèves 1/4 de cœur !";
            PlayDialogueEffect();
        }

        // Mise à jour de l'alpha de l'image du boss à chaque coup
        UpdateBossImageAlpha();

        if (bossCurrentHearts <= 0)
        {
            dialogueText.text = "*Chronos est vaincu !";
            // Optionnel : désactiver le raycast ou l'objet une fois invisible
            if (bossImage != null)
                bossImage.raycastTarget = false;

            // Arrêter le mouvement et nettoyer les traînées
            StopBossMovementAndClearTrails();
        }

        UpdateUI();
    }

    public void Heal()
    {
        int oldHP = playerHP;
        int healAmount = playerMaxHP - playerHP;

        if (healAmount <= 0)
        {
            dialogueText.text = "* Tu es déjà à pleine santé.";
            PlayDialogueEffect();
            return;
        }

        playerHP = playerMaxHP;

        // Effet visuel optimisé
        playerHPBar.DOColor(colorGreen, 0.15f)
            .OnComplete(() => playerHPBar.DOColor(colorWhite, SHAKE_DURATION));

        if (playerHPBarRect != null)
        {
            playerHPBarRect
                .DOScale(1.15f, SCALE_DURATION)
                .SetEase(Ease.OutBack)
                .OnPlay(() =>
                {
                    if (sfxSource != null && sfxHeal != null)
                        sfxSource.PlayOneShot(sfxHeal);
                })
                .OnComplete(() => playerHPBarRect.DOScale(1f, 0.1f));
        }

        if (hpAnimCoroutine != null)
            StopCoroutine(hpAnimCoroutine);

        hpAnimCoroutine = StartCoroutine(AnimateHPBarAndText(oldHP, playerHP));

        if (playerHPTextRect != null)
        {
            playerHPTextRect
                .DOScale(1.3f, SCALE_DURATION)
                .SetEase(Ease.OutBack)
                .OnComplete(() => playerHPTextRect.DOScale(1f, 0.1f));
        }

        UpdateHPBar();
        UpdatePlayerHPText();
        UpdateUI();

        dialogueText.text = $"* Tu te soignes de {healAmount} PV !";
    }

    public void OnJewelCollected()
    {
        isPausedForJewel = true;

        ChronosAttackController attackController = FindFirstObjectByType<ChronosAttackController>();
        if (attackController != null)
            attackController.enabled = false;

        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerSoul");
        if (playerObj != null)
        {
            PlayerSoul playerSoul = playerObj.GetComponent<PlayerSoul>();
            if (playerSoul != null)
            {
                playerSoul.ExitJusticeMode();
                playerSoul.SetMovementEnabled(true);
            }
        }

        dialogueText.text = "* Un joyau ! Choisis : Attaquer ou Te soigner.";

        if (gamepadCursor != null)
            gamepadCursor.SetActive(true);
    }

    public void ChooseAttack()
    {
        Attack();
        UnlockPlayerMovement();

        if (gamepadCursor != null)
            gamepadCursor.SetActive(false);

        ResumeAttacks();
    }

    public void ChooseHeal()
    {
        Heal();
        UnlockPlayerMovement();

        if (gamepadCursor != null)
            gamepadCursor.SetActive(false);

        ResumeAttacks();
    }

    private void UnlockPlayerMovement()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerSoul");
        if (playerObj != null)
        {
            PlayerSoul playerSoul = playerObj.GetComponent<PlayerSoul>();
            if (playerSoul != null)
            {
                playerSoul.ExitJusticeMode();
                playerSoul.SetMovementEnabled(true);
            }

            JusticeShieldController shieldCtrl = playerObj.GetComponent<JusticeShieldController>();
            if (shieldCtrl != null)
                shieldCtrl.DeactivateShields();
        }

        if (gamepadCursor != null)
            gamepadCursor.SetActive(false);
    }

    private void ResumeAttacks()
    {
        isPausedForJewel = false;

        ChronosAttackController attackController = FindFirstObjectByType<ChronosAttackController>();
        if (attackController != null)
            attackController.enabled = true;

        dialogueText.text = "* Les attaques reprennent !";
    }

    private Sprite GetHeartSprite(int value)
    {
        return value switch
        {
            4 => heartFull,
            3 => heartThreeQuarters,
            2 => heartHalf,
            1 => heartQuarter,
            _ => heartEmpty,
        };
    }

    public void UpdateUI()
    {
        playerHPBar.fillAmount = (float)playerHP / playerMaxHP;
        UpdatePlayerHPText();

        // Mise à jour des cœurs du boss
        for (int i = 0; i < bossMaxHearts; i++)
        {
            if (i < bossCurrentHearts - 1)
            {
                bossHeartImages[i].sprite = heartFull;
                StartHeartRotation(i);
            }
            else if (i == bossCurrentHearts - 1)
            {
                bossHeartImages[i].sprite = GetHeartSprite(bossCurrentHP);

                RectTransform heartRect = bossHeartImages[i].rectTransform;
                heartRect
                    .DOScale(1.3f, 0.1f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => heartRect.DOScale(1f, 0.1f));

                heartRect.DOShakeRotation(SHAKE_DURATION, 15f);
                StopHeartRotation(i);
            }
            else
            {
                bossHeartImages[i].sprite = heartEmpty;
                StopHeartRotation(i);
            }
        }
    }

    void StartHeartRotation(int index)
    {
        if (heartRotateTweens[index] != null && heartRotateTweens[index].IsActive())
            return;

        RectTransform rt = bossHeartImages[index].rectTransform;

        heartRotateTweens[index] = rt
            .DORotate(new Vector3(0, 0, 180f), 5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(2f);
    }

    void StopHeartRotation(int index)
    {
        if (heartRotateTweens[index] != null)
        {
            heartRotateTweens[index].Kill();
            heartRotateTweens[index] = null;
        }

        bossHeartImages[index].rectTransform.rotation = Quaternion.identity;
    }

    void PlayPlayerDamageEffects()
    {
        if (playerHPBarRect != null)
        {
            playerHPBarRect
                .DOShakePosition(SHAKE_DURATION, new Vector3(10f, 0, 0), 15)
                .OnPlay(() =>
                {
                    if (sfxSource != null && sfxDamage != null)
                        sfxSource.PlayOneShot(sfxDamage);
                });
        }

        playerHPBar.DOColor(colorRed, 0.1f)
            .OnComplete(() => playerHPBar.DOColor(colorWhite, 0.15f));

        if (playerHPTextRect != null)
        {
            playerHPTextRect.DOShakePosition(0.3f, 8f, 20);
        }
    }

    void PlayDialogueEffect()
    {
        if (dialogueTextRect == null) return;

        dialogueTextRect.DOKill();
        dialogueTextRect.localScale = Vector3.one * 0.95f;

        dialogueTextRect
            .DOScale(1f, 0.15f)
            .SetEase(Ease.OutBack)
            .OnPlay(() =>
            {
                if (sfxSource != null && sfxDamage != null)
                    sfxSource.PlayOneShot(sfxDamage, 0.3f);
            });
    }

    private Sprite GetHPBarSprite(int currentHP)
    {
        float ratio = (float)currentHP / playerMaxHP;

        if (ratio >= 0.75f)
            return hpFull;
        else if (ratio >= 0.5f)
            return hpHalf;
        else if (ratio > 0f)
            return hpLow;
        else
            return hpEmpty;
    }

    private void PlayHPBarEffect(bool isDamage)
    {
        if (playerHPBarRect == null) return;

        playerHPBarRect.DOKill();

        if (isDamage)
        {
            playerHPBarRect.DOShakePosition(SHAKE_DURATION, new Vector3(10f, 0, 0), 15);

            if (sfxSource != null && sfxDamage != null)
                sfxSource.PlayOneShot(sfxDamage);
        }
        else
        {
            playerHPBarRect
                .DOScale(1.15f, SCALE_DURATION)
                .SetEase(Ease.OutBack)
                .OnPlay(() =>
                {
                    if (sfxSource != null && sfxHeal != null)
                        sfxSource.PlayOneShot(sfxHeal);
                })
                .OnComplete(() => playerHPBarRect.DOScale(1f, 0.1f));
        }
    }

    private void UpdateHPBar()
    {
        playerHPBar.sprite = GetHPBarSprite(playerHP);

        float targetFill = (float)playerHP / playerMaxHP;
        playerHPBar.DOFillAmount(targetFill, 0.4f);
    }

    private void UpdatePlayerHPText(int hp = -1)
    {
        if (playerHPText != null)
            playerHPText.text = $"{(hp < 0 ? playerHP : hp)} / {playerMaxHP}";
    }

    // -----------------------------
    // Nouvelle logique pour l'alpha du boss
    // -----------------------------
    private int TotalBossHP => bossMaxHearts * bossHeartHP;

    private int GetRemainingBossHP()
    {
        if (bossCurrentHearts <= 0) return 0;
        return (bossCurrentHearts - 1) * bossHeartHP + bossCurrentHP;
    }

    private void UpdateBossImageAlpha(bool instant = false)
    {
        if (bossImage == null) return;

        float alpha = (float)GetRemainingBossHP() / Mathf.Max(1, TotalBossHP);
        alpha = Mathf.Clamp01(alpha);

        if (instant)
        {
            Color c = bossImage.color;
            bossImage.color = new Color(c.r, c.g, c.b, alpha);
        }
        else
        {
            bossImage.DOFade(alpha, 0.25f);
        }

        // Si plus de PV, s'assurer que l'image est désactivée/complètement transparente
        if (GetRemainingBossHP() <= 0 && bossImage != null)
        {
            // Garder l'objet actif si vous voulez d'autres animations, sinon désactivez-le:
            // bossImage.gameObject.SetActive(false);
            bossImage.raycastTarget = false;
        }
    }

    // -----------------------------
    // Mouvement infini + rémanence
    // -----------------------------
    private void InitializeTrailPool()
    {
        if (bossImage == null) return;

        // Créer le pool d'Images en tant qu'enfants du même parent que bossImage
        Transform parent = bossImage.transform.parent;
        trailPool = new Image[Mathf.Max(1, trailPoolSize)];
        for (int i = 0; i < trailPool.Length; i++)
        {
            GameObject go = new GameObject($"BossTrail_{i}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            // Copier la taille et sprite du boss
            img.rectTransform.sizeDelta = bossImage.rectTransform.sizeDelta;
            img.sprite = bossImage.sprite;
            img.color = new Color(1f, 1f, 1f, 0f);
            go.SetActive(false);
            trailPool[i] = img;
        }
        trailIndex = 0;
    }

    private void AnimateBossInfinity()
    {
        // Lissajous / figure-8 approximatif : x = A*sin(t), y = B*sin(2t)/2
        infinityT += Time.deltaTime * infinitySpeed;
        float x = infinityRadiusX * Mathf.Sin(infinityT);
        float y = infinityRadiusY * (Mathf.Sin(infinityT * 2f) * 0.5f);
        Vector2 newPos = bossCenterAnchored + new Vector2(x, y);
        bossRect.anchoredPosition = newPos;

        // Optionnel : légère rotation pour dynamique
        bossRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(infinityT) * 6f);

        // Spawn rémanence
        trailTimer += Time.deltaTime;
        if (trailTimer >= trailSpawnInterval)
        {
            SpawnTrailAt(newPos);
            trailTimer = 0f;
        }
    }

    private void SpawnTrailAt(Vector2 anchoredPos)
    {
        if (trailPool == null || trailPool.Length == 0 || bossImage == null) return;

        Image t = trailPool[trailIndex];
        trailIndex = (trailIndex + 1) % trailPool.Length;

        if (t == null) return;

        // Préparer l'image du trail
        t.sprite = bossImage.sprite;
        t.gameObject.SetActive(true);
        RectTransform tr = t.rectTransform;
        tr.anchoredPosition = anchoredPos;
        tr.localScale = bossRect.localScale * trailScaleMultiplier;

        // Copier couleur actuelle du boss (avec alpha)
        Color baseColor = bossImage.color;
        baseColor.a = Mathf.Clamp01(baseColor.a); // s'assurer
        t.color = baseColor;

        // Kill tweens précédents
        t.DOKill();

        // Animation : fondu vers 0 + déplacement léger
        Vector2 jitter = new Vector2(Random.Range(-8f, 8f), Random.Range(-8f, 8f));
        tr.DOAnchorPos(anchoredPos + jitter, trailFadeDuration).SetEase(Ease.OutQuad);
        t.DOFade(0f, trailFadeDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            if (t != null)
            {
                t.gameObject.SetActive(false);
                // remettre alpha à 0 pour sécurité
                Color c = t.color;
                t.color = new Color(c.r, c.g, c.b, 0f);
            }
        });
    }

    private void StopBossMovementAndClearTrails()
    {
        enableBossInfinityMovement = false;

        if (bossImage != null)
        {
            bossImage.DOKill();
            bossImage.DOFade(0f, 0.5f);
        }

        if (trailPool != null)
        {
            foreach (var t in trailPool)
            {
                if (t == null) continue;
                t.DOKill();
                t.gameObject.SetActive(false);
            }
        }
    }
}