using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class FinalDoor : MonoBehaviour
{

    public float openHeight = 0f;
    public GameObject Score;
    private bool hasStartedOpening = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Score.GetComponent<Score>().Cire + Score.GetComponent<Score>().Plume >= 5)
        {
            if (!hasStartedOpening)
            {
                hasStartedOpening = true;
                GetComponent<AudioSource>().Play();
                StartCoroutine(SpawnDelay());
            }

            transform.position += new Vector3(0, openHeight * Time.deltaTime, 0);
        }
    }


    
    IEnumerator SpawnDelay() {
        yield return new WaitForSeconds(2f);
        Destroy(this.gameObject);
    }
}
