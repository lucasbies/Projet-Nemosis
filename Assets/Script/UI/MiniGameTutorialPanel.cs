using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using DG.Tweening;
using UnityEngine.EventSystems;

public class MiniGameTutorialPanel : MonoBehaviour
{
    [Header("Main UI")]
    public TMP_Text titleText;
    public Button continueButton;
    public Button backButton;
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;

    [Header("Animation Targets")]
    public CanvasGroup backgroundOverlay;
    public RectTransform contentContainer;
    public RectTransform videoSection;
    public RectTransform controlsSection;

    [Header("Device Toggle")]
    public Button keyboardToggleButton;
    public Button gamepadToggleButton;
    public GameObject keyboardControlsPanel;
    public GameObject gamepadControlsPanel;

    [Header("Device Toggle Colors")]
    public Color keyboardActiveColor = new Color(0.58f, 0.20f, 0.92f, 1f);
    public Color gamepadActiveColor = new Color(0.86f, 0.15f, 0.47f, 1f);
    public Color inactiveDeviceColor = new Color(0.22f, 0.25f, 0.32f, 1f);

    [Header("Rebind UI - Clavier")]
    public TMP_Text[] laneLabelsKeyboard;
    public TMP_Text[] currentKeyLabelsKeyboard;
    public Button[] rebindLaneButtonsKeyboard;
    public Image[] laneIconsKeyboard;

    [Header("Rebind UI - Manette")]
    public TMP_Text[] laneLabelsGamepad;
    public TMP_Text[] currentKeyLabelsGamepad;
    public Button[] rebindLaneButtonsGamepad;
    public Image[] laneIconsGamepad;

    [Header("Tips Section")]
    public GameObject tipsSection;
    public TMP_Text tipText;

    [Header("Lane Colors")]
    public Color lane0Color = new Color(0.23f, 0.51f, 0.96f, 1f);
    public Color lane1Color = new Color(0.13f, 0.77f, 0.37f, 1f);
    public Color lane2Color = new Color(0.98f, 0.80f, 0.13f, 1f);
    public Color lane3Color = new Color(0.86f, 0.15f, 0.15f, 1f);

    private InputAction[] actionsKeyboard;
    private InputAction[] actionsGamepad;
    private bool isAnimating = false;

    [Header("Tutorial")]
    public MiniGameTutorialPanel tutorialPanel;
    public VideoClip tutorialClip;

    [Header("Animation Parameters")]
    public float backgroundFadeDuration = 0.35f;
    public float containerPopDuration = 0.45f;
    public float sectionSlideDuration = 0.38f;
    public float controlsSwitchDuration = 0.28f;
    public float continuePulseScale = 1.05f;
    public float continuePulseDuration = 0.9f;
    public Vector2 sectionSlideOffset = new Vector2(300f, 0f);
    public Ease popEase = Ease.OutBack;
    public Ease slideEase = Ease.OutCubic;

    private Vector2 videoOriginalAnchoredPos;
    private Vector2 controlsOriginalAnchoredPos;
    private Vector3 contentOriginalScale;

    private void Awake()
    {
        if (keyboardToggleButton != null)
            keyboardToggleButton.onClick.AddListener(() => ShowDeviceControls(true));

        if (gamepadToggleButton != null)
            gamepadToggleButton.onClick.AddListener(() => ShowDeviceControls(false));

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (backButton != null)
            backButton.onClick.AddListener(Hide);

        SetupLaneIconColors();

        if (videoSection != null) videoOriginalAnchoredPos = videoSection.anchoredPosition;
        if (controlsSection != null) controlsOriginalAnchoredPos = controlsSection.anchoredPosition;
        if (contentContainer != null) contentOriginalScale = contentContainer.localScale;
    }

    // ==================== MÉTHODES PUBLIQUES ====================

    /// <summary>
    /// Version standard pour jeux avec 4 lanes (Rhythm)
    /// </summary>
    public void Show(
        string miniGameName,
        InputAction[] actionsKeyboard,
        InputAction[] actionsGamepad,
        VideoClip tutorialClip = null,
        string tip = null)
    {
        this.actionsKeyboard = actionsKeyboard;
        this.actionsGamepad = actionsGamepad;

        titleText.text = $"Contrôles : {miniGameName}";

        // Réafficher toutes les lanes
        ShowAllLanes();
        UpdateControlBindings();
        SetupVideo(tutorialClip);
        SetupTip(tip);

        ShowDeviceControls(true);
        gameObject.SetActive(true);
        AnimateIn();
    }

