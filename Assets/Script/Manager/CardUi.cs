using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class CardUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button actionButton;

    public VillageCard currentCard;

    public static CardUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void Setup(VillageCard Card)
    {
        currentCard = Card;

        iconImage.sprite = Card.icon;
        nameText.text = Card.cardName;
        descriptionText.text = Card.description;

    }

    public void OnActionButtonCClickedAnimation()
    {
        if (currentCard == null)
        {
            Debug.LogError("[CardUI] currentCard est null! Setup() n'a pas été appelé.");
            return;
        }
        
        if (DOTweenManager.Instance.IsAnimating == false)
        {
            StartCoroutine(DOTweenManager.Instance.OnActionCardAnimation(gameObject.transform, currentCard));
           
        }

        
    }

    public void OnActionCard()
    {
        if (currentCard != null)
        {
            currentCard.PlayCard();
        }
    }

    public void AfterCard()
    {
        if (currentCard != null)
        {
            UIManager.Instance.closeInteractionMenu();
            GameManager.Instance.EndHalfDay();
        }
    }
    
}
