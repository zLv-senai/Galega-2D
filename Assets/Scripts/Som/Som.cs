using UnityEngine;

// Toca o clipe 'Sons' quando o jogador aperta Espaço (com o jogo rodando), no volume do slider Efeitos.
// Código não usado: nenhuma cena ou prefab tem este script.
public class Som : MonoBehaviour
{
    // AudioSource que toca o som e o clipe tocado ao apertar Espaço.
    public AudioSource audioSource;
    public AudioClip Sons;

    // Update is called once per frame

    // Com o jogo rodando, cada Espaço apertado toca o clipe 'Sons'.
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
    // Toca o clipe uma vez, no volume do slider 'Efeitos' do menu Settings.
    public void PlaySound(AudioClip som)
    
    {
        // Integração: o slider "Efeitos" do menu Settings também vale aqui (o "Geral" já vale pelo AudioListener).
        audioSource.PlayOneShot(som, ConfiguracaoDeAudio.Efeitos);
    }
}
