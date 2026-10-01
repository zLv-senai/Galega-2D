using UnityEngine;
using UnityEngine.UIElements;

// Seta na borda da tela que aponta para o boss quando ele está vivo e fora da câmera.
// Fica no objeto "Hud", junto do HudProgressao (mesmo PanelRenderer), e usa o elemento "SetaBoss" do
// HudProgressao.uxml. Some quando o boss entra na tela, quando morre (ou é cancelado) e fora de OnPlay/Pause.
// A forma é desenhada com Painter2D (não depende de a fonte ter o caractere "▲").
// DefaultExecutionOrder alto: o LateUpdate roda depois do CameraFollow, com a câmera já no lugar deste frame.
[DefaultExecutionOrder(1000)]
public class SetaDoBoss : MonoBehaviour
{
    private const string NomeElemento = "SetaBoss";

    // Pulso de tamanho da seta (1 ciclo = PeriodoPulso segundos; vai de 1 até 1 + AmplitudePulso).
    private const float PeriodoPulso = 0.6f;
    private const float AmplitudePulso = 0.15f;

    // Tamanho usado quando o boss não tem Renderer (caixa de 1x1 unidade em volta do centro).
    private static readonly Vector3 TamanhoReservaDoBoss = Vector3.one;

    [SerializeField] private PanelRenderer painel;                // vazio = o PanelRenderer do mesmo objeto
    [SerializeField] private ControladorDeBoss controladorBoss;   // vazio = procura na cena

    [Header("Aparência (px do painel: referência de 1200 de largura)")]
    [SerializeField] private float tamanhoSeta = 44f;
    [SerializeField] private float margemBorda = 40f;   // distância até as bordas esquerda, direita e de baixo
    [SerializeField] private float margemTopo = 96f;    // maior no topo para não cobrir a barra de vida do boss
    [SerializeField] private Color corSeta = new Color(1f, 0.27f, 0.2f, 1f);
    [SerializeField] private Color corContorno = new Color(0.35f, 0f, 0f, 1f);

    private VisualElement seta;
    private Camera cam;
    private BossController bossDoRenderer;
    private Renderer rendererDoBoss;
    private bool estadoPermite = true;
    private bool visivel = true; // true para o primeiro Mostrar(false) realmente escrever o estilo

    private void Awake()
    {
        if (painel == null)
        {
            painel = GetComponent<PanelRenderer>();
        }

        if (painel != null)
        {
            painel.RegisterUIReloadCallback(OnUIReload);
        }
        else
        {
            Debug.LogWarning("SetaDoBoss: PanelRenderer não encontrado. Coloque este script no objeto do HudProgressao.", this);
        }
    }

    private void OnDestroy()
    {
        if (painel != null)
        {
            painel.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnEnable()
    {
        // Evento estático: precisa cancelar no OnDisable.
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
        if (GameManager.Instance != null)
        {
            TratarMudancaDeEstado(GameManager.Instance.gameState);
        }
    }

    private void OnDisable()
    {
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
        Mostrar(false);
    }

    private void Start()
    {
        if (controladorBoss == null)
        {
            controladorBoss = FindAnyObjectByType<ControladorDeBoss>();
        }

        cam = Camera.main;
    }

    // Só em OnPlay e Pause (no Pause a seta fica parada no lugar, como o resto do HUD).
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        estadoPermite = estado == GameManager.GameState.OnPlay || estado == GameManager.GameState.Pause;
    }

    private void OnUIReload(PanelRenderer panel, VisualElement root, int version)
    {
        if (seta != null)
        {
            seta.generateVisualContent -= DesenharSeta;
        }

        seta = root.Q<VisualElement>(NomeElemento);
        if (seta == null)
        {
            Debug.LogWarning("SetaDoBoss: elemento '" + NomeElemento + "' não encontrado. Confira se o PanelRenderer usa o HudProgressao.uxml atualizado.", this);
            return;
        }

        seta.pickingMode = PickingMode.Ignore;
        seta.style.width = new Length(tamanhoSeta);
        seta.style.height = new Length(tamanhoSeta);
        seta.generateVisualContent += DesenharSeta;
        seta.MarkDirtyRepaint();

        // Árvore nova começa escondida (display: none no USS).
        visivel = true;
        Mostrar(false);
    }

    private void LateUpdate()
    {
        BossController boss = controladorBoss != null ? controladorBoss.BossAtual : null;
        if (seta == null || seta.panel == null || !estadoPermite || boss == null || boss.vida <= 0)
        {
            Mostrar(false);
            return;
        }

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Mostrar(false);
                return;
            }
        }

        if (BossEstaNaTela(boss) || !Posicionar(boss.transform.position))
        {
            Mostrar(false);
            return;
        }

