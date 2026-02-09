using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scènes")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string optionsSceneName = "Menu_option";

    private const string SAVE_KEY = "GAME_SAVE_v1";

    [Header("UI")]
    [SerializeField] private Button continueButton;

    [SerializeField] private GameObject firstSelected;



    private void OnEnable()
    {
        if (EventSystem.current == null || firstSelected == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
    private void Start()
    {
        // Désactiver le bouton "Continuer" s'il n'y a pas de sauvegarde
        bool hasSave = PlayerPrefs.HasKey(SAVE_KEY);
        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
            // ou continueButton.gameObject.SetActive(hasSave); si tu veux complètement le cacher
        }
    }

    //Lance une nouvelle partie
    public void Play()
    {
        // supprimer l'ancienne sauvegarde de partie
        PlayerPrefs.DeleteKey("GAME_SAVE_v1");

        // flag pour dire "nouvelle partie"
        PlayerPrefs.SetInt("NEW_GAME_REQUESTED", 1);
        PlayerPrefs.Save();

        // Détruire explicitement l'ancien GameManager s'il existe,
        // pour forcer une réinitialisation propre.
        if (GameManager.Instance != null)
        {
            Object.Destroy(GameManager.Instance.gameObject);
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    ///Lance la partie sauvegardée
    public void Continue()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.LogWarning("[MainMenu] Aucune sauvegarde trouvée.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[MainMenu] GameManager.Instance est null dans le menu.");
        }

        SceneManager.sceneLoaded += OnGameplaySceneLoaded;
        SceneManager.LoadScene(gameplaySceneName);
    }

    //amène au menu des options
    public void Options()
    {
        SceneManager.LoadScene(optionsSceneName);
    }

    //Quitte le jeu
    public void Quit()
    {
        Application.Quit();
    }

    // si on appuie sur continuer, on charge la sauvegarde après le chargement de la scène de jeu
    private void OnGameplaySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameplaySceneName)
            return;

        SceneManager.sceneLoaded -= OnGameplaySceneLoaded;

        // Charger la sauvegarde
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGame();
        }
        else
        {
            Debug.LogError("[MainMenu] GameManager.Instance est null après chargement.");
        }

        // Activer l'UI principale
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIActive(true);
            UIManager.Instance.ShowMainUI();
        }
        else
        {
            Debug.LogWarning("[MainMenu] UIManager.Instance est null.");
        }
    }
}
