//Bonjour mon cher Jordan, j'espère que tu te porte bien et au pire si ce n'est pas le cas je m'en bas les couilles tu peux aller te faire enculé avec toute ta famille d'italien en espérant que tu retourne dans ton pays pour manger des pates et des pizzas et tu m'as demandé d'abréger donc je m'arrête là, je te souhaite une bonne journée et j'espère que tu vas crever dans d'atroces souffrances et que tu vas brûler en enfer pour l'éternité, au revoir mon cher Jordan.
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer & param�tres expos�s")]
    public AudioMixer masterMixer;
    public string masterParam = "MasterVolume";
    public string musicParam = "MusicVolume";
    public string sfxParam = "SFXVolume";
    public string voiceParam = "VoiceVolume";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [Header("Audio Sources")]
    public AudioSource musicAudioSource;
    public AudioSource sfxAudioSource;
    public AudioSource voiceAudioSource;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Appliquer les valeurs sauvegard�es imm�diatement
        ApplySavedVolumes();

        // R�-applications retard�es pour �craser d'�ventuels overrides d'initialisation
        StartCoroutine(ReapplyNextFrame());

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // applique puis r�applique la frame suivante (prot�ge contre overrides post-load)
        ApplySavedVolumes();
        StartCoroutine(ReapplyNextFrame());
    }

    private IEnumerator ReapplyNextFrame()
    {
        // attendre la fin du frame courant et du suivant pour �tre s�r
        yield return null;
        yield return null;
        ApplySavedVolumes();
    }

    private void ApplySavedVolumes()
    {
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f), save: false);
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f), save: false);
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f), save: false);
        SetVoiceVolume(PlayerPrefs.GetFloat("VoiceVolume", 1f), save: false);

        if (enableDebugLogs)
        {
            LogMixerParam(masterParam);
            LogMixerParam(musicParam);
            LogMixerParam(sfxParam);
            LogMixerParam(voiceParam);
        }
    }

    private void LogMixerParam(string param)
    {
        if (masterMixer == null)
        {
            if (enableDebugLogs) Debug.LogWarning("AudioManager : masterMixer non assign�.");
            return;
        }

        if (masterMixer.GetFloat(param, out float val))
            Debug.Log($"AudioManager : param '{param}' = {val} dB");
        else
            Debug.LogWarning($"AudioManager : param expos� '{param}' introuvable dans le AudioMixer.");
    }

    #region Set Volume Methods
    public void SetMasterVolume(float linear, bool save = true)
    {
        if (masterMixer != null)
            masterMixer.SetFloat(masterParam, LinearToDB(linear));
        if (save) PlayerPrefs.SetFloat("MasterVolume", linear);
        if (enableDebugLogs) Debug.Log($"AudioManager: SetMasterVolume({linear})");
    }

    public void SetMusicVolume(float linear, bool save = true)
    {
        float db = LinearToDB(linear);
        if (masterMixer != null)
            masterMixer.SetFloat(musicParam, db);
        if (save) PlayerPrefs.SetFloat("MusicVolume", linear);
        if (enableDebugLogs) Debug.Log($"AudioManager: SetMusicVolume({linear}) -> {db} dB");
    }

    public void SetSFXVolume(float linear, bool save = true)
    {
        if (masterMixer != null)
            masterMixer.SetFloat(sfxParam, LinearToDB(linear));
        if (save) PlayerPrefs.SetFloat("SFXVolume", linear);
        if (enableDebugLogs) Debug.Log($"AudioManager: SetSFXVolume({linear})");
    }

    public void SetVoiceVolume(float linear, bool save = true)
    {
        if (masterMixer != null)
            masterMixer.SetFloat(voiceParam, LinearToDB(linear));
        if (save) PlayerPrefs.SetFloat("VoiceVolume", linear);
        if (enableDebugLogs) Debug.Log($"AudioManager: SetVoiceVolume({linear})");
    }

    private float LinearToDB(float linear)
    {
        if (linear <= 0.0001f) return -80f;
        return Mathf.Log10(linear) * 20f;
    }
    #endregion
    public void Save()
    {
        PlayerPrefs.Save();
    }

    public void PlayLoopMusic(AudioClip clip)
    {
        if (musicAudioSource == null)
        {
            if (enableDebugLogs) Debug.LogWarning("AudioManager: musicAudioSource non assign�.");
            return;
        }

        if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
        {
            if (enableDebugLogs) Debug.Log("AudioManager: La musique est d��j� en cours de lecture.");
            return;
        }

        musicAudioSource.clip = clip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();

        if (enableDebugLogs) Debug.Log($"AudioManager: Lecture de la musique en boucle '{clip.name}'.");
    }

    public void StopLoopMusic()
    {
        if (musicAudioSource == null)
        {
            if (enableDebugLogs) Debug.LogWarning("AudioManager: musicAudioSource non assign�.");
            return;
        }

        musicAudioSource.Stop();
        musicAudioSource.clip = null;

        if (enableDebugLogs) Debug.Log("AudioManager: Musique en boucle arr�t�e.");
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxAudioSource == null)
        {
            if (enableDebugLogs) Debug.LogWarning("AudioManager: sfxAudioSource non assign�.");
            return;
        }

        sfxAudioSource.PlayOneShot(clip);

        if (enableDebugLogs) Debug.Log($"AudioManager: Lecture du SFX '{clip.name}'.");
    }
}