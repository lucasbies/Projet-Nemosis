using UnityEngine;
using UnityEngine.Audio;

public class FinalScore : MonoBehaviour
{
    public TMPro.TextMeshProUGUI ScoreText;

    public GameObject player;


    public AudioResource audioSourceDeath;
    public AudioResource audioSourceWin;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void Update()
    {
        if (player.GetComponent<PacmanMovement>().health <= 0)
        {
            ScoreText.text = "Tu es mort !";
            StopAllAudio();
            GetComponent<AudioSource>().resource = audioSourceDeath;
            GetComponent<AudioSource>().Play();

            Time.timeScale = 0f; // Arrête le temps pour figer le jeu
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {   
        if(other.gameObject == player)
        {
            StopAllAudio();
            GetComponent<AudioSource>().resource = audioSourceWin;
            GetComponent<AudioSource>().Play();

            ScoreText.text = "Score : " + player.GetComponent<PacmanMovement>().health;
            Time.timeScale = 0f;
        }
    }

    void StopAllAudio()
    {
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allSources)
        {
            source.Stop();
        }
    }
}
