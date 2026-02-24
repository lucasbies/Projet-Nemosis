using UnityEngine;
using System.Collections;

public class Spawn : MonoBehaviour
{
    [Tooltip("Feather or Wax")]
    public GameObject feather,wax;

    public GameObject toSpawn = null;

    void Start()
    {
        SpawnItem();
    }

    void Update()
    {
        //On lance la coroutine si 
        if ( toSpawn == null)
        {
            StartCoroutine(SpawnDelay());
        }

    }
    void SpawnItem() {
        int rand = Random.Range(0, 2);
        if (rand == 0) {
            toSpawn = Instantiate(feather, transform.position, Quaternion.identity);
        }
        else {
            toSpawn = Instantiate(wax, transform.position, Quaternion.identity);
        }
        
    }
    IEnumerator SpawnDelay() {
        toSpawn = feather; // Juste pour éviter de relancer plusieurs fois la coroutine
        GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(Random.Range(1f, 2f));
        SpawnItem();
    }




}
