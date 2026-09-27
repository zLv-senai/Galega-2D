using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyMove : MonoBehaviour, IDamageable
{
    //Declarando variável para armazenar a posição do alvo
    public Transform target;
     // Publicando a variável vida para que possa ser ajustada no Inspector do Unity
    public int vida =2;
    public float fireHate  = 1.0f;
    public GameManager gameManager;
    public GameObject tiroPrefab;
    private Transform gun;
    private bool canShoot = true;

    private void Start()
    {
        gun = transform.Find("Gun").gameObject.transform;
    }


    // Update is called once per frame
     private void Update()
    {
        if(gameManager.gameState == GameManager.GameState.OnPlay)
        {
            SeguirJogador();
            if (canShoot)
            {
            canShoot = false;
            StartCoroutine(Shoot());
            }
        }
    }

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

       IEnumerator Shoot()
    {
        if(tiroPrefab != null && gun != null)
        {
            Instantiate(tiroPrefab, gun.position, Quaternion.Euler(0, 0, -180));
            yield return new WaitForSeconds(fireHate);
            canShoot = true;
        }
    }

     private void SeguirJogador()
    //Definindo a posição do inimigo para a posição do alvo, velocidade de movimento. 
    { transform.position = Vector2.MoveTowards(transform.position, target.position, 2 * Time.deltaTime/4);
    }
}
