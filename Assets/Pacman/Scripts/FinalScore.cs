using UnityEngine;

public class FinalScore : MonoBehaviour
{
    public TMPro.TextMeshProUGUI ScoreText;
    public float scoreTime;
    public GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scoreTime = 0;
        ScoreText.text = "";
    }

    void OnTriggerEnter2D(Collider2D other)
    {   
        if(other.gameObject == player)
        {
            scoreTime = Time.time;
            ScoreText.text = "Final Score : " + scoreTime.ToString("F2") + " seconds!";
        }
    }
}
