using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniGameCardButton : MonoBehaviour
{
    public MiniGameCardEffectSO cardData;

    [Header("UI")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    private Button _button;
    private MiniGameCardPanelManager _panelManager;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnClickAnimation);

        _panelManager = GetComponentInParent<MiniGameCardPanelManager>();
    }

    private void Start()
    {
        if (cardData != null)
            RefreshUI();
    }

    // => on passe maintenant aussi le panelManager explicitement
    public void SetCard(MiniGameCardEffectSO newCard, MiniGameCardPanelManager panel)
    {
        cardData = newCard;
        _panelManager = panel;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (cardData == null) return;

        if (iconImage != null) iconImage.sprite = cardData.icon;
        if (nameText != null) nameText.text = cardData.cardName;
        if (descriptionText != null) descriptionText.text = cardData.description;
    }

    private void OnClickAnimation()
    {
        if (DOTweenManager.Instance.IsAnimating == false)
        {
            StartCoroutine(DOTweenManager.Instance.OnActionCardMiniJeuAnimation(gameObject.transform, AfterOnClick));
        }
    }

    private void AfterOnClick()
    {
        Debug.Log($"[MiniGameCardButton] OnClick sur {cardData?.cardName}");

        if (cardData == null) return;

        // 1) Sauvegarder la carte choisie pour le prochain mini-jeu
        if (MiniGameCardRuntime.Instance != null)
            MiniGameCardRuntime.Instance.SelectCard(cardData);

        // 2) NE PLUS SUPPRIMER de la collection, juste marquer comme utilis�e
        if (_panelManager != null)
        {
            _panelManager.MarkCardUsed(cardData);
        }

        // 3) Avancer le temps (Matin -> Aprem, Aprem -> jour suivant)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndHalfDay();
        }

        // 4) Fermer le panel mini-jeu et revenir au ModeChoiceUI
        if (UIManager.Instance != null)
            UIManager.Instance.CloseMiniJeuCardPanelAndBackToModeChoice();
    }
}