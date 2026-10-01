using UnityEngine;

public class Som : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip Sons;

    // Update is called once per frame

    void Update()
    {
        // Integração: o projeto usa só o Input System novo; Input.GetKeyDown daria erro.
        // Integração: só com o jogo rodando (no menu o Espaço é digitado no campo de nome).
        if (EstadoDoJogo.Rodando
            && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlaySound(Sons);
        }
    }
    public void PlaySound(AudioClip som)
    
    {
        // Integração: o slider "Efeitos" do menu Settings também vale aqui (o "Geral" já vale pelo AudioListener).
        audioSource.PlayOneShot(som, ConfiguracaoDeAudio.Efeitos);
    }
}
