using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // Publicando a variável vida para que possa ser ajustada no Inspector do Unity
    public int vida =2;   

    // Método para reduzir a vida do inimigo quando ele recebe dano
    public void TakeDamage(int dano)
    {
                  
          vida -= dano; 

        if (vida < 1 )  
        {
            Destroy(gameObject);
        }
    }
}