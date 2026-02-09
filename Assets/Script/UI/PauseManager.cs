using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("UI References - À assigner dans l'Inspector")]
    [Tooltip("Le panel/canvas qui contient le menu pause")]
    public GameObject pauseMenuRoot;

    [Tooltip("Premier bouton sélectionné (pour manette)")]
    public GameObject firstSelectedButton;

    [Tooltip("Canvas des piliers à masquer pendant la pause")]
    public GameObject pillarsCanvas;

    [Header("Noms des scènes")]
    public string mainMenuSceneName = "Menu_principal";
    public string optionsSceneName = "Menu_option";
    public string scheduleSceneName = "EmploiDuTemps";

    private void Start()
    {
        // S'assurer que le menu pause est caché au démarrage
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
    }

    /// <summary>
    /// Affiche le menu pause
    /// </summary>
    public void ShowPauseMenu()
    {
        // Masquer les piliers
        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(false);

        // Afficher le menu
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);

            // Focus manette sur le premier bouton
            if (EventSystem.current != null && firstSelectedButton != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            }
        }
    }

    /// <summary>
    /// Cache le menu pause
    /// </summary>
    public void HidePauseMenu()
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        // Réafficher les piliers
        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(true);
    }

    // ===== BOUTONS DU MENU PAUSE =====

    /// <summary>
    /// Bouton "Continuer" - Reprend le jeu
    /// </summary>
    public void OnContinueButton()
    {
        PauseController.Instance?.Resume();
    }

    /// <summary>
    /// Bouton "Options"
    /// </summary>
    public void OnOptionsButton()
    {
        CleanupAndLoadScene(optionsSceneName);
    }

    /// <summary>
    /// Bouton "Emploi du temps"
    /// </summary>
    public void OnScheduleButton()
    {
        CleanupAndLoadScene(scheduleSceneName);
    }

    /// <summary>
    /// Bouton "Quitter vers menu principal"
    /// </summary>
    public void OnQuitToMainMenuButton()
    {
        StartCoroutine(QuitToMainMenuSequence());
    }

    private IEnumerator QuitToMainMenuSequence()
    {
        Debug.Log("[PauseManager] Séquence de retour au menu principal démarrée.");

        // 1. Réinitialiser l'état de pause
        PauseController.ResetPauseState();

        // 2. Cacher toutes les UI
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(false);

        if (GameManager.Instance != null)
        {
            Debug.Log("[PauseManager] Destruction du GameManager pour réinitialisation complète.");
            Destroy(GameManager.Instance.gameObject);
        }

        if (UIManager.Instance != null)
        {
            Debug.Log("[PauseManager] Destruction de l'UIManager.");
            Destroy(UIManager.Instance.gameObject);
        }

        if (DOTweenManager.Instance != null)
        {
            Debug.Log("[PauseManager] Destruction de DOTweenManager.");
            Destroy(DOTweenManager.Instance.gameObject);
        }

        if (PauseController.Instance != null)
        {
            Debug.Log("[PauseManager] Destruction de PauseController.");
            Destroy(PauseController.Instance.gameObject);
        }

        // 3. Attendre une frame pour que les destructions soient effectives
        yield return null;

        // 4. Charger la scène du menu principal
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.Log($"[PauseManager] Chargement de la scène '{mainMenuSceneName}'.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("[PauseManager] Nom de scène du menu principal vide!");
        }
    }

    private void CleanupAndLoadScene(string sceneName)
    {
        PauseController.ResetPauseState();

        // Cacher les UI
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        if (pillarsCanvas != null)
            pillarsCanvas.SetActive(false);

        // Charger la scène
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogError($"[PauseManager] Nom de scène vide!");
    }
}