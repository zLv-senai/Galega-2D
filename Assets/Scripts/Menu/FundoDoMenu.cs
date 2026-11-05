using System.Collections.Generic;
using UnityEngine;

// Gameplay decorativa atrás do menu principal: inimigos falsos entram pela direita em ondas, e tiros falsos saem do
// player real até o inimigo mais próximo e o explodem. É só visual (sem colliders nem scripts de jogo) e usa o tempo
// real, porque o menu roda com Time.timeScale = 0. O MenuManager cria este objeto e ele some sozinho ao sair do Menu.
public sealed class FundoDoMenu : MonoBehaviour
{
    private const int MaxInimigos = 8;
    private const float VelocidadeDoTiro = 12f;
    private const float IntervaloDoTiro = 0.25f;
    private const float RaioDeAcerto = 0.45f;
    private const float IntervaloEntreOndas = 4f;
    private const float IntervaloEntreInimigos = 0.5f;
    private const float IntervaloEntreBosses = 30f;
    private const float VelocidadeDoInimigo = 2.2f;
    private const float VelocidadeDoBoss = 1.1f;
    private const float AmplitudeDoSeno = 1.4f;
    private const float FrequenciaDoSeno = 1.6f;

    // Mesmos prefabs que o EnemySpawn e o ControladorDeBoss usam por padrão (Assets/Resources).
    private static readonly string[] InimigosNoResources = { "EnemyUno", "EnemyDuo", "EnemyThree" };
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
        public float yBase;
        public float fase;
        public float velocidade;
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
    private Visual visualDoTiro;
    private Camera camera2D;
    private Transform player;

    private int restanteDaOnda;
    private float yDaOnda;
    private float tempoAteNovaOnda;
    private float tempoAteProximoInimigo;
    private float tempoAteBoss = IntervaloEntreBosses;
    private float tempoAteProximoTiro;

    private void Start()
    {
        camera2D = Camera.main;
        PlayerMove jogador = FindFirstObjectByType<PlayerMove>();
        player = jogador != null ? jogador.transform : null;

        CarregarVisuais(InimigosNoResources, visuaisDeInimigo);
        CarregarVisuais(BossesNoResources, visuaisDeBoss);
        visualDoTiro = jogador != null ? LerVisual(jogador.tiroPrefab) : null;

        if (camera2D == null || player == null || visuaisDeInimigo.Count == 0 || visualDoTiro == null)
        {
            Debug.LogWarning("FundoDoMenu: câmera, player, inimigos ou tiro não encontrados. A gameplay do fundo foi desligada.");
            enabled = false;
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.gameState != GameManager.GameState.Menu)
        {
            Destroy(gameObject);
            return;
        }

        float dt = Time.unscaledDeltaTime;
        GerarInimigos(dt);
        MoverInimigos(dt);
        AtirarNoMaisProximo(dt);
        MoverTiros(dt);
    }

    private void GerarInimigos(float dt)
    {
        Bounds tela = LimitesDaTela();
        tempoAteNovaOnda -= dt;
        if (restanteDaOnda == 0 && tempoAteNovaOnda <= 0f)
        {
            restanteDaOnda = Random.Range(4, 7);
            yDaOnda = Random.Range(tela.min.y + 1.5f, tela.max.y - 1.5f);
            tempoAteNovaOnda = IntervaloEntreOndas;
        }

        tempoAteProximoInimigo -= dt;
        if (restanteDaOnda > 0 && tempoAteProximoInimigo <= 0f && inimigos.Count < MaxInimigos)
        {
            CriarInimigo(visuaisDeInimigo, yDaOnda, VelocidadeDoInimigo, tela);
            restanteDaOnda--;
            tempoAteProximoInimigo = IntervaloEntreInimigos;
        }

        tempoAteBoss -= dt;
        if (tempoAteBoss <= 0f && visuaisDeBoss.Count > 0 && inimigos.Count < MaxInimigos)
        {
            CriarInimigo(visuaisDeBoss, tela.center.y, VelocidadeDoBoss, tela);
            tempoAteBoss = IntervaloEntreBosses;
        }
    }

    private void CriarInimigo(List<Visual> opcoes, float y, float velocidade, Bounds tela)
    {
        Visual visual = opcoes[Random.Range(0, opcoes.Count)];
        Transform corpo = CriarSprite("Inimigo falso", visual, new Vector3(tela.max.x + 1f, y, 0f), Quaternion.identity);
        inimigos.Add(new InimigoFalso { corpo = corpo, yBase = y, fase = Random.value * 6.28f, velocidade = velocidade });
    }

    // Anda para a esquerda com movimento senoidal e some no meio da tela (a metade esquerda é do menu).
    private void MoverInimigos(float dt)
    {
        Bounds tela = LimitesDaTela();
        float limiteEsquerdo = tela.center.x - tela.extents.x * 0.1f;

        for (int i = inimigos.Count - 1; i >= 0; i--)
        {
            InimigoFalso inimigo = inimigos[i];
            Vector3 posicao = inimigo.corpo.position;
            posicao.x -= inimigo.velocidade * dt;
            posicao.y = inimigo.yBase + AmplitudeDoSeno * Mathf.Sin(Time.unscaledTime * FrequenciaDoSeno + inimigo.fase);
            inimigo.corpo.position = posicao;

            if (posicao.x < limiteEsquerdo)
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
        Transform corpo = CriarSprite("Tiro falso", visualDoTiro, player.position, Quaternion.LookRotation(Vector3.forward, direcao));
        tiros.Add(new TiroFalso { corpo = corpo, alvo = alvo });
    }

    // Os tiros perseguem o alvo; ao chegar, os dois somem numa explosão (sem som).
    private void MoverTiros(float dt)
    {
        for (int i = tiros.Count - 1; i >= 0; i--)
        {
            TiroFalso tiro = tiros[i];
            if (tiro.alvo == null || !tiro.alvo.vivo)
            {
                tiro.alvo = inimigos.Count > 0 ? MaisProximoDe(tiro.corpo.position) : null;
            }

            if (tiro.alvo == null)
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

    private Bounds LimitesDaTela()
    {
        float altura = camera2D.orthographicSize * 2f;
        Vector3 centro = camera2D.transform.position;
        return new Bounds(new Vector3(centro.x, centro.y, 0f), new Vector3(altura * camera2D.aspect, altura, 1f));
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
