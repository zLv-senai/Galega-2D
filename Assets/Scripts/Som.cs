using UnityEngine;

public class Som : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip Sons;

    // Update is called once per frame

    void Update()
    {
        if (Input.GetKeyDown (KeyCode.Space))
        {
            PlaySound(Sons);
        }
    }
    public void PlaySound(AudioClip som)
    
    {
        audioSource.PlayOneShot(som);
    }
}
