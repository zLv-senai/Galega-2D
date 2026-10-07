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

    // Integração (VFX): prefab da explosão em Assets/Resources, usado se o campo explosaoPrefab estiver vazio,
    // e quanto tempo ela fica na cena se não tiver partículas para medir.
    private const string ExplosaoNoResources = "Explosion";
    private const float DuracaoPadraoExplosao = 2f;

     // Publicando a variável vida para que possa ser ajustada no Inspector do Unity
    public int vida =2;
    // Segundos de espera entre um tiro e o próximo.
    public float fireHate  = 1.0f;

    // Opcionais: se ficarem vazios, o tiro vem do Resources e a arma é procurada nos filhos.
    [SerializeField] private GameObject tiroPrefab;
    [SerializeField] private Transform gun;

    // Integração (VFX): explosão criada onde o inimigo morre. Vazio = Resources/Explosion.
    [SerializeField] private GameObject explosaoPrefab;

    // Avisa uma vez só (e não uma vez por inimigo) que falta o prefab da explosão.
    private static bool avisouSemExplosao;

    // Contato: distância em que o inimigo para de andar, colado na nave em vez de ficar em cima dela.
    [SerializeField] float distanciaParada = 0.6f;

    // Só atira depois de entrar esta fração para dentro da tela (0.05 = 5% de cada borda).
    [SerializeField, Range(0f, 0.4f)] private float margemTela = 0.05f;

    // Segundos entre entrar na tela e o primeiro tiro (dá tempo do jogador ver o inimigo).
    [SerializeField] private float atrasoPrimeiroTiro = 0.5f;

    // Velocidade de perseguição (unidades/s). Antes era fixa em 0,5 (2 * deltaTime / 4): 10x mais lenta
    // que o player (5), então todo inimigo que ficava para trás nunca mais alcançava.
    [SerializeField] private float velocidade = 2f;

    [Header("Reciclagem (inimigo que ficou longe)")]
    // Unidades além da borda da câmera a partir das quais o inimigo conta como "longe".
    [SerializeField] private float distanciaReciclar = 6f;
    // Segundos seguidos longe antes de reaparecer do outro lado da tela.
    [SerializeField] private float tempoParaReciclar = 1.5f;
    // Distância além da borda onde ele reaparece (a mesma margem do EnemySpawn).
    [SerializeField] private float margemReaparecer = 1f;

    // Segundos seguidos que o inimigo está longe (zera quando volta para perto ou é reciclado).
    private float tempoLonge;

    // Achados sozinhos no Start (um inimigo criado por código não tem nada arrastado).
    //Declarando variável para armazenar a posição do alvo
    private Transform target;
    private GameManager gameManager;

    // Intervalo entre tiros: fica false enquanto espera o fireHate.
    private bool canShoot = true;

    // Time.time a partir do qual pode atirar depois de entrar na tela (-1 = está fora da tela).
    private float liberaTiroEm = -1f;

    // Câmera principal, usada para saber se o inimigo está na tela ou ficou longe.
    private Camera cam;

    // Evento estático: quem quiser saber quando QUALQUER inimigo morre assina aqui (ex.: GeradorDeGemas).
    public static event System.Action<EnemyMove> AoMorrer;

    // Som: avisado quando qualquer inimigo atira (ex.: GerenciadorDeSom).
    public static event System.Action<EnemyMove> AoAtirar;

    // Pega a câmera e o GameManager, acha a arma (Gun) e o prefab do tiro se não vieram do Inspector
    // e procura o player.
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

        if (explosaoPrefab == null)
        {
            explosaoPrefab = Resources.Load<GameObject>(ExplosaoNoResources);
            if (explosaoPrefab == null && !avisouSemExplosao)
            {
                Debug.LogWarning("EnemyMove: sem explosaoPrefab e sem 'Explosion' em Assets/Resources. Os inimigos vão morrer sem explosão.");
                avisouSemExplosao = true;
            }
        }

        BuscarAlvo();
    }

    // Se ainda não há alvo, procura o player da cena.
    private void BuscarAlvo()
    {
        if (target != null)
        {
            return;
        }

        GameObject jogador = Jogador.Encontrar();
        if (jogador != null)
        {
            target = jogador.transform;
        }
    }

    // Update is called once per frame
    // Com o jogo em andamento: segue o player, recicla o inimigo que ficou longe, mira a arma
    // e atira (só dentro da tela).
    private void Update()
    {
        GameManager gm = ObterGameManager();
        if(gm == null || gm.gameState != GameManager.GameState.OnPlay)
        {
            return;
        }

        SeguirJogador();

        // Ficou para trás (o player é mais rápido): reaparece logo fora da tela, na frente do player.
        // Neste frame não mira nem atira.
        if (ReciclarSeEstiverLonge())
        {
            return;
        }

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

    // Inimigo esquecido longe reaparece do lado oposto da tela. É o MESMO objeto: continua no "vivos" do
    // GerenciadorDeWaves, com a vida (e o bônus da wave), e não dispara AoMorrer (sem gema, drop nem abate).
    // O boss não tem EnemyMove, então nunca é reciclado.
    private bool ReciclarSeEstiverLonge()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                return false;
            }
        }

        float altura = cam.orthographicSize;
        float largura = altura * cam.aspect;
        Vector2 deslocamento = transform.position - cam.transform.position;

        bool longe = Mathf.Abs(deslocamento.x) > largura + distanciaReciclar
                  || Mathf.Abs(deslocamento.y) > altura + distanciaReciclar;
        if (!longe)
        {
            tempoLonge = 0f;
            return false;
        }

        tempoLonge += Time.deltaTime;
        if (tempoLonge < tempoParaReciclar)
        {
            return false;
        }

        tempoLonge = 0f;
        transform.position = EnemySpawn.PontoForaDaTela(cam, LadoOposto(deslocamento, largura, altura), margemReaparecer);
        liberaTiroEm = -1f;
        return true;
    }

    // Lado da tela oposto ao lado em que o inimigo ficou (ficou em cima = o player foi para baixo = reaparece embaixo).
    // Compara as distâncias proporcionais à metade da tela, para a tela larga (16:9) não favorecer os lados.
    private static int LadoOposto(Vector2 deslocamento, float largura, float altura)
    {
        float proporcaoX = Mathf.Abs(deslocamento.x) / Mathf.Max(0.01f, largura);
        float proporcaoY = Mathf.Abs(deslocamento.y) / Mathf.Max(0.01f, altura);

        if (proporcaoX > proporcaoY)
        {
            return deslocamento.x > 0f ? EnemySpawn.LadoEsquerda : EnemySpawn.LadoDireita;
        }

        return deslocamento.y > 0f ? EnemySpawn.LadoBaixo : EnemySpawn.LadoCima;
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
                CriarExplosao();
                Destroy(gameObject);
            }
        }
    }

    // Integração (VFX): cria a explosão onde o inimigo morreu e a apaga quando as partículas acabam
    // (o prefab Explosion não se destrói sozinho).
    private void CriarExplosao()
    {
        if (explosaoPrefab == null)
        {
            return;
        }

        GameObject explosao = Instantiate(explosaoPrefab, transform.position, Quaternion.identity);
        Destroy(explosao, DuracaoDasParticulas(explosao));
    }

    // Tempo até a última partícula sumir: o maior (atraso + duração + vida da partícula) entre os ParticleSystems do objeto.
    private static float DuracaoDasParticulas(GameObject objeto)
    {
        float duracao = 0f;
        foreach (ParticleSystem particulas in objeto.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule principal = particulas.main;
            float total = principal.startDelay.constantMax + principal.duration + principal.startLifetime.constantMax;
            duracao = Mathf.Max(duracao, total);
        }

        return duracao > 0f ? duracao : DuracaoPadraoExplosao;
    }

    // Atira um tiro na direção do player (se há alvo, prefab e arma) e depois espera fireHate para poder atirar de novo.
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

            AoAtirar?.Invoke(this);
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

        transform.position = Vector2.MoveTowards(transform.position, target.position, velocidade * Time.deltaTime);
    }
}
