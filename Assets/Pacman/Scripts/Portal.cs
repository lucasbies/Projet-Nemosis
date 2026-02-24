using UnityEngine;

public class Portal : MonoBehaviour
{

    public GameObject linkedPortal;

    public float teleportDecalage = 0; // Délai pour éviter les téléportations instantanées en boucle

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
        if(collision.gameObject.CompareTag("Player"))
        {
            GetComponent<AudioSource>().Play();
            collision.transform.position = linkedPortal.transform.position + new Vector3(0, teleportDecalage, 0); // Impossible si c'est si simple lol 
        }
    }
}
