using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    // Publicando a variável vida para que possa ser ajustada no Inspector do Unity
    public int vida =2;   

    // Método para reduzir a vida do inimigo quando ele recebe dano
    public void TakeDamage(int dano)
    {
                  
          vida -= dano;
          Debug.Log(vida);

        if (vida < 1 )  
        {
            Destroy(gameObject);
        }
    }
}