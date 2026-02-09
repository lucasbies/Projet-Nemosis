using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class PauseController : MonoBehaviour
{
    private static PauseController _instance;
    public static PauseController Instance => _instance;

    // État de pause - statique pour persister entre les scènes
    public static bool IsGamePaused { get; private set; } = false;

    [Header("Configuration")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private void Awake()
    {
        // Gestion singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Doit être à la racine pour DontDestroyOnLoad
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        // Réinitialiser au démarrage
        IsGamePaused = false;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        // Seulement dans la scène de jeu
        if (SceneManager.GetActiveScene().name != gameplaySceneName)
            return;

        // Détection des inputs
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool startPressed = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        if (escapePressed || startPressed)
            TogglePause();
    }

    public void TogglePause()
    {
        if (SceneManager.GetActiveScene().name != gameplaySceneName)
            return;

        if (IsGamePaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (IsGamePaused) return;

        IsGamePaused = true;
        Time.timeScale = 0f;

        // Cacher l'UI principale
        UIManager.Instance?.HideAllUI();

        // Afficher le menu pause via le PauseManager de la scène
        var pauseManager = FindFirstObjectByType<PauseManager>();
        if (pauseManager != null)
            pauseManager.ShowPauseMenu();
        else
            Debug.LogWarning("[PauseController] PauseManager non trouvé dans la scène!");
    }

    public void Resume()
    {
        if (!IsGamePaused) return;

        IsGamePaused = false;
        Time.timeScale = 1f;

        // Cacher le menu pause
        var pauseManager = FindFirstObjectByType<PauseManager>();
        if (pauseManager != null)
            pauseManager.HidePauseMenu();

        // Réafficher l'UI principale
        UIManager.Instance?.ShowMainUI();
    }

    /// <summary>
    /// Appelé avant de changer de scène (quitter, options, etc.)
    /// </summary>
    public static void ResetPauseState()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;
    }
}