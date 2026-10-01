using UnityEngine;

public class Pontuacao : MonoBehaviour
{
    public int pontos ;
    public int multiplicador = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Integração: o projeto usa só o Input System novo; Input.GetKeyDown daria erro.
        if (UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            pontos += 10 * multiplicador;
        }
    }
}