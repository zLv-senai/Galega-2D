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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            pontos += 10 * multiplicador;
        }
    }
}