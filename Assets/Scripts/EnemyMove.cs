using System.Collections;
using UnityEngine;

public class EnemyMove : MonoBehaviour, IDamageable
{
    //Declarando variável para armazenar a posição do alvo
    private Transform target;
     // Publicando a variável vida para que possa ser ajustada no Inspector do Unity
    public int vida =2;
    public float fireHate  = 1.0f;
    private GameManager gameManager;
    private GameObject tiroPrefab;
    private GameObject gun;
    private bool canShoot = false;

    private void Awake()
    {
        if(gun == null)
        {
        gun = transform.Find("Enemy/Gun").gameObject;            
        }
        if(target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }
        if(gameManager == null)
        {
            gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        }

        if(tiroPrefab == null)
        {
            tiroPrefab = Resources.Load<GameObject>("Tiro");
        }

    }


    // Update is called once per frame
     private void Update()
    {
        if(gameManager.gameState == GameManager.GameState.OnPlay)
        {
            SeguirJogador();
            Vector2 direcao = target.position - gun.transform.position;
            gun.transform.up = direcao;
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
            Transform gunTr = gun.transform;
            
            GameObject tiro = Instantiate(tiroPrefab, gunTr.position, gunTr.rotation);
            tiro.GetComponent<Shot>().tagGameObject = "Inimigo";    
        }
        yield return new WaitForSeconds(fireHate);
            canShoot = true;
    }

     private void SeguirJogador()
    //Definindo a posição do inimigo para a posição do alvo, velocidade de movimento. 
    { transform.position = Vector2.MoveTowards(transform.position, target.position, 2 * Time.deltaTime/4);
    }
}
