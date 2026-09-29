using System.Collections;
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
    [SerializeField] private Transform gun;
    private bool canShoot = true;
    // Contato: distância em que o inimigo para de andar, colado na nave em vez de ficar em cima dela.
    [SerializeField] float distanciaParada = 0.6f;

    // Evento estático: quem quiser saber quando QUALQUER inimigo morre assina aqui (ex.: GeradorDeGemas).
    public static event System.Action<EnemyMove> AoMorrer;

    private void Start()
    {
        if (gun == null)
        {
            Transform gunEncontrada = transform.Find("Gun");
            if (gunEncontrada != null)
            {
                gun = gunEncontrada;
            }
            else
            {
                // Sem "Gun" no prefab e sem referência arrastada: atira a partir
                // do próprio transform em vez de quebrar o inimigo.
                gun = transform;
                Debug.LogWarning("EnemyMove: 'Gun' não encontrado em " + name + ". Usando o próprio transform como ponto de disparo.");
            }
        }

        if (target == null)
        {
            GameObject jogador = GameObject.FindWithTag("Player");
            if (jogador != null)
            {
                target = jogador.transform;
            }
        }
    }


    // Update is called once per frame
     private void Update()
    {
        GameManager gm = ObterGameManager();
        if(gm != null && gm.gameState == GameManager.GameState.OnPlay)
        {
            SeguirJogador();
            if (canShoot)
            {
            canShoot = false;
            StartCoroutine(Shoot());
            }
        }
    }

    // gameManager pode não estar arrastado no Inspector; usa o singleton como fallback.
    private GameManager ObterGameManager()
    {
        return gameManager != null ? gameManager : GameManager.Instance;
    }

    // Método para reduzir a vida do inimigo quando ele recebe dano
    public void TakeDamage(int dano)
    {
        if (vida < 1)
        {
            // Já está morto (ou prestes a ser destruído neste frame): evita Destroy/AoMorrer duplicados.
            return;
        }

          vida -= dano;
          Debug.Log(vida);

        if (vida < 1 )
        {
            // try/finally: o Destroy acontece mesmo se um assinante de AoMorrer lançar exceção.
            try
            {
                AoMorrer?.Invoke(this);
            }
            finally
            {
                Destroy(gameObject);
            }
        }
    }

       IEnumerator Shoot()
    {
        // Só atira se tiver alvo, prefab e ponto de disparo. canShoot sempre
        // volta a true depois do yield, mesmo se algum desses estiver nulo,
        // para o inimigo não travar sem poder atirar de novo.
        if(target != null && tiroPrefab != null && gun != null)
        {
            Instantiate(tiroPrefab, gun.position, gun.rotation);
        }

        yield return new WaitForSeconds(fireHate);
        canShoot = true;
    }

     private void SeguirJogador()
    //Definindo a posição do inimigo para a posição do alvo, velocidade de movimento.
    {
        // Se o alvo (player) morreu/sumiu, não há para onde seguir.
        if (target == null)
        {
            return;
        }

        // Contato: já está colado no alvo, então não se move mais.
        if (Vector2.Distance(transform.position, target.position) <= distanciaParada)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target.position, 2 * Time.deltaTime/4);
    }
}
