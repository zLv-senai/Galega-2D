using System.Collections;
using UnityEngine;

// Inimigo: segue o player, mira a arma nele e atira só quando está dentro da tela.
// Acha sozinho tudo o que precisa (player, GameManager, tiro e arma), então funciona
// tanto colocado na cena quanto criado por código pelo EnemySpawn (via Resources).
public class EnemyMove : MonoBehaviour, IDamageable
{
    // Tag que marca os tiros dos inimigos (o Shot ignora quem tem a mesma tag de quem atirou).
    private const string TagInimigo = "Inimigo";

    // Nome do prefab do tiro dentro de Assets/Resources, usado se o campo tiroPrefab estiver vazio.
    private const string TiroNoResources = "Tiro";

     // Publicando a variável vida para que possa ser ajustada no Inspector do Unity
    public int vida =2;
    public float fireHate  = 1.0f;

    // Opcionais: se ficarem vazios, o tiro vem do Resources e a arma é procurada nos filhos.
    [SerializeField] private GameObject tiroPrefab;
    [SerializeField] private Transform gun;

    // Contato: distância em que o inimigo para de andar, colado na nave em vez de ficar em cima dela.
    [SerializeField] float distanciaParada = 0.6f;

    // Só atira depois de entrar esta fração para dentro da tela (0.05 = 5% de cada borda).
    [SerializeField, Range(0f, 0.4f)] private float margemTela = 0.05f;

    // Segundos entre entrar na tela e o primeiro tiro (dá tempo do jogador ver o inimigo).
    [SerializeField] private float atrasoPrimeiroTiro = 0.5f;

    // Achados sozinhos no Start (um inimigo criado por código não tem nada arrastado).
    //Declarando variável para armazenar a posição do alvo
    private Transform target;
    private GameManager gameManager;

    // Intervalo entre tiros: fica false enquanto espera o fireHate.
    private bool canShoot = true;

    // Time.time a partir do qual pode atirar depois de entrar na tela (-1 = está fora da tela).
    private float liberaTiroEm = -1f;

    private Camera cam;

    // Evento estático: quem quiser saber quando QUALQUER inimigo morre assina aqui (ex.: GeradorDeGemas).
    public static event System.Action<EnemyMove> AoMorrer;

    private void Start()
    {
        cam = Camera.main;
        gameManager = GameManager.Instance;

        if (gun == null)
        {
            // "Gun" = inimigos antigos da cena; "Enemy/Gun" = prefab 3D do Resources.
            gun = transform.Find("Gun");
            if (gun == null)
            {
                gun = transform.Find("Enemy/Gun");
            }

            if (gun == null)
            {
                // Sem "Gun" no prefab e sem referência arrastada: atira a partir
                // do próprio transform em vez de quebrar o inimigo.
                gun = transform;
                Debug.LogWarning("EnemyMove: 'Gun' não encontrado em " + name + ". Usando o próprio transform como ponto de disparo.");
            }
        }

        if (tiroPrefab == null)
        {
            tiroPrefab = Resources.Load<GameObject>(TiroNoResources);
            if (tiroPrefab == null)
            {
                Debug.LogWarning("EnemyMove: sem tiroPrefab e sem 'Tiro' em Assets/Resources. " + name + " não vai atirar.");
            }
        }

        BuscarAlvo();
    }

    private void BuscarAlvo()
    {
        if (target != null)
        {
            return;
        }

        GameObject jogador = GameObject.FindWithTag("Player");
        if (jogador != null)
        {
            target = jogador.transform;
        }
    }

    // Update is called once per frame
     private void Update()
    {
        GameManager gm = ObterGameManager();
        if(gm == null || gm.gameState != GameManager.GameState.OnPlay)
        {
            return;
        }

        SeguirJogador();
        MirarNoJogador();

        // Fora da tela: não atira, e o atraso do primeiro tiro recomeça quando voltar.
        if (!EstaNaTela())
        {
            liberaTiroEm = -1f;
            return;
        }

        if (liberaTiroEm < 0f)
        {
            liberaTiroEm = Time.time + atrasoPrimeiroTiro;
        }

        if (canShoot && Time.time >= liberaTiroEm)
        {
            canShoot = false;
            StartCoroutine(Shoot());
        }
    }

    // O GameManager pode ainda não existir no Start (ordem de inicialização); tenta de novo.
    private GameManager ObterGameManager()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        return gameManager;
    }

    // Mesma técnica do DestruirForaDaTela: posição na tela de 0 a 1, descontando a margem.
    private bool EstaNaTela()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                return false;
            }
        }

        Vector3 p = cam.WorldToViewportPoint(transform.position);
        return p.x > margemTela && p.x < 1f - margemTela
            && p.y > margemTela && p.y < 1f - margemTela;
    }

    // Gira a arma para o "up" dela apontar para o player (o tiro anda no eixo Y local).
    private void MirarNoJogador()
    {
        if (target == null || gun == null || gun == transform)
        {
            return;
        }

        Vector2 direcao = target.position - gun.position;
        if (direcao.sqrMagnitude > 0.0001f)
        {
            gun.up = direcao;
        }
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
            // A rotação sai da direção até o player, então funciona mesmo quando
            // a arma é o próprio inimigo (que não é girado pela mira).
            Vector2 direcao = target.position - gun.position;
            Quaternion rotacao = direcao.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(Vector3.forward, direcao)
                : gun.rotation;

            GameObject tiro = Instantiate(tiroPrefab, gun.position, rotacao);

            // Tiro do inimigo: não acerta outros inimigos e não explode minas.
            Shot shot = tiro.GetComponent<Shot>();
            if (shot != null)
            {
                shot.Configurar(TagInimigo, false);
            }
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
