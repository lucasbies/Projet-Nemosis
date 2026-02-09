using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    public GameObject pauseMenuRoot;      // Panel racine du menu pause
    public GameObject firstSelected;      // Bouton sélectionné par défaut (Continue)
    [Tooltip("Bouton ou icône qui ouvre le menu pause")]
    public GameObject pauseToggleButton;  // <-- ton bouton TogglePause

    [Header("Autres Canvas à masquer")]
    [SerializeField] private GameObject pillarsCanvas; // <-- assigne le canvas des piliers ici

    [Header("Scenes")]
    public string mainMenuSceneName = "Menu_principal";
    public string optionsSceneName = "Menu_option";
    public string gameplaySceneName = "SampleScene"; // scène où le bouton doit être visible

    private bool _isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pauseToggleButton != null)
            pauseToggleButton.SetActive(scene.name == gameplaySceneName);

        if (scene.name == gameplaySceneName)
        {
            // 1) Réactiver tout l’UI principal géré par UIManager
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetUIActive(true);
                UIManager.Instance.ShowMainUI();   // <-- au lieu de GameModeChoice direct
            }

            // 2) Réactiver (ou non) le canvas des piliers
            if (pillarsCanvas != null)
                pillarsCanvas.SetActive(true);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            TogglePause();
    }

    public void TogglePause()
    {
        if (_isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (_isPaused) return;

        _isPaused = true;
        Time.timeScale = 0f;

        // Masquer le canvas des piliers quand on ouvre le menu pause
        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(false);

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);

            if (EventSystem.current != null && firstSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(firstSelected);
            }
        }
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;

        _isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        // Si tu veux que le canvas des piliers revienne quand on reprend :
        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(true);
    }

    // Bouton "Continuer"
    public void OnContinueButton()
    {
        UIManager.Instance?.SetUIActive(true);
        ResumeGame();
    }

    // Bouton "Options"
    public void OnOptionsButton()
    {
        
        Time.timeScale = 1f;
        _isPaused = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        // On garde aussi les piliers cachés en quittant vers Options
        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(false);

        UIManager.Instance?.HideAllUI();

        if (!string.IsNullOrEmpty(optionsSceneName))
            SceneManager.LoadScene(optionsSceneName);
        else
            Debug.LogWarning("PauseManager : optionsSceneName non défini.");
    }

    // Bouton "Quitter"
    public void OnQuitToMainMenuButton()
    {
        Time.timeScale = 1f;
        _isPaused = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(false);

        UIManager.Instance?.HideAllUI();

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogWarning("PauseManager : mainMenuSceneName non défini.");
    }

    public void OnScheduleButton()
    {
        Time.timeScale = 1f;
        _isPaused = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(false);

        UIManager.Instance?.HideAllUI();
        SceneManager.LoadScene("EmploiDuTemps");
    }
}