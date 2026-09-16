using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    //Declarando variável para armazenar a posição do alvo
 public Transform target;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
     void Start()
    { 
     
    }
 private void SeguirJogador()
 //Definindo a posição do inimigo para a posição do alvo
    { transform.position = Vector2.MoveTowards(transform.position, target.position, 2 * Time.deltaTime/4);
    }
    // Update is called once per frame
     void Update()
    {
        SeguirJogador();
    }
}