    /// <summary>
    /// Version spéciale pour Chyron (seulement 2 contrôles: Gauche/Droite)
    /// </summary>
    public void ShowChyron(
        string miniGameName,
        InputAction[] actionsKeyboard,
        InputAction[] actionsGamepad,
        VideoClip tutorialClip = null,
        string tip = null)
    {
        this.actionsKeyboard = actionsKeyboard;
        this.actionsGamepad = actionsGamepad;

        titleText.text = $"Contrôles : {miniGameName}";

        // Mettre à jour seulement les 2 premières lanes
        UpdateChyronControlBindings();
        SetupVideo(tutorialClip);
        SetupTip(tip);

        // Cacher les lanes 2 et 3
        HideUnusedLanes();

        ShowDeviceControls(true);
        gameObject.SetActive(true);
        AnimateIn();
    }

    /// <summary>
    /// Version simplifiée pour les jeux sans rebind (comme click)
    /// Cache les contrôles et affiche seulement la vidéo + tip
    /// </summary>
    public void ShowSimple(string miniGameName, VideoClip tutorialClip = null, string tip = null)
    {
        titleText.text = $"Contrôles : {miniGameName}";

        // Cacher complètement les sections de contrôles
        if (keyboardControlsPanel != null) keyboardControlsPanel.SetActive(false);
        if (gamepadControlsPanel != null) gamepadControlsPanel.SetActive(false);
        if (keyboardToggleButton != null) keyboardToggleButton.gameObject.SetActive(false);
        if (gamepadToggleButton != null) gamepadToggleButton.gameObject.SetActive(false);

        SetupVideo(tutorialClip);
        SetupTip(tip);

        gameObject.SetActive(true);
        AnimateIn();
    }

    /// <summary>
    /// Version pour Chronos (combat au tour par tour, contrôles complexes)
    /// Affiche instructions textuelles au lieu de rebind
    /// </summary>
    public void ShowChronos(string miniGameName, VideoClip tutorialClip = null, string tip = null)
    {
        titleText.text = $"Contrôles : {miniGameName}";

        // Cacher les contrôles de rebind
        if (keyboardControlsPanel != null) keyboardControlsPanel.SetActive(false);
        if (gamepadControlsPanel != null) gamepadControlsPanel.SetActive(false);
        if (keyboardToggleButton != null) keyboardToggleButton.gameObject.SetActive(false);
        if (gamepadToggleButton != null) gamepadToggleButton.gameObject.SetActive(false);

        SetupVideo(tutorialClip);

        // Tip personnalisé avec instructions
        if (tipsSection != null)
        {
            tipsSection.SetActive(true);
            if (tipText != null)
            {
                tipText.text = tip ?? "ZQSD / Flèches : Se déplacer\nA / Bouton Sud : Choisir\nBoucliers : ZQSD pendant l'attaque Justice";
            }
        }

        gameObject.SetActive(true);
        AnimateIn();
    }

    /// <summary>
    /// Version pour PlayerSoul/Chronos avec instructions de mouvement
    /// </summary>
    public void ShowMovementOnly(string miniGameName, VideoClip tutorialClip = null, string tip = null)
    {
        titleText.text = $"Contrôles : {miniGameName}";

        // Cacher les contrôles
        if (keyboardControlsPanel != null) keyboardControlsPanel.SetActive(false);
        if (gamepadControlsPanel != null) gamepadControlsPanel.SetActive(false);
        if (keyboardToggleButton != null) keyboardToggleButton.gameObject.SetActive(false);
        if (gamepadToggleButton != null) gamepadToggleButton.gameObject.SetActive(false);

        SetupVideo(tutorialClip);

        if (tipsSection != null)
        {
            tipsSection.SetActive(true);
            if (tipText != null)
            {
                tipText.text = tip ?? "ZQSD / Stick Gauche : Se déplacer\nMode Justice : Mouvement limité à 4 directions";
            }
        }

        gameObject.SetActive(true);
        AnimateIn();
    }

