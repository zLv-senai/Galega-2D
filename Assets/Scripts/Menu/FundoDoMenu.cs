using System.Collections.Generic;
using UnityEngine;

// Gameplay decorativa atrás do menu principal: a nave real voa sozinha pela metade direita da tela, atira com o tiro
// real (só o visual) nos inimigos falsos que nascem pela direita, por cima e por baixo, e os explode. É só visual
// (sem colliders nem scripts de jogo) e usa o tempo real, porque o menu roda com Time.timeScale = 0. O MenuManager cria
// este objeto e chama Encerrar() para devolver player e câmera ao estado original.
public sealed class FundoDoMenu : MonoBehaviour
{
    private const int MaxInimigos = 8;
    private const float VelocidadeDoTiro = 12f;
    private const float IntervaloDoTiro = 0.25f;
    private const float RaioDeAcerto = 0.45f;
    private const float IntervaloEntreInimigos = 0.6f;
    private const float IntervaloEntreBosses = 30f;
    private const float VelocidadeDoInimigo = 2.2f;
    private const float VelocidadeDoBoss = 1.1f;
    private const float AmplitudeDoSeno = 1.4f;
    private const float FrequenciaDoSeno = 1.6f;
    private const float SuavizacaoDaMira = 10f;

    // Mesmos prefabs que o EnemySpawn e o ControladorDeBoss usam por padrão (Assets/Resources).
    private static readonly string[] InimigosNoResources = { "EnemyUno", "EnemyDuo", "EnemyThree" };
    private const string TiroNoResources = "Tiro";
    private const string TiroRealNoResources = "ShootPlayer";
    private static readonly string[] BossesNoResources = { "BOSS1(FINAL)", "BOSS2(FINAL)", "BOSS3(FINAL)" };

    // Aparência copiada do SpriteRenderer de um prefab (o prefab em si nunca é instanciado).
    private sealed class Visual
    {
        public Sprite sprite;
        public Color cor;
        public Material material;
        public Vector3 escala;
        public int camadaId;
        public int ordem;
    }

    private sealed class InimigoFalso
    {
        public Transform corpo;
        public Vector2 posicaoBase;
        public Vector2 direcao;
        public Vector2 perpendicular;
        public float fase;
        public float velocidade;
        public float idade;
        public bool vivo = true;
    }

    private sealed class TiroFalso
    {
        public Transform corpo;
        public InimigoFalso alvo;
    }

    private readonly List<InimigoFalso> inimigos = new List<InimigoFalso>();
    private readonly List<TiroFalso> tiros = new List<TiroFalso>();
    private readonly List<Visual> visuaisDeInimigo = new List<Visual>();
    private readonly List<Visual> visuaisDeBoss = new List<Visual>();
    private Visual visualDoTiro;          // plano B (sprite) quando não há prefab de partículas
    private GameObject tiroReal;          // prefab de partículas do player
    private Transform sala;               // pai inativo: a instância só ativa depois de ficar só visual
    private Camera camera2D;
    private CameraFollow seguidor;
    private Transform player;

    private Vector3 posicaoOriginalDoPlayer;
    private Quaternion rotacaoOriginalDoPlayer;
    private Vector3 escalaOriginalDoPlayer;
    private Vector3 posicaoOriginalDaCamera;
    private bool seguidorEstavaLigado;
    private bool estadoGuardado;
    private bool restaurado;

    private Vector3 centroDaCamera;
    private float tempo;
    private float tempoAteProximoInimigo;
    private float tempoAteBoss = IntervaloEntreBosses;
    private float tempoAteProximoTiro;

