using UnityEngine;

// Contador de pontos de teste: soma 10 x multiplicador a cada Espaço com o jogo rodando.
// Código não usado: nenhuma cena ou prefab tem este script.
public class Pontuacao : MonoBehaviour
{
    // Pontos acumulados e o multiplicador aplicado a cada Espaço.
    public int pontos ;
    public int multiplicador = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    // Com o jogo rodando, cada Espaço apertado soma 10 x multiplicador nos pontos.
    void Update()
    {
        // Integração: o projeto usa só o Input System novo; Input.GetKeyDown daria erro.
        // Integração: só com o jogo rodando (no menu o Espaço é digitado no campo de nome).
        if (EstadoDoJogo.Rodando
            && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            pontos += 10 * multiplicador;
        }
    }
}