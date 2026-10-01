using UnityEngine;

public class Som : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip Sons;

    // Update is called once per frame

    void Update()
    {
        // Integração: o projeto usa só o Input System novo; Input.GetKeyDown daria erro.
        if (UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlaySound(Sons);
        }
    }
    public void PlaySound(AudioClip som)
    
    {
        audioSource.PlayOneShot(som);
    }
}