    private void Start()
    {
        camera2D = Camera.main;
        PlayerMove jogador = FindAnyObjectByType<PlayerMove>();
        player = jogador != null ? jogador.transform : null;
        seguidor = camera2D != null ? camera2D.GetComponent<CameraFollow>() : null;

        CarregarVisuais(InimigosNoResources, visuaisDeInimigo);
        CarregarVisuais(BossesNoResources, visuaisDeBoss);
        tiroReal = jogador != null && jogador.tiroPrefab != null ? jogador.tiroPrefab : Resources.Load<GameObject>(TiroRealNoResources);
        if (tiroReal == null)
        {
            visualDoTiro = LerVisual(Resources.Load<GameObject>(TiroNoResources));
            Debug.LogWarning("FundoDoMenu: sem prefab de tiro real; usando o sprite do \"Tiro\" de Resources.");
        }

        List<string> faltando = new List<string>();
        if (camera2D == null) faltando.Add("câmera (Camera.main)");
        if (player == null) faltando.Add("player (PlayerMove)");
        if (visuaisDeInimigo.Count == 0) faltando.Add("inimigos (Resources)");
        if (tiroReal == null && visualDoTiro == null) faltando.Add("tiro");

        if (faltando.Count > 0)
        {
            Debug.LogWarning("FundoDoMenu: faltou " + string.Join(", ", faltando) + ". A gameplay do fundo foi desligada.");
            enabled = false;
            return;
        }

        sala = new GameObject("TiroEmPreparo").transform;
        sala.SetParent(transform, false);
        sala.gameObject.SetActive(false);

        posicaoOriginalDoPlayer = player.position;
        rotacaoOriginalDoPlayer = player.rotation;
        escalaOriginalDoPlayer = player.localScale;
        posicaoOriginalDaCamera = camera2D.transform.position;
        centroDaCamera = posicaoOriginalDaCamera;
        centroDaCamera.z = 0f;
        if (seguidor != null)
        {
            seguidorEstavaLigado = seguidor.enabled;
            seguidor.enabled = false;
        }

        estadoGuardado = true;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.gameState != GameManager.GameState.Menu)
        {
            Encerrar();
            return;
        }

