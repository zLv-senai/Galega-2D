using UnityEngine;

// Integração: Rondar foi adicionado NO FIM do enum (o padrão é salvo como número nas cenas).
public enum PadraoBoss { Oscilar, Seguir, Rondar }

public class BossController : MonoBehaviour, IDamageable
// Este script controla o comportamento do boss, incluindo movimento, tiro e vida. Ele implementa a interface IDamageable para receber dano de tiros.
{
    // Integração: tag dos inimigos (a mesma do EnemyMove). O Shot ignora quem tem a mesma tag de quem atirou,
    // então o tiro do boss não acerta inimigos nem o próprio boss.
    private const string TagInimigo = "Inimigo";

    // Integração: Rondar. A volta é suavizada (Lerp) e limitada a velocidade * este valor (o boss nunca "teleporta").
    private const float SuavizacaoRondar = 3f;
    private const float MultiplicadorVelocidadeRondar = 4f;

    // Integração: sem alvo, procura o player de novo a cada tanto de segundos (e não todo frame).
    private const float IntervaloBuscaAlvo = 1f;

    // Integração: avisado quando QUALQUER boss morre (ControladorDeBoss, GerenciadorDeSom). Evento estático:
    // quem assinar precisa cancelar no OnDisable.
    public static event System.Action<BossController> AoMorrer;

    [Header("Referências")]
    // Integração: mantido por compatibilidade com a cena do Samuel. O estado do jogo agora vem de EstadoDoJogo.
    public GameManager gameManager;
    public Transform target;
    public GameObject tiroPrefab;
    public Transform gun;

    [Header("Vida")]
    public int vida = 20;

    [Header("Movimento")]
    public PadraoBoss padrao = PadraoBoss.Oscilar;
    public float velocidade = 2f;
    public float amplitudeX = 3f;
    public float amplitudeY = 0f;
    // Integração: padrão Rondar: distância que o boss mantém do player enquanto gira em volta dele.
    public float distanciaRondar = 5f;

    [Header("Tiro")]
    public float intervaloTiro = 1.5f;
    public int quantidadeTiros = 3;
    public float anguloEntreTiros = 15f;
    public float anguloBase = 0f;
    // Integração: true = o leque de tiros é centrado na direção do player; false = usa a rotação da arma (como antes).
    public bool mirarNoPlayer = true;

    private Vector3 posicaoInicial;
    private float tempo;
    private float timerTiro;

    // Integração: valores de fábrica guardados no Awake, para o Fortalecer sempre partir deles (não acumula se chamado 2x).
    private int vidaBase;
    private int vidaMaxima;
    private float intervaloTiroBase;
    private int quantidadeTirosBase;
    private float velocidadeBase;

    // Integração: estado do padrão Rondar, da busca do alvo e da morte.
    private float anguloRondar;
    private bool rondaIniciada;
    private float proximaBuscaAlvo;
    private bool morreu;

    // Integração: para a barra de vida do boss no HUD ("vida" já é pública, e este é o valor máximo atual).
    public int VidaMaxima => vidaMaxima;

    void Awake()
    {
        vidaBase = vida;
        vidaMaxima = vida;
        intervaloTiroBase = intervaloTiro;
        quantidadeTirosBase = quantidadeTiros;
        velocidadeBase = velocidade;
    }

    void Start()
    {
        posicaoInicial = transform.position;

        // Integração: boss criado por código (ControladorDeBoss) não tem nada arrastado.
        BuscarAlvo();

        if (gun == null)
        {
            gun = transform;
        }
    }

    void Update()
    {
        // Integração: antes era gameManager.gameState == OnPlay (quebrava sem o gameManager arrastado).
        if (EstadoDoJogo.Rodando)
        {
            Mover();
            Atirar();
            //Isso garante que o boss só se move e atira quando o jogo está em andamento, evitando que ele continue agindo durante pausas ou menus.
        }
    }

    // Integração: deixa o boss mais forte conforme o número dele na partida (0 = primeiro boss).
    // Sempre parte dos valores de fábrica, então pode ser chamado uma vez logo depois de criar o boss.
    public void Fortalecer(int indice)
    {
        indice = Mathf.Max(0, indice);

        vidaMaxima = Mathf.Max(1, Mathf.RoundToInt(vidaBase * (1f + 0.5f * indice)));
        vida = vidaMaxima;
        intervaloTiro = intervaloTiroBase * Mathf.Pow(0.8f, indice);
        quantidadeTiros = quantidadeTirosBase + 2 * indice;
        velocidade = velocidadeBase * (1f + 0.1f * indice);
    }

    void BuscarAlvo()
    {
        if (target != null)
        {
            return;
        }

        PlayerMove jogador = FindAnyObjectByType<PlayerMove>();
        if (jogador != null)
        {
            target = jogador.transform;
        }
    }

    // Integração: o alvo pode faltar (player não achado no Start, ou destruído): tenta de novo 1x por segundo.
    bool TemAlvo()
    {
        if (target != null)
        {
            return true;
        }

        if (Time.time < proximaBuscaAlvo)
        {
            return false;
        }

        proximaBuscaAlvo = Time.time + IntervaloBuscaAlvo;
        BuscarAlvo();
        return target != null;
    }

    void Mover()
    {
        switch (padrao)
        {
            case PadraoBoss.Oscilar:
                tempo += Time.deltaTime * velocidade;

                float offsetX = Mathf.Sin(tempo) * amplitudeX;
                float offsetY = Mathf.Cos(tempo) * amplitudeY;

                // O Z fica 0 para não mudar a profundidade do objeto na tela.
                transform.position = posicaoInicial + new Vector3(offsetX, offsetY, 0f);
                break;

            case PadraoBoss.Seguir:
                // Integração: sem alvo, fica parado (antes dava NullReferenceException).
                if (!TemAlvo())
                {
                    break;
                }

                transform.position = Vector2.MoveTowards(transform.position, target.position, velocidade * Time.deltaTime);
                break;

            // Integração: novo padrão. Gira em volta do player a distanciaRondar; sem alvo, para.
            case PadraoBoss.Rondar:
                if (!TemAlvo())
                {
                    break;
                }

                Rondar();
                break;
                // Se quiser adicionar mais padrões de movimento, é só colocar mais cases aqui.
        }
    }

    // Integração: segue um ponto que gira em volta do player. O ângulo inicial é o da posição atual do boss
    // (assim ele chega do lugar onde nasceu, sem pular para outro lado).
    void Rondar()
    {
        if (!rondaIniciada)
        {
            Vector2 relativa = transform.position - target.position;
            anguloRondar = relativa.sqrMagnitude > 0.0001f ? Mathf.Atan2(relativa.y, relativa.x) : 0f;
            rondaIniciada = true;
        }

        float raio = Mathf.Max(0.1f, distanciaRondar);

        // velocidade (unidades por segundo) vira velocidade angular (rad/s): a volta tem sempre a mesma velocidade na borda.
        anguloRondar += (velocidade / raio) * Time.deltaTime;

        Vector3 centro = target.position;
        Vector3 desejada = new Vector3(
            centro.x + Mathf.Cos(anguloRondar) * raio,
            centro.y + Mathf.Sin(anguloRondar) * raio,
            transform.position.z);

        Vector3 suave = Vector3.Lerp(transform.position, desejada, 1f - Mathf.Exp(-SuavizacaoRondar * Time.deltaTime));
        transform.position = Vector3.MoveTowards(transform.position, suave, velocidade * MultiplicadorVelocidadeRondar * Time.deltaTime);
    }

    void Atirar()
    {
        // Integração: só conta o tempo do tiro com o boss dentro da tela
        // (ele nasce fora da câmera e não deve atirar de onde o player não vê).
        if (!EstaNaTela())
        {
            return;
        }

        timerTiro += Time.deltaTime;

        if (timerTiro >= intervaloTiro)
        {
            timerTiro = 0f;
            Disparar();
            // Se quiser adicionar mais efeitos de tiro, é só colocar mais cases aqui.
        }
    }

    private bool EstaNaTela()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return true;
        }

        Vector3 vp = cam.WorldToViewportPoint(transform.position);
        return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    // Integração: rotação base do leque. Com mirarNoPlayer, o "up" aponta para o player (como o EnemyMove faz);
    // sem alvo ou com mirarNoPlayer desligado, usa a rotação da arma como antes.
    Quaternion RotacaoDeMira()
    {
        if (mirarNoPlayer && target != null)
        {
            Vector2 direcao = target.position - gun.position;
            if (direcao.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(Vector3.forward, direcao);
            }
        }

        return gun.rotation;
    }

    void Disparar()
    {
        // Integração: sem prefab de tiro ou sem arma não há o que disparar.
        if (tiroPrefab == null || gun == null)
        {
            return;
        }

        // Ângulo do primeiro tiro, para o leque ficar centralizado na mira da arma.
        float anguloInicial = -((quantidadeTiros - 1) * anguloEntreTiros / 2f);
        Quaternion mira = RotacaoDeMira();

        for (int i = 0; i < quantidadeTiros; i++)
        {
            float angulo = anguloInicial + (i * anguloEntreTiros) + anguloBase;

            // Multiplicar rotações = gira a mira por mais este ângulo.
            Quaternion rotacao = mira * Quaternion.Euler(0, 0, angulo);
            GameObject tiro = Instantiate(tiroPrefab, gun.position, rotacao);
          //Isso faz com que o tiro seja instanciado na posição da arma, com a rotação da mira mais o ângulo calculado para cada tiro do leque.

            // Integração: tiro de inimigo (não acerta inimigos nem explode minas), igual ao EnemyMove.
            Shot shot = tiro.GetComponent<Shot>();
            if (shot != null)
            {
                shot.Configurar(TagInimigo, false);
            }
        }
    }

    public void TakeDamage(int dano)
    {
        // Integração: já morreu (ou será destruído no fim deste frame): evita Destroy/AoMorrer duplicados.
        if (morreu)
        {
            return;
        }

        vida -= dano;

        if (vida < 1)
        {
            morreu = true;

            // try/finally: o Destroy acontece mesmo se um assinante de AoMorrer lançar exceção (igual ao EnemyMove).
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
    // O método TakeDamage é chamado quando o boss leva dano. Ele diminui a vida do boss e, se a vida chegar a zero, destrói o objeto do boss.
}
