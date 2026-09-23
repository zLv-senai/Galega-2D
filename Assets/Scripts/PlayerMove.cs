using UnityEngine;

public class PlayerMove : MonoBehaviour

{   int vida = 100;
    float velocidade = 5.0f;

    int pontuacao = 0;

    string tipoDeTiro = "Bala Simples";

    int xp = 0;

    int level = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Movimento();
    }

    void GanharXp (int xpGanho)
    {
        xp = xp + xpGanho;

        if (xp >= 100)
        {
            level = level + 1;
            xp = xp - 100;
        }

    }

    void Movimento()
        {
            if (Input.GetKey(KeyCode.W))
        {
            transform.position = transform.position + new Vector3 (0, 1, 0) * velocidade * Time.deltaTime;

        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.position = transform.position + new Vector3 (-1, 0, 0) * velocidade * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.position = transform.position + new Vector3 (0, -1, 0) * velocidade * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.position = transform.position + new Vector3 (1, 0, 0) * velocidade * Time.deltaTime;
        }
        }
}