        Mostrar(true);
    }

    // Visível = a caixa do sprite do boss cruza o retângulo da câmera (câmera ortográfica 2D).
    // Com os bounds (e não só o centro), a seta não pisca quando o boss ronda na beirada da tela.
    private bool BossEstaNaTela(BossController boss)
    {
        if (boss != bossDoRenderer)
        {
            bossDoRenderer = boss;
            rendererDoBoss = boss.GetComponentInChildren<Renderer>();
        }

        Bounds caixa = rendererDoBoss != null
            ? rendererDoBoss.bounds
            : new Bounds(boss.transform.position, TamanhoReservaDoBoss);

        float altura = cam.orthographicSize;
        float largura = altura * cam.aspect;
        Vector2 centro = cam.transform.position;

        return caixa.max.x > centro.x - largura && caixa.min.x < centro.x + largura
            && caixa.max.y > centro.y - altura && caixa.min.y < centro.y + altura;
    }

    // Coloca a seta na borda do painel (com margem), na linha do centro da tela até o boss, girada para ele.
    // Devolve false se o painel ainda não tem tamanho (antes do primeiro layout).
    private bool Posicionar(Vector3 posicaoBoss)
    {
        Rect tela = seta.panel.visualTree.layout;
        if (float.IsNaN(tela.width) || float.IsNaN(tela.height) || tela.width <= 0f || tela.height <= 0f)
        {
            return false;
        }

        // Viewport (0..1, Y para cima) -> painel (px de referência, Y para baixo). Vale com o Scale With Screen Size.
        Vector3 vp = cam.WorldToViewportPoint(posicaoBoss);
        Vector2 centro = new Vector2(tela.width * 0.5f, tela.height * 0.5f);
        Vector2 direcao = new Vector2(vp.x * tela.width, (1f - vp.y) * tela.height) - centro;
        if (direcao.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        Vector2 minimo = new Vector2(margemBorda, margemTopo);
        Vector2 maximo = new Vector2(tela.width - margemBorda, tela.height - margemBorda);
        Vector2 naBorda = centro + direcao * FracaoAteABorda(centro, direcao, minimo, maximo);
        naBorda = new Vector2(Mathf.Clamp(naBorda.x, minimo.x, maximo.x), Mathf.Clamp(naBorda.y, minimo.y, maximo.y));

        // Painel -> local do pai (HudRaiz), centralizando o elemento no ponto.
        Vector2 local = seta.parent.WorldToLocal(naBorda);
        float meio = tamanhoSeta * 0.5f;
        seta.style.translate = new Translate(new Length(local.x - meio), new Length(local.y - meio));

        // Y para baixo: Atan2 dá o ângulo no sentido horário, igual ao rotate do UI Toolkit. O desenho aponta para a direita (0°).
        float graus = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        seta.style.rotate = new Rotate(new Angle(graus, AngleUnit.Degree));

        float pulso = 1f + AmplitudePulso * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / PeriodoPulso)));
        seta.style.scale = new Scale(new Vector2(pulso, pulso));
        return true;
    }

    // Quanto da direção (0..1+) cabe até bater no retângulo [minimo, maximo] saindo do centro.
    private static float FracaoAteABorda(Vector2 centro, Vector2 direcao, Vector2 minimo, Vector2 maximo)
    {
        float fracao = float.MaxValue;

        if (direcao.x > 0f)
        {
            fracao = Mathf.Min(fracao, (maximo.x - centro.x) / direcao.x);
        }
        else if (direcao.x < 0f)
        {
            fracao = Mathf.Min(fracao, (minimo.x - centro.x) / direcao.x);
        }

        if (direcao.y > 0f)
        {
            fracao = Mathf.Min(fracao, (maximo.y - centro.y) / direcao.y);
        }
        else if (direcao.y < 0f)
        {
            fracao = Mathf.Min(fracao, (minimo.y - centro.y) / direcao.y);
        }

        return Mathf.Max(0f, fracao);
    }

    // Ponta de flecha apontando para a direita (0°), com um entalhe atrás. Desenhada uma vez; mover e girar
    // a seta (translate/rotate/scale) não exige redesenhar.
    private void DesenharSeta(MeshGenerationContext contexto)
    {
        Rect r = contexto.visualElement.contentRect;
        if (r.width <= 0f || r.height <= 0f)
        {
            return;
        }

        Painter2D pintor = contexto.painter2D;
        pintor.BeginPath();
        pintor.MoveTo(new Vector2(r.xMax, r.center.y));
        pintor.LineTo(new Vector2(r.xMin, r.yMin));
        pintor.LineTo(new Vector2(r.xMin + r.width * 0.3f, r.center.y));
        pintor.LineTo(new Vector2(r.xMin, r.yMax));
        pintor.ClosePath();

        pintor.fillColor = corSeta;
        pintor.Fill();

        pintor.strokeColor = corContorno;
        pintor.lineWidth = 3f;
        pintor.lineJoin = LineJoin.Round;
        pintor.Stroke();
    }

    // Só escreve o estilo quando muda (o LateUpdate chama todo frame).
    private void Mostrar(bool mostrar)
    {
        if (seta == null || visivel == mostrar)
        {
            return;
        }

        visivel = mostrar;
        seta.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
