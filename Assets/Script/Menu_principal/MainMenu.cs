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

    private void Awake()
    {
        // IMPORTANT: Réinitialiser l'état de pause
        PauseController.ResetPauseState();
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        // Focus manette
        if (EventSystem.current != null && firstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    private void Start()
    {
        // Double sécurité pour le timeScale
        Time.timeScale = 1f;

        // Désactiver "Continuer" si pas de sauvegarde
        bool hasSave = PlayerPrefs.HasKey(SAVE_KEY);
        if (continueButton != null)
            continueButton.interactable = hasSave;
    }

    public void Play()
    {
        // Supprimer l'ancienne sauvegarde
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.SetInt("NEW_GAME_REQUESTED", 1);
        PlayerPrefs.Save();

        // Détruire l'ancien GameManager
        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        // Réinitialiser la pause et charger
        PauseController.ResetPauseState();
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Continue()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.LogWarning("[MainMenu] Aucune sauvegarde trouvée.");
            return;
        }

        PauseController.ResetPauseState();
        SceneManager.sceneLoaded += OnGameplaySceneLoaded;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Options()
    {
        SceneManager.LoadScene(optionsSceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void OnGameplaySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameplaySceneName)
            return;

        SceneManager.sceneLoaded -= OnGameplaySceneLoaded;

        if (GameManager.Instance != null)
            GameManager.Instance.LoadGame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIActive(true);
            UIManager.Instance.ShowMainUI();
        }
    }
}