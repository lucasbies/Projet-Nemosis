using UnityEngine;

public class FinalScore : MonoBehaviour
{
    public TMPro.TextMeshProUGUI ScoreText;

    public GameObject player;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void Update()
    {
        if (player.GetComponent<PacmanMovement>().health <= 0)
        {
            ScoreText.text = "Tu es mort !";
            Time.timeScale = 0f; // Arrête le temps pour figer le jeu
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {   
        if(other.gameObject == player)
        {
            ScoreText.text = "Score : " + player.GetComponent<PacmanMovement>().health;
            Time.timeScale = 0f;
        }
    }
}
