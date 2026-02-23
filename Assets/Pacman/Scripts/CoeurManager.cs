using UnityEngine;

public class CoeurManager : MonoBehaviour
{
    public GameObject coeur1;
    public GameObject coeur2;
    public GameObject coeur3;
    public GameObject Player;

    public Sprite coeurVide;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        switch (Player.GetComponent<PacmanMovement>().health) {
            case 2: 
                coeur1.GetComponent<SpriteRenderer>().sprite = coeurVide;
                break;
            case 1: 
                coeur2.GetComponent<SpriteRenderer>().sprite = coeurVide;
                break;
            case 0:
                coeur3.GetComponent<SpriteRenderer>().sprite = coeurVide;
                break;

        }
    }
}
