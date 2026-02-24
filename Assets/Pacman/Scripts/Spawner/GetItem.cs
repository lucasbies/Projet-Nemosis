using UnityEngine;
using UnityEngine.Audio;

public class GetItem : MonoBehaviour
{

    public GameObject Score;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if(collision.gameObject.tag == "Player")
        {
            if(gameObject.tag == "Wax")
            {
                Score.GetComponent<Score>().Cire += 1;
            }
            else if(gameObject.tag == "Feather")
            {
                Score.GetComponent<Score>().Plume += 1;

            }

            Destroy(this.gameObject);
        }
    }
}
