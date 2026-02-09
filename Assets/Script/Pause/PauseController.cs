using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; } = false;

    [Header("Référence UI Pause")]
    [SerializeField] private PauseManager pauseManager; // assigne ton PauseManager dans l’Inspector (optionnel)

    private void Awake()
    {
        // Garder le contrôleur entre les scènes (un seul point d'entrée pour l'input Pause)
        DontDestroyOnLoad(gameObject);

        if (pauseManager == null)
            pauseManager = PauseManager.Instance;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // Centralise l’input de pause ici (Escape / Start)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            TogglePause();
    }

    public void TogglePause()
    {
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

        // Masquer UI de jeu principale si présent
        if (UIManager.Instance != null)
            UIManager.Instance.HideAllUI();

        EnsurePauseManager();
        if (pauseManager != null)
            pauseManager.PauseGame();
        else
            Debug.LogWarning("[PauseController] Pas de PauseManager trouvé pour afficher le menu pause.");
    }

    public void Resume()
    {
        if (!IsGamePaused) return;

        IsGamePaused = false;
        Time.timeScale = 1f;

        EnsurePauseManager();
        if (pauseManager != null)
            pauseManager.ResumeGame();
    }

    // Lorsqu'une nouvelle scène est chargée, restaurer l'état de pause si nécessaire
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Retrouver PauseManager dans la nouvelle scène si l'instance a été recréée
        pauseManager = PauseManager.Instance ?? FindFirstObjectByType<PauseManager>();

        if (IsGamePaused)
        {
            // Si on est en pause, s'assurer que le timescale est à 0 et que le menu est visible
            Time.timeScale = 0f;
            if (UIManager.Instance != null)
                UIManager.Instance.HideAllUI();

            if (pauseManager != null)
                pauseManager.PauseGame();
            else
                Debug.LogWarning("[PauseController] OnSceneLoaded : PauseManager introuvable alors que le jeu doit être en pause.");
        }
        else
        {
            // s'assurer d'être en run normal
            Time.timeScale = 1f;
            if (pauseManager != null)
                pauseManager.ResumeGame();
        }
    }

    private void EnsurePauseManager()
    {
        if (pauseManager == null)
            pauseManager = PauseManager.Instance ?? FindFirstObjectByType<PauseManager>();
    }
}