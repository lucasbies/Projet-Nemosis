using UnityEngine;

public enum MiniGameType
{
    Any,
    Rhythm,
    Chyron,
    Zeus,
    NuitGlaciale,
    Tri
}

[CreateAssetMenu(fileName = "MiniGameCardEffect", menuName = "Card/MiniGameEffect")]
public class MiniGameCardEffectSO : ScriptableObject
{
    [Header("Infos UI")]
    public string cardName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cible")]
    public MiniGameType targetMiniGame = MiniGameType.Any;

    [Header("Paramètres génériques")]
    [Tooltip("Vitesse globale : tempo, spawn, etc.")]
    public float speedMultiplier = 1f;      

    [Tooltip("Pression / récompenses globales")]
    public float difficultyMultiplier = 1f; 

    [Tooltip("Inversion des contrôles (surtout Rhythm)")]
    public bool invertControls = false;

    [Header("Spawn / densité")]
    [Tooltip("Multiplier de quantité d'entités (notes, âmes, maisons, éclairs...)")]
    public float spawnRateMultiplier = 1f;

    [Tooltip("Ajoute un chaos aléatoire au spawn (0 = normal, 1 = très chaotique)")]
    [Range(0f, 1f)] public float chaosLevel = 0f;

    [Header("Récompenses globales")]
    [Tooltip("Bonus plat sur les gains de stats (appliqué par les mini-jeux qui le supportent)")]
    public float rewardFlatBonus = 0f;

    [Tooltip("Multiplicateur sur les gains de stats (Or/Foi/Food/Humain...)")]
    public float rewardMultiplier = 1f;

    [Header("Effets spéciaux")]
    [Tooltip("Plus d'ennemis / entités hostiles")]
    public bool moreEnemies = false;

    [Tooltip("Moins d'ennemis / entités hostiles")]
    public bool lessEnemies = false;

    [Tooltip("Mode 'chaos visuel' : peut être utilisé pour effets FX, caméra, etc.")]
    public bool visualChaos = false;

    [Tooltip("Mode 'One Chance' : le mini-jeu peut décider de punir la moindre erreur")]
    public bool oneMistakeFail = false;
}