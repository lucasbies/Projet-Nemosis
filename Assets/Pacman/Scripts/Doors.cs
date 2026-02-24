using UnityEngine;
using System.Collections;
public class Doors : MonoBehaviour
{
    public float openHeight = 0f;
    public float openWidth = 0f;
    public GameObject Score;

    private AudioSource audioSource;
    private bool hasStartedOpening = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Score.GetComponent<Score>().Cire + Score.GetComponent<Score>().Plume >= 1)
        {
            if (!hasStartedOpening)
            {
                hasStartedOpening = true;
                if (audioSource != null)
                    audioSource.Play();
                StartCoroutine(SpawnDelay());
            }

            transform.position += new Vector3(openWidth * Time.deltaTime, openHeight * Time.deltaTime, 0);
        }
    }

    IEnumerator SpawnDelay() {
        yield return new WaitForSeconds(2.6f);
        Destroy(this.gameObject);
    }
}