        float dt = Time.unscaledDeltaTime;
        tempo += dt;
        MoverNave(dt);
        GerarInimigos(dt);
        MoverInimigos(dt);
        AtirarNoMaisProximo(dt);
        MoverTiros(dt);
    }

    // Some no mesmo frame: devolve player e câmera ao que eram, para a EntradaDaNave ler a posição certa.
    public void Encerrar()
    {
        enabled = false;
        foreach (InimigoFalso inimigo in inimigos) Destroy(inimigo.corpo.gameObject);
        foreach (TiroFalso tiro in tiros) Destroy(tiro.corpo.gameObject);
        inimigos.Clear();
        tiros.Clear();
        Restaurar();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Restaurar();
    }

    private void Restaurar()
    {
        if (restaurado || !estadoGuardado)
        {
            return;
        }

        restaurado = true;
        if (player != null)
        {
            player.SetPositionAndRotation(posicaoOriginalDoPlayer, rotacaoOriginalDoPlayer);
            player.localScale = escalaOriginalDoPlayer;
            foreach (TrailRenderer rastro in player.GetComponentsInChildren<TrailRenderer>(true)) rastro.Clear();
        }

        if (camera2D != null)
        {
            camera2D.transform.position = posicaoOriginalDaCamera;
        }

        if (seguidor != null)
        {
            seguidor.enabled = seguidorEstavaLigado;
        }
    }

    // Trajetória suave na metade direita/central (planeta à esquerda, caixa de descrição embaixo); gira para o alvo.
    private void MoverNave(float dt)
    {
        Vector3 posicao = centroDaCamera + new Vector3(2.5f + 2.8f * Mathf.Sin(tempo * 0.45f), 2.4f * Mathf.Sin(tempo * 0.8f), 0f);
        posicao.z = player.position.z;
        player.position = posicao;

        InimigoFalso alvo = MaisProximoDe(player.position);
        Vector2 direcao = alvo != null ? (Vector2)(alvo.corpo.position - player.position) : Vector2.right;
        // Convenção do PlayerMove/LookAt: Atan2(y, x), 0 grau = direita.
        Quaternion desejada = Quaternion.Euler(0f, 0f, Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg);
        player.rotation = Quaternion.Slerp(player.rotation, desejada, 1f - Mathf.Exp(-SuavizacaoDaMira * dt));
    }

    private void GerarInimigos(float dt)
    {
        Bounds tela = LimitesDaTela();
        tempoAteProximoInimigo -= dt;
        if (tempoAteProximoInimigo <= 0f && inimigos.Count < MaxInimigos)
        {
            CriarInimigo(visuaisDeInimigo, PontoDeEntrada(tela), VelocidadeDoInimigo);
            tempoAteProximoInimigo = IntervaloEntreInimigos;
        }

        tempoAteBoss -= dt;
        if (tempoAteBoss <= 0f && visuaisDeBoss.Count > 0 && inimigos.Count < MaxInimigos)
        {
            CriarInimigo(visuaisDeBoss, new Vector2(tela.max.x + 1f, tela.center.y), VelocidadeDoBoss);
            tempoAteBoss = IntervaloEntreBosses;
        }
    }

    // Direita, em cima ou embaixo, sempre à direita do planeta.
    private static Vector2 PontoDeEntrada(Bounds tela)
    {
        float xDireito = tela.center.x + tela.extents.x * 0.15f;
        switch (Random.Range(0, 3))
        {
            case 0: return new Vector2(Random.Range(xDireito, tela.max.x), tela.max.y + 1f);
            case 1: return new Vector2(Random.Range(xDireito, tela.max.x), tela.min.y - 1f);
            default: return new Vector2(tela.max.x + 1f, Random.Range(tela.min.y + 1f, tela.max.y - 1f));
        }
    }

    private void CriarInimigo(List<Visual> opcoes, Vector2 origem, float velocidade)
    {
        Visual visual = opcoes[Random.Range(0, opcoes.Count)];
        Transform corpo = CriarSprite("Inimigo falso", visual, origem, Quaternion.identity);
        Vector2 destino = (Vector2)centroDaCamera + new Vector2(2.5f + Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f));
        Vector2 direcao = (destino - origem).normalized;
        inimigos.Add(new InimigoFalso
        {
            corpo = corpo,
            posicaoBase = origem,
            direcao = direcao,
            perpendicular = new Vector2(-direcao.y, direcao.x),
            fase = Random.value * 6.28f,
            velocidade = velocidade,
        });
    }

    // Anda até a área da nave com movimento senoidal; some ao sair da tela (ou ao chegar perto do planeta).
    private void MoverInimigos(float dt)
    {
        Bounds tela = LimitesDaTela();
        float limiteEsquerdo = tela.center.x - tela.extents.x * 0.1f;
        Bounds margem = tela;
        margem.Expand(2f);

        for (int i = inimigos.Count - 1; i >= 0; i--)
        {
            InimigoFalso inimigo = inimigos[i];
            inimigo.idade += dt;
            inimigo.posicaoBase += inimigo.direcao * (inimigo.velocidade * dt);
            float onda = AmplitudeDoSeno * Mathf.Sin(tempo * FrequenciaDoSeno + inimigo.fase);
            Vector2 posicao = inimigo.posicaoBase + inimigo.perpendicular * onda;
            inimigo.corpo.position = new Vector3(posicao.x, posicao.y, 0f);

            if (posicao.x < limiteEsquerdo || (inimigo.idade > 1f && !margem.Contains(inimigo.corpo.position)))
            {
                Remover(inimigo);
            }
        }
    }

    private void AtirarNoMaisProximo(float dt)
    {
        tempoAteProximoTiro -= dt;
        if (tempoAteProximoTiro > 0f || inimigos.Count == 0)
        {
            return;
        }

        tempoAteProximoTiro = IntervaloDoTiro;
        InimigoFalso alvo = MaisProximoDe(player.position);
        Vector3 direcao = alvo.corpo.position - player.position;
        Quaternion rotacao = Quaternion.LookRotation(Vector3.forward, direcao);
        Transform corpo = tiroReal != null ? CriarTiroReal(player.position, rotacao) : CriarSprite("Tiro falso", visualDoTiro, player.position, rotacao);
        tiros.Add(new TiroFalso { corpo = corpo, alvo = alvo });
    }

    // Instancia o prefab de partículas do player só como visual: sem scripts, física nem rastro, e com tempo real.
    private Transform CriarTiroReal(Vector3 posicao, Quaternion rotacao)
    {
        GameObject objeto = Instantiate(tiroReal, sala);   // dentro do pai inativo: Awake/OnEnable só rodam depois
        foreach (MonoBehaviour script in objeto.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
        foreach (Collider2D colisor in objeto.GetComponentsInChildren<Collider2D>(true)) colisor.enabled = false;
        foreach (Rigidbody2D corpoRigido in objeto.GetComponentsInChildren<Rigidbody2D>(true)) corpoRigido.simulated = false;
        // TrailRenderer não tem tempo real: com timeScale 0 o rastro nunca some, então fica desligado.
        foreach (TrailRenderer rastro in objeto.GetComponentsInChildren<TrailRenderer>(true)) rastro.enabled = false;
        foreach (ParticleSystem particulas in objeto.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule principal = particulas.main;
            principal.useUnscaledTime = true;
        }

        objeto.name = "Tiro falso";
        objeto.transform.SetParent(transform, false);
        objeto.transform.SetPositionAndRotation(posicao, rotacao);
        return objeto.transform;
    }

    // Os tiros perseguem o alvo; ao chegar, os dois somem numa explosão (sem som).
    private void MoverTiros(float dt)
    {
        Bounds tela = LimitesDaTela();
        tela.Expand(2f);

        for (int i = tiros.Count - 1; i >= 0; i--)
        {
            TiroFalso tiro = tiros[i];
            if (tiro.alvo == null || !tiro.alvo.vivo)
            {
                tiro.alvo = inimigos.Count > 0 ? MaisProximoDe(tiro.corpo.position) : null;
            }

            if (tiro.alvo == null || !tela.Contains(tiro.corpo.position))
            {
                Destroy(tiro.corpo.gameObject);
                tiros.RemoveAt(i);
                continue;
            }

            Vector3 ate = tiro.alvo.corpo.position - tiro.corpo.position;
            if (ate.magnitude <= RaioDeAcerto)
            {
                EfeitoDeExplosao.Criar(tiro.alvo.corpo.position, 1f);
                Remover(tiro.alvo);
                Destroy(tiro.corpo.gameObject);
                tiros.RemoveAt(i);
                continue;
            }

            tiro.corpo.rotation = Quaternion.LookRotation(Vector3.forward, ate);
            tiro.corpo.position += ate.normalized * (VelocidadeDoTiro * dt);
        }
    }

    private InimigoFalso MaisProximoDe(Vector3 ponto)
    {
        InimigoFalso melhor = null;
        float menorDistancia = float.MaxValue;
        foreach (InimigoFalso inimigo in inimigos)
        {
            float distancia = (inimigo.corpo.position - ponto).sqrMagnitude;
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                melhor = inimigo;
            }
        }

        return melhor;
    }

    private void Remover(InimigoFalso inimigo)
    {
        inimigo.vivo = false;
        inimigos.Remove(inimigo);
        Destroy(inimigo.corpo.gameObject);
    }

    // Usa o centro guardado da câmera: ela fica parada, então a tela não depende da posição atual.
    private Bounds LimitesDaTela()
    {
        float altura = camera2D.orthographicSize * 2f;
        return new Bounds(centroDaCamera, new Vector3(altura * camera2D.aspect, altura, 1f));
    }

    private Transform CriarSprite(string nome, Visual visual, Vector3 posicao, Quaternion rotacao)
    {
        GameObject objeto = new GameObject(nome);
        objeto.transform.SetParent(transform, false);
        objeto.transform.SetPositionAndRotation(posicao, rotacao);
        objeto.transform.localScale = visual.escala;

        SpriteRenderer renderizador = objeto.AddComponent<SpriteRenderer>();
        renderizador.sprite = visual.sprite;
        renderizador.color = visual.cor;
        renderizador.sharedMaterial = visual.material;
        renderizador.sortingLayerID = visual.camadaId;
        renderizador.sortingOrder = visual.ordem;
        return objeto.transform;
    }

    private static void CarregarVisuais(string[] nomes, List<Visual> destino)
    {
        foreach (string nome in nomes)
        {
            Visual visual = LerVisual(Resources.Load<GameObject>(nome));
            if (visual != null)
            {
                destino.Add(visual);
            }
        }
    }

    private static Visual LerVisual(GameObject prefab)
    {
        SpriteRenderer renderizador = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>() : null;
        if (renderizador == null || renderizador.sprite == null)
        {
            return null;
        }

        return new Visual
        {
            sprite = renderizador.sprite,
            cor = renderizador.color,
            material = renderizador.sharedMaterial,
            escala = renderizador.transform.lossyScale,
            camadaId = renderizador.sortingLayerID,
            ordem = renderizador.sortingOrder,
        };
    }
}
