using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        // Affichage SIMPLE : seulement la description narrative
        if (descriptionText != null)
            descriptionText.text = cardData.description;
    }

    private void OnClickAnimation()
    {
        // Vérifier que DOTweenManager existe ET que l'animation n'est pas en cours
        if (DOTweenManager.Instance == null)
        {
            Debug.LogError("[MiniGameCardButton] DOTweenManager.Instance est null!");
            DirectExecute(); // Fallback : exécuter directement
            return;
        }

        if (DOTweenManager.Instance.IsAnimating)
        {
            Debug.LogWarning("[MiniGameCardButton] Animation déjà en cours, clic ignoré.");
            return;
        }

        if (cardData != null && MiniGameCardRuntime.Instance != null)
        {
            MiniGameCardRuntime.Instance.SelectCard(cardData);
            Debug.Log($"[MiniGameCardButton] Carte '{cardData.cardName}' sélectionnée et stockée.");
        }

        if (_panelManager != null)
            _panelManager.MarkCardUsed(cardData);

        if (DOTweenManager.Instance != null)
        {
            DOTweenManager.Instance.StartCoroutine(PlayFullMiniGameCardSequence());
        }
    }

    private IEnumerator PlayFullMiniGameCardSequence()
    {
        yield return DOTweenManager.Instance.StartCoroutine(
            DOTweenManager.Instance.AnimationCardMiniJeuSimple(transform, null)
        );

        Debug.Log("[MiniGameCardButton] Animation de carte terminée.");

        if (_panelManager != null && _panelManager.gameObject.activeInHierarchy)
        {
            _panelManager.gameObject.SetActive(false);
        }

        yield return DOTweenManager.Instance.StartCoroutine(
            DOTweenManager.Instance.transitionChoixJeu(AfterAllAnimations, endday: true)
        );
    }

    private void AfterAllAnimations()
    {
        Debug.Log($"[MiniGameCardButton] Toutes animations terminées pour {cardData?.cardName}");


        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseMiniJeuCardPanelAndBackToModeChoice();
        }
    }

    private void DirectExecute()
    {
        if (cardData != null && MiniGameCardRuntime.Instance != null)
        {
            MiniGameCardRuntime.Instance.SelectCard(cardData);
        }

        if (_panelManager != null)
        {
            _panelManager.MarkCardUsed(cardData);
            _panelManager.gameObject.SetActive(false);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.EndHalfDay();

        if (UIManager.Instance != null)
            UIManager.Instance.CloseMiniJeuCardPanelAndBackToModeChoice();
    }
}