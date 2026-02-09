using System.Collections.Generic;
using TMPro; // Ajoutez ceci en haut du fichier
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameCardPanelManager : MonoBehaviour
{
    [Header("Source de cartes")]
    [SerializeField] private MiniGameCardCollectionSO cardCollection;
    public MiniGameCardCollectionSO CardCollection => cardCollection;

    [Header("Boutons de cartes dans le panel")]
    [SerializeField] private MiniGameCardButton[] cardButtons;

    [Header("Config")]
    [SerializeField] private int cardsToDraw = 3;

    [Header("UI")]
    [SerializeField] private TMP_Text rerollText; // Ajoutez ce champ
    [SerializeField] private Button rerollButton; // Nouveau : bouton de reroll sur le panel

    // Cartes déjà utilisées (pour la journée / session en cours)
    private readonly HashSet<MiniGameCardEffectSO> _usedCards = new HashSet<MiniGameCardEffectSO>();

    private void OnEnable()
    {
        // Liaison boutons (sécurisée)
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(RerollMiniGameCards);
        }

        RandomizeCards();
        UpdateRerollText(); // Ajoutez cet appel pour l'init
    }

    public void RandomizeCards()
    {
        if (cardCollection == null || cardButtons == null || cardButtons.Length == 0)
        {
            Debug.LogWarning("[MiniGameCardPanelManager] cardCollection ou cardButtons non assignés.");
            UpdateRerollText();
            return;
        }

        // Pool de cartes disponibles = toutes les cartes - celles déjà utilisées
        List<MiniGameCardEffectSO> pool = new List<MiniGameCardEffectSO>();
        foreach (var c in cardCollection.allMiniGameCards)
        {
            if (c != null && !_usedCards.Contains(c))
                pool.Add(c);
        }

        // Si plus aucune carte dispo, réinitialiser le pool (comme pour les villages)
        if (pool.Count == 0)
        {
            Debug.Log("[MiniGameCardPanelManager] Plus de cartes disponibles, réinitialisation du pool.");
            _usedCards.Clear();
            pool.AddRange(cardCollection.allMiniGameCards);
        }

        // Tirage aléatoire
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (i < cardsToDraw && pool.Count > 0)
            {
                int index = Random.Range(0, pool.Count);
                var card = pool[index];
                pool.RemoveAt(index);

                cardButtons[i].gameObject.SetActive(true);
                cardButtons[i].SetCard(card, this);
            }
            else
            {
                cardButtons[i].gameObject.SetActive(false);
            }
        }

        // Mise à jour de l'UI reroll / état du bouton
        UpdateRerollText();
    }

    /// <summary>
    /// Marque une carte comme utilisée (empêche réapparition durant la session)
    /// </summary>
    public void MarkCardUsed(MiniGameCardEffectSO card)
    {
        if (card != null)
            _usedCards.Add(card);

        // Optionnel : mettre à jour l'affichage du reroll (utile si vous voulez montrer qu'une carte a été choisie)
        UpdateRerollText();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        UIManager.Instance?.GameModeChoice();
        GameManager.Instance?.EndHalfDay();
    }

    public void RerollMiniGameCards()
    {
        // Optionnel : limite de rerolls par jour
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MiniGameCardPanelManager] GameManager.Instance est null.");
            return;
        }

        if (GameManager.Instance.RerollsRemaining <= 0)
        {
            Debug.LogWarning("[MiniGameCardPanelManager] Pas de rerolls restants !");
            // Désactiver bouton pour éviter spam
            if (rerollButton != null) rerollButton.interactable = false;
            UpdateRerollText();
            return;
        }

        GameManager.Instance.RerollsRemaining--;
        RandomizeCards();
        UpdateRerollText(); // Ajoutez cet appel
    }

    private void UpdateRerollText()
    {
        if (rerollText != null)
        {
            int remaining = GameManager.Instance != null ? GameManager.Instance.RerollsRemaining : 0;
            rerollText.SetText($"Rerolls : {remaining}");
        }

        if (rerollButton != null)
        {
            bool enabled = GameManager.Instance != null && GameManager.Instance.RerollsRemaining > 0;
            rerollButton.interactable = enabled;
        }
    }
}