    public void Hide()
    {
        if (isAnimating) return;

        AnimateOut(() =>
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
        });
    }

    // ==================== MÉTHODES PRIVÉES ====================

    private void SetupTip(string tip)
    {
        if (tipsSection != null)
        {
            if (!string.IsNullOrEmpty(tip))
            {
                tipsSection.SetActive(true);
                tipText.text = tip;
            }
            else
            {
                tipsSection.SetActive(false);
            }
        }
    }

    private void ShowDeviceControls(bool showKeyboard)
    {
        if (keyboardControlsPanel != null)
            keyboardControlsPanel.SetActive(showKeyboard);

        if (gamepadControlsPanel != null)
            gamepadControlsPanel.SetActive(!showKeyboard);

        UpdateToggleButtonColors(showKeyboard);
        AnimateControlsSwitch(showKeyboard);
    }

    private void UpdateToggleButtonColors(bool keyboardActive)
    {
        if (keyboardToggleButton != null)
        {
            var keyboardImage = keyboardToggleButton.GetComponent<Image>();
            if (keyboardImage != null)
                keyboardImage.color = keyboardActive ? keyboardActiveColor : inactiveDeviceColor;
        }

        if (gamepadToggleButton != null)
        {
            var gamepadImage = gamepadToggleButton.GetComponent<Image>();
            if (gamepadImage != null)
                gamepadImage.color = keyboardActive ? inactiveDeviceColor : gamepadActiveColor;
        }
    }

    private void UpdateControlBindings()
    {
        if (actionsKeyboard != null)
        {
            for (int i = 0; i < actionsKeyboard.Length && i < 4; i++)
            {
                if (i < laneLabelsKeyboard.Length && laneLabelsKeyboard[i] != null)
                    laneLabelsKeyboard[i].text = $"Lane {i}";

                if (i < currentKeyLabelsKeyboard.Length && currentKeyLabelsKeyboard[i] != null)
                {
                    string binding = GetBindingDisplayString(actionsKeyboard[i], "<Keyboard>");
                    currentKeyLabelsKeyboard[i].text = $"{binding}";
                }

                if (i < rebindLaneButtonsKeyboard.Length && rebindLaneButtonsKeyboard[i] != null)
                {
                    int idx = i;
                    rebindLaneButtonsKeyboard[i].onClick.RemoveAllListeners();
                    rebindLaneButtonsKeyboard[i].onClick.AddListener(() => StartRebind(actionsKeyboard[idx], idx, false));
                }
            }
        }

        if (actionsGamepad != null)
        {
            for (int i = 0; i < actionsGamepad.Length && i < 4; i++)
            {
                if (i < laneLabelsGamepad.Length && laneLabelsGamepad[i] != null)
                    laneLabelsGamepad[i].text = $"Lane {i}";

                if (i < currentKeyLabelsGamepad.Length && currentKeyLabelsGamepad[i] != null)
                {
                    string binding = GetBindingDisplayString(actionsGamepad[i], "<Gamepad>");
                    currentKeyLabelsGamepad[i].text = $"{binding}";
                }

                if (i < rebindLaneButtonsGamepad.Length && rebindLaneButtonsGamepad[i] != null)
                {
                    int idx = i;
                    rebindLaneButtonsGamepad[i].onClick.RemoveAllListeners();
                    rebindLaneButtonsGamepad[i].onClick.AddListener(() => StartRebind(actionsGamepad[idx], idx, true));
                }
            }
        }
    }

    private void UpdateChyronControlBindings()
    {
        if (actionsKeyboard != null && actionsKeyboard.Length >= 2)
        {
            // Lane 0 = Gauche
            if (laneLabelsKeyboard.Length > 0 && laneLabelsKeyboard[0] != null)
                laneLabelsKeyboard[0].text = "Gauche";

            if (currentKeyLabelsKeyboard.Length > 0 && currentKeyLabelsKeyboard[0] != null)
            {
                string binding = GetBindingDisplayString(actionsKeyboard[0], "<Keyboard>");
                currentKeyLabelsKeyboard[0].text = $"{binding}";
            }

            if (rebindLaneButtonsKeyboard.Length > 0 && rebindLaneButtonsKeyboard[0] != null)
            {
                rebindLaneButtonsKeyboard[0].onClick.RemoveAllListeners();
                rebindLaneButtonsKeyboard[0].onClick.AddListener(() => StartRebind(actionsKeyboard[0], 0, false));
            }

            // Lane 1 = Droite
            if (laneLabelsKeyboard.Length > 1 && laneLabelsKeyboard[1] != null)
                laneLabelsKeyboard[1].text = "Droite ";

            if (currentKeyLabelsKeyboard.Length > 1 && currentKeyLabelsKeyboard[1] != null)
            {
                string binding = GetBindingDisplayString(actionsKeyboard[1], "<Keyboard>");
                currentKeyLabelsKeyboard[1].text = $"{binding}";
            }

            if (rebindLaneButtonsKeyboard.Length > 1 && rebindLaneButtonsKeyboard[1] != null)
            {
                rebindLaneButtonsKeyboard[1].onClick.RemoveAllListeners();
                rebindLaneButtonsKeyboard[1].onClick.AddListener(() => StartRebind(actionsKeyboard[1], 1, false));
            }
        }

        if (actionsGamepad != null && actionsGamepad.Length >= 2)
        {
            // Lane 0 = Gauche
            if (laneLabelsGamepad.Length > 0 && laneLabelsGamepad[0] != null)
                laneLabelsGamepad[0].text = "Gauche";

            if (currentKeyLabelsGamepad.Length > 0 && currentKeyLabelsGamepad[0] != null)
            {
                string binding = GetBindingDisplayString(actionsGamepad[0], "<Gamepad>");
                currentKeyLabelsGamepad[0].text = $"{binding}";
            }

            if (rebindLaneButtonsGamepad.Length > 0 && rebindLaneButtonsGamepad[0] != null)
            {
                rebindLaneButtonsGamepad[0].onClick.RemoveAllListeners();
                rebindLaneButtonsGamepad[0].onClick.AddListener(() => StartRebind(actionsGamepad[0], 0, true));
            }

            // Lane 1 = Droite
            if (laneLabelsGamepad.Length > 1 && laneLabelsGamepad[1] != null)
                laneLabelsGamepad[1].text = "Droite";

            if (currentKeyLabelsGamepad.Length > 1 && currentKeyLabelsGamepad[1] != null)
            {
                string binding = GetBindingDisplayString(actionsGamepad[1], "<Gamepad>");
                currentKeyLabelsGamepad[1].text = $"{binding}";
            }

            if (rebindLaneButtonsGamepad.Length > 1 && rebindLaneButtonsGamepad[1] != null)
            {
                rebindLaneButtonsGamepad[1].onClick.RemoveAllListeners();
                rebindLaneButtonsGamepad[1].onClick.AddListener(() => StartRebind(actionsGamepad[1], 1, true));
            }
        }
    }

    private void HideUnusedLanes()
    {
        // Keyboard - cacher lanes 2 et 3
        for (int i = 2; i < 4; i++)
        {
            if (i < laneLabelsKeyboard.Length && laneLabelsKeyboard[i] != null && laneLabelsKeyboard[i].transform.parent != null)
                laneLabelsKeyboard[i].transform.parent.gameObject.SetActive(false);
        }

        // Gamepad - cacher lanes 2 et 3
        for (int i = 2; i < 4; i++)
        {
            if (i < laneLabelsGamepad.Length && laneLabelsGamepad[i] != null && laneLabelsGamepad[i].transform.parent != null)
                laneLabelsGamepad[i].transform.parent.gameObject.SetActive(false);
        }
    }

    private void ShowAllLanes()
    {
        // Keyboard - afficher toutes les lanes
        for (int i = 0; i < laneLabelsKeyboard.Length; i++)
        {
            if (laneLabelsKeyboard[i] != null && laneLabelsKeyboard[i].transform.parent != null)
                laneLabelsKeyboard[i].transform.parent.gameObject.SetActive(true);
        }

        // Gamepad - afficher toutes les lanes
        for (int i = 0; i < laneLabelsGamepad.Length; i++)
        {
            if (laneLabelsGamepad[i] != null && laneLabelsGamepad[i].transform.parent != null)
                laneLabelsGamepad[i].transform.parent.gameObject.SetActive(true);
        }
    }

    private void SetupLaneIconColors()
    {
        Color[] colors = { lane0Color, lane1Color, lane2Color, lane3Color };

        if (laneIconsKeyboard != null)
        {
            for (int i = 0; i < laneIconsKeyboard.Length && i < colors.Length; i++)
            {
                if (laneIconsKeyboard[i] != null)
                    laneIconsKeyboard[i].color = colors[i];
            }
        }

        if (laneIconsGamepad != null)
        {
            for (int i = 0; i < laneIconsGamepad.Length && i < colors.Length; i++)
            {
                if (laneIconsGamepad[i] != null)
                    laneIconsGamepad[i].color = colors[i];
            }
        }
    }

    private void SetupVideo(VideoClip tutorialClip)
    {
        if (videoPlayer != null)
        {
            if (tutorialClip != null)
            {
                videoPlayer.clip = tutorialClip;

                if (videoDisplay != null)
                {
                    videoPlayer.renderMode = VideoRenderMode.RenderTexture;

                    if (videoPlayer.targetTexture == null)
                    {
                        RenderTexture rt = new RenderTexture(1920, 1080, 0);
                        videoPlayer.targetTexture = rt;
                        videoDisplay.texture = rt;
                    }
                }

                videoPlayer.gameObject.SetActive(true);
                videoPlayer.isLooping = true;
                videoPlayer.Play();
            }
            else
            {
                videoPlayer.gameObject.SetActive(false);
            }
        }
    }

    private void StartRebind(InputAction action, int laneIndex, bool isGamepad)
    {
        TMP_Text currentKeyLabel = isGamepad
            ? (laneIndex < currentKeyLabelsGamepad.Length ? currentKeyLabelsGamepad[laneIndex] : null)
            : (laneIndex < currentKeyLabelsKeyboard.Length ? currentKeyLabelsKeyboard[laneIndex] : null);

        Button rebindButton = isGamepad
            ? (laneIndex < rebindLaneButtonsGamepad.Length ? rebindLaneButtonsGamepad[laneIndex] : null)
            : (laneIndex < rebindLaneButtonsKeyboard.Length ? rebindLaneButtonsKeyboard[laneIndex] : null);

        if (currentKeyLabel != null)
            currentKeyLabel.text = "...";

        if (rebindButton != null)
        {
            var buttonText = rebindButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = "...";
            rebindButton.interactable = false;
        }

        action.Disable();
        var rebinding = action.PerformInteractiveRebinding();

        if (isGamepad)
            rebinding.WithControlsExcluding("<Keyboard>/*");
        else
            rebinding.WithControlsExcluding("<Gamepad>/*");

        rebinding.WithControlsExcluding("<Mouse>/position");
        rebinding.WithControlsExcluding("<Mouse>/delta");

        rebinding.OnComplete(op =>
        {
            action.Enable();
            op.Dispose();

            string binding = GetBindingDisplayString(action, isGamepad ? "<Gamepad>" : "<Keyboard>");

            if (currentKeyLabel != null)
                currentKeyLabel.text = binding;

            if (rebindButton != null)
            {
                var buttonText = rebindButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null) buttonText.text = "Modifier";
                rebindButton.interactable = true;
            }

            if (InputManager.Instance != null)
                InputManager.Instance.SaveRebinds(isGamepad);
        }).Start();
    }

    private string GetBindingDisplayString(InputAction action, string deviceLayout)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isPartOfComposite) continue;

            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path)) path = binding.path;

            if (!path.Contains(deviceLayout)) continue;

            string displayString = null;
            try
            {
                displayString = action.GetBindingDisplayString(i);
            }
            catch { }

            if (string.IsNullOrEmpty(displayString))
            {
                try
                {
                    displayString = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }
                catch
                {
                    displayString = path;
                }
            }

            if (!string.IsNullOrEmpty(displayString))
            {
                displayString = displayString.Replace("Keyboard/", "").Replace("Gamepad/", "").Trim();
                return displayString;
            }
        }
        return "Non assigné";
    }

    // ==================== ANIMATIONS ====================

    private void AnimateIn()
    {
        isAnimating = true;

        if (backgroundOverlay != null)
            backgroundOverlay.alpha = 0;

        if (contentContainer != null)
        {
            contentContainer.localScale = Vector3.zero;
            contentContainer.rotation = Quaternion.Euler(0, 0, 5);
        }

        Sequence seq = DOTween.Sequence();

        if (backgroundOverlay != null)
            seq.Append(backgroundOverlay.DOFade(1, backgroundFadeDuration).SetEase(Ease.OutQuad));

        if (contentContainer != null)
        {
            seq.Append(contentContainer.DOScale(1, containerPopDuration).SetEase(popEase));
            seq.Join(contentContainer.DORotate(Vector3.zero, containerPopDuration).SetEase(popEase));
        }

        if (videoSection != null)
        {
            videoSection.anchoredPosition = videoOriginalAnchoredPos - sectionSlideOffset;
            seq.Append(videoSection.DOAnchorPos(videoOriginalAnchoredPos, sectionSlideDuration).SetEase(slideEase));
        }

        if (controlsSection != null)
        {
            controlsSection.anchoredPosition = controlsOriginalAnchoredPos + sectionSlideOffset;
            seq.Join(controlsSection.DOAnchorPos(controlsOriginalAnchoredPos, sectionSlideDuration).SetEase(slideEase));
        }

        seq.OnComplete(() =>
        {
            isAnimating = false;
            if (continueButton != null)
            {
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
                continueButton.transform.DOScale(continuePulseScale, continuePulseDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        });
    }

    private void AnimateOut(System.Action onComplete = null)
    {
        isAnimating = true;

        if (continueButton != null)
        {
            DOTween.Kill(continueButton.transform);
            continueButton.transform.localScale = Vector3.one;
        }

        Sequence seq = DOTween.Sequence();

        if (videoSection != null)
            seq.Append(videoSection.DOAnchorPos(videoOriginalAnchoredPos - sectionSlideOffset, sectionSlideDuration * 0.8f).SetEase(Ease.InCubic));

        if (controlsSection != null)
            seq.Join(controlsSection.DOAnchorPos(controlsOriginalAnchoredPos + sectionSlideOffset, sectionSlideDuration * 0.8f).SetEase(Ease.InCubic));

        if (contentContainer != null)
        {
            seq.Append(contentContainer.DOScale(0.8f, containerPopDuration * 0.7f).SetEase(Ease.InBack));
            seq.Join(contentContainer.DORotate(new Vector3(0, 0, -5), containerPopDuration * 0.7f).SetEase(Ease.InBack));
        }

        if (backgroundOverlay != null)
            seq.Append(backgroundOverlay.DOFade(0, backgroundFadeDuration * 0.6f).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            isAnimating = false;
            onComplete?.Invoke();
        });
    }

    private void AnimateControlsSwitch(bool toKeyboard)
    {
        GameObject panelToShow = toKeyboard ? keyboardControlsPanel : gamepadControlsPanel;
        GameObject panelToHide = toKeyboard ? gamepadControlsPanel : keyboardControlsPanel;

        if (panelToHide != null)
        {
            CanvasGroup cgHide = panelToHide.GetComponent<CanvasGroup>();
            if (cgHide == null) cgHide = panelToHide.AddComponent<CanvasGroup>();
            cgHide.DOFade(0, controlsSwitchDuration).OnComplete(() => panelToHide.SetActive(false));
        }

        if (panelToShow != null)
        {
            CanvasGroup cgShow = panelToShow.GetComponent<CanvasGroup>();
            if (cgShow == null) cgShow = panelToShow.AddComponent<CanvasGroup>();
            cgShow.alpha = 0;
            panelToShow.SetActive(true);
            cgShow.DOFade(1, controlsSwitchDuration);

            RectTransform rt = panelToShow.GetComponent<RectTransform>();
            Vector2 originalPos = rt.anchoredPosition;
            rt.anchoredPosition = originalPos + new Vector2(50, 0);
            rt.DOAnchorPos(originalPos, controlsSwitchDuration).SetEase(slideEase);
        }
    }

    private void OnContinueClicked()
    {
        Hide();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        if (continueButton != null)
            DOTween.Kill(continueButton.transform);
    }
}