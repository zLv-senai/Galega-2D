using UnityEngine;

// Padrões em que o boss alterna modos. Cada modelo tem o seu (campo "padrao" do prefab):
//   Cacador (BOSS1): cerca o player e, de tempos em tempos, avisa e dá uma investida em linha reta.
//   Serpente (BOSS2): desenha uma flor em volta do player e alterna com varreduras pelas bordas da tela.
//   Espectro (BOSS3): orbita, some e reaparece atirando em círculo; no fim do ciclo teleporta para uma borda da tela,
//   gira a base do cone para dentro e varre com um laser, que anda em coordenadas de mundo (não segue o player).
// A câmera fica centrada no player, então "em volta do player" é o mesmo que "dentro da tela".
public partial class BossController
{
    private enum Modo { Cruzeiro, Aviso, Investida, Recuperando, Varredura, Sumindo, Surgindo, Pausa, LaserAviso, LaserFino, LaserAbrindo, LaserVarrendo, LaserFechando }

    // Para onde o Espectro vai ao reaparecer: o outro lado da tela, uma borda (laser) ou de volta à órbita.
    private enum DestinoDoSalto { OutroLado, Borda, Orbita }

    // Geral: folga entre o boss e a borda da tela, tamanho de tela sem câmera e margem sobre a velocidade do player
    // (sem isso ele fugiria do boss correndo com buffs).
    private const float MargemTelaX = 1.2f;
    private const float MargemTelaY = 1f;
    private const float MeiaAlturaPadrao = 5f;
    private const float AspectoPadrao = 16f / 9f;
    private const float FolgaSobreOPlayer = 1.2f;
    private const int OrdemDaLinha = 9; // logo abaixo do sprite do boss (order 10)

    // Cacador
    private const float DuracaoCerco = 3.5f;
    private const float GiroDoCerco = 0.6f;
    private const float MultiplicadorCerco = 3f;
    private const float DuracaoAviso = 0.9f;
    private const float MiraTravaAntesDoFim = 0.3f;
    private const float MultiplicadorInvestida = 9f;
    private const float PassaDoAlvo = 4f;
    private const float DuracaoRecuperacao = 0.7f;

    // Serpente
    // Giros pensados para o boss andar a uns 4 a 5 unidades por segundo: o tiro do player anda a 10, e mais rápido que isso quase não dá para acertar.
    private const float DuracaoFlor = 10f;
    private const float GiroDaFlor = 0.28f;
    private const float PetalasDaFlor = 3f;
    private const float FundoDaPetala = 0.3f; // 0.3 mantém o boss a mais de 2,5 unidades do player no ponto mais fechado
    private const float MultiplicadorFlor = 5f;
    private const float MultiplicadorVarredura = 2.5f;
    private const float IntervaloNaVarredura = 0.75f; // na varredura ele atira mais rápido
    private const float ChegouAoPonto = 0.5f;
    private const float LimiteVarredura = 10f; // reserva: nunca fica preso numa varredura
    private const int PontosDaVarredura = 4;

    // Espectro
    private const float DuracaoOrbita = 7f;
    private const float GiroDaOrbita = 0.3f;
    private const float EscalaDaOrbita = 0.9f;
    private const float MultiplicadorOrbita = 5f;
    private const int SaltosPorCiclo = 3;
    private const float DuracaoSumindo = 0.35f;
    private const float DuracaoSurgindo = 0.45f;
    private const float DuracaoPausaEntreSaltos = 0.5f;
    private const float DesvioDoSalto = 50f;
    private const int TirosEmCirculo = 12;
    private const float DuracaoAvisoFeixe = 0.8f;
    private const float DuracaoFinoFeixe = 0.15f;
    private const float DuracaoAbrindoFeixe = 0.2f;
    private const float DuracaoVarredura = 2.5f;
    private const float DuracaoFechandoFeixe = 0.12f;
    private const float VelocidadeVarredura = 3.5f; // unidades de mundo por segundo, ao longo da borda
    private const float LarguraFinaFeixe = 0.08f;
    private const float MargemDaBorda = 0.2f;
    private const float ComprimentoLaser = 30f;
    private const float RaioDoPlayerNoLaser = 0.35f;
    private const float IntervaloDanoLaser = 0.35f;
    private const int DanoLaserAlemDoTiro = 3;
    private const float FracaoDaCamadaInterna = 0.42f;
    private const float LarguraMinimaInterna = 0.05f;
    private const float AlfaAvisoAceso = 0.34f;
    private const float AlfaAvisoApagado = 0.1f;
    private const float PiscadasPorSegundo = 12f;

    // Linha de aviso do Cacador.
    private const float LarguraAviso = 0.06f;
    private static readonly Color CorDoAviso = new Color(1f, 0.25f, 0.25f, 0.85f);
    private static readonly Color CorDoFeixe = new Color(238f / 255f, 117f / 255f, 222f / 255f, 1f);

    // Direção do feixe (para dentro da tela) quando o boss está na borda: cima, baixo, esquerda, direita.
    private static readonly Vector2[] DirecoesDoFeixe = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(-1f, 0f) };

    // Estado dos modos.
    private Modo modo = Modo.Cruzeiro;
    private float tempoNoModo;
    private float sentidoDeGiro = 1f;
    private Vector2 direcaoDoGolpe;
    private float distanciaDoGolpe;
    private int indiceDaVarredura;
    private float ladoDaVarredura = 1f;
    private int saltosRestantes;
    private float proximoDanoLaser;
    private DestinoDoSalto destinoDoSalto;
    private int ultimaBorda = -1;
    private Vector2 direcaoDoFeixe;
    private float sentidoDaVarredura = 1f;
    private float posicaoDeVarredura; // eixo lateral da borda, em coordenadas de mundo
    private Vector3 ultimaPosicaoDoAlvo;
    private bool temPosicaoDoAlvo;
    private float velocidadeDoAlvo;

    // Componentes usados pelos modos (o Awake do BossController chama PrepararModos).
    private SpriteRenderer sprite;
    private Collider2D meuCollider;
    private Color corOriginal = Color.white;
    private LineRenderer linha;
    private LineRenderer linhaInterna; // só o Espectro: o miolo branco do laser
    private float larguraDoFeixe;      // largura da base do cone
    private float meiaAlturaDoBoss;    // do centro à base do cone

    // Quem usa Modo: os padrões antigos ficam sempre em Cruzeiro, e esta conta continua igual para eles.
    private bool PodeAtirarNoModo => modo == Modo.Cruzeiro || modo == Modo.Varredura;
    private float FatorDoIntervaloDeTiro => modo == Modo.Varredura ? IntervaloNaVarredura : 1f;

    // Guarda sprite e collider. A linha só é criada para quem precisa dela.
    private void PrepararModos()
    {
        sprite = GetComponent<SpriteRenderer>();
        meuCollider = GetComponent<Collider2D>();
        if (sprite != null)
        {
            corOriginal = sprite.color;
        }

        if (padrao == PadraoBoss.Cacador || padrao == PadraoBoss.Espectro)
        {
            linha = CriarLinha("LinhaDoBoss", OrdemDaLinha);
        }

        if (padrao == PadraoBoss.Espectro && sprite != null && sprite.sprite != null)
        {
            // Medidas do cone antes de girar o boss: o feixe sai da base e tem a largura dela.
            larguraDoFeixe = sprite.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
            meiaAlturaDoBoss = sprite.sprite.bounds.extents.y * Mathf.Abs(transform.lossyScale.y);

            // O feixe fica atrás do sprite, na mesma sorting layer.
            linha.sortingLayerID = sprite.sortingLayerID;
            linha.sortingOrder = sprite.sortingOrder - 2;
            linhaInterna = CriarLinha("LinhaInternaDoBoss", sprite.sortingOrder - 1);
            linhaInterna.sortingLayerID = sprite.sortingLayerID;
        }
    }

    private LineRenderer CriarLinha(string nome, int ordem)
    {
        GameObject objeto = new GameObject(nome);
        objeto.transform.SetParent(transform, false);

        LineRenderer nova = objeto.AddComponent<LineRenderer>();
        nova.useWorldSpace = true;
        nova.positionCount = 2;
        nova.numCapVertices = 4;
        nova.sortingOrder = ordem;
        // Usa o material do próprio sprite: ele já está na build, enquanto um Shader.Find poderia falhar nela.
        nova.sharedMaterial = sprite != null ? sprite.sharedMaterial : null;
        nova.enabled = false;
        return nova;
    }

    // Um frame dos padrões com modos. Sem alvo, o boss espera parado.
    private void MoverComModos()
    {
        if (!TemAlvo())
        {
            return;
        }

        MedirVelocidadeDoAlvo();

        // O relógio do modo principal só anda com o boss na tela: ele nasce fora dela e precisa chegar antes de atacar.
        if (modo != Modo.Cruzeiro || EstaNaTela())
        {
            tempoNoModo += Time.deltaTime;
        }

        switch (padrao)
        {
            case PadraoBoss.Cacador:
                MoverCacador();
                break;
            case PadraoBoss.Serpente:
                MoverSerpente();
                break;
            case PadraoBoss.Espectro:
                MoverEspectro();
                break;
        }
    }

    private void TrocarModo(Modo novo)
    {
        modo = novo;
        tempoNoModo = 0f;

        if (novo == Modo.Aviso || novo == Modo.LaserAviso)
        {
            AoAvisarAtaque?.Invoke(this);
        }
    }

    private void VoltarAoCruzeiro()
    {
        sentidoDeGiro = -sentidoDeGiro;
        rondaIniciada = false;
        TrocarModo(Modo.Cruzeiro);
    }

    // ---- Cacador ----

    private void MoverCacador()
    {
        switch (modo)
        {
            case Modo.Cruzeiro:
                Circular(distanciaRondar, distanciaRondar, GiroDoCerco, velocidade * MultiplicadorCerco);
                if (tempoNoModo >= DuracaoCerco)
                {
                    TrocarModo(Modo.Aviso);
                }
                break;

            case Modo.Aviso:
                // A mira acompanha o player e trava pouco antes do fim: é a janela para ele sair da linha.
                if (tempoNoModo < DuracaoAviso - MiraTravaAntesDoFim)
                {
                    Vector2 paraOAlvo = target.position - transform.position;
                    direcaoDoGolpe = paraOAlvo.normalized;
                    distanciaDoGolpe = paraOAlvo.magnitude + PassaDoAlvo;
                }

                MostrarLinha(direcaoDoGolpe, distanciaDoGolpe, LarguraAviso, CorDoAviso);
                PiscarPerigo();

                if (tempoNoModo >= DuracaoAviso)
                {
                    EsconderLinha();
                    RestaurarCor();
                    TrocarModo(Modo.Investida);
                }
                break;

            case Modo.Investida:
                float passo = Mathf.Min(velocidade * MultiplicadorInvestida * Time.deltaTime, distanciaDoGolpe);
                transform.position += (Vector3)(direcaoDoGolpe * passo);
                distanciaDoGolpe -= passo;
                if (distanciaDoGolpe <= 0f)
                {
                    TrocarModo(Modo.Recuperando);
                }
                break;

            case Modo.Recuperando:
                if (tempoNoModo >= DuracaoRecuperacao)
                {
                    VoltarAoCruzeiro();
                }
                break;
        }
    }

    // ---- Serpente ----

    private void MoverSerpente()
    {
        Vector2 limite = LimiteDaTela();

        if (modo == Modo.Cruzeiro)
        {
            // Curva polar de 3 pétalas: fecha e abre em volta do player sem passar por cima dele.
            IniciarAnguloDaRonda();
            anguloRondar += sentidoDeGiro * velocidade * GiroDaFlor * Time.deltaTime;
            float raio = (1f + FundoDaPetala * Mathf.Cos(PetalasDaFlor * anguloRondar)) / (1f + FundoDaPetala);
            IrPara(new Vector2(limite.x * raio * Mathf.Cos(anguloRondar), limite.y * raio * Mathf.Sin(anguloRondar)), velocidade * MultiplicadorFlor);

            if (tempoNoModo >= DuracaoFlor)
            {
                indiceDaVarredura = 0;
                ladoDaVarredura = -ladoDaVarredura;
                TrocarModo(Modo.Varredura);
            }

            return;
        }

        // Varredura: canto, canto oposto na mesma borda, desce (ou sobe) e volta pela borda de baixo (ou de cima).
        Vector2 ponto = PontoDaVarredura(indiceDaVarredura, limite);
        IrPara(ponto, velocidade * MultiplicadorVarredura);

        Vector2 relativo = transform.position - target.position;
        if ((relativo - ponto).magnitude <= ChegouAoPonto)
        {
            indiceDaVarredura++;
        }

        if (indiceDaVarredura >= PontosDaVarredura || tempoNoModo >= LimiteVarredura)
        {
            VoltarAoCruzeiro();
        }
    }

    private Vector2 PontoDaVarredura(int indice, Vector2 limite)
    {
        float x = (indice == 0 || indice == 3) ? -limite.x : limite.x;
        float y = indice < 2 ? limite.y : -limite.y;
        return new Vector2(x * ladoDaVarredura, y * ladoDaVarredura);
    }

    // ---- Espectro ----

    private void MoverEspectro()
    {
        switch (modo)
        {
            case Modo.Cruzeiro:
                Vector2 limite = LimiteDaTela();
                Circular(limite.x * EscalaDaOrbita, limite.y * EscalaDaOrbita, velocidade * GiroDaOrbita, velocidade * MultiplicadorOrbita);
                if (tempoNoModo >= DuracaoOrbita)
                {
                    saltosRestantes = SaltosPorCiclo;
                    destinoDoSalto = DestinoDoSalto.OutroLado;
                    ComecarASumir();
                }
                break;

            case Modo.Sumindo:
                DefinirOpacidade(1f - tempoNoModo / DuracaoSumindo);
                if (tempoNoModo >= DuracaoSumindo)
                {
                    SaltarPara(destinoDoSalto);
                    TrocarModo(Modo.Surgindo);
                }
                break;

            case Modo.Surgindo:
                DefinirOpacidade(tempoNoModo / DuracaoSurgindo);
                if (destinoDoSalto == DestinoDoSalto.Borda)
                {
                    AtualizarPosicaoNaBorda();
                }

                if (tempoNoModo >= DuracaoSurgindo)
                {
                    RestaurarCor();
                    AtivarCollider(true);
                    TerminarDeSurgir();
                }
                break;

            case Modo.Pausa:
                if (tempoNoModo >= DuracaoPausaEntreSaltos)
                {
                    ComecarASumir();
                }
                break;

            case Modo.LaserAviso:
            case Modo.LaserFino:
            case Modo.LaserAbrindo:
            case Modo.LaserVarrendo:
            case Modo.LaserFechando:
                MoverLaser();
                break;
        }
    }

    private void ComecarASumir()
    {
        // Sem collider enquanto some e surge: não bate por contato nem leva tiro.
        AtivarCollider(false);
        TrocarModo(Modo.Sumindo);
    }

    private void SaltarPara(DestinoDoSalto destino)
    {
        switch (destino)
        {
            case DestinoDoSalto.Borda:
                SaltarParaUmaBorda();
                break;
            case DestinoDoSalto.Orbita:
                SaltarParaAOrbita();
                break;
            default:
                SaltarParaOOutroLado();
                break;
        }
    }

    // Fim do Surgindo: o salto normal atira em círculo; ao lado da borda começa o laser; na órbita volta ao cruzeiro.
    private void TerminarDeSurgir()
    {
        switch (destinoDoSalto)
        {
            case DestinoDoSalto.Borda:
                TrocarModo(Modo.LaserAviso);
                break;
            case DestinoDoSalto.Orbita:
                VoltarAoCruzeiro();
                break;
            default:
                DispararEmCirculo(TirosEmCirculo);
                saltosRestantes--;
                if (saltosRestantes <= 0)
                {
                    destinoDoSalto = DestinoDoSalto.Borda;
                }

                TrocarModo(Modo.Pausa);
                break;
        }
    }

    // Sorteia uma borda (nunca a mesma da vez anterior), gira o boss para a base do cone apontar para dentro da tela
    // e escolhe o sentido da varredura. A varredura começa metade do trajeto antes do player.
    private void SaltarParaUmaBorda()
    {
        int borda;
        do
        {
            borda = Random.Range(0, DirecoesDoFeixe.Length);
        }
        while (borda == ultimaBorda);

        ultimaBorda = borda;
        direcaoDoFeixe = DirecoesDoFeixe[borda];
        // Atan2 e não FromToRotation: este degenera quando o giro é de 180 graus.
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direcaoDoFeixe.x, -direcaoDoFeixe.y) * Mathf.Rad2Deg);

        sentidoDaVarredura = Random.value < 0.5f ? -1f : 1f;
        float eixoLateralDoAlvo = direcaoDoFeixe.x == 0f ? target.position.x : target.position.y;
        posicaoDeVarredura = eixoLateralDoAlvo - sentidoDaVarredura * VelocidadeVarredura * DuracaoVarredura / 2f;
        AtualizarPosicaoNaBorda();
    }

    private void SaltarParaAOrbita()
    {
        transform.rotation = Quaternion.identity;

        // A ronda parte do ângulo sorteado (senão o Circular iria até o ângulo antigo, de antes do laser).
        float angulo = Random.Range(0f, 2f * Mathf.PI);
        anguloRondar = angulo;
        rondaIniciada = true;

        Vector2 limite = LimiteDaTela();
        Vector3 destino = target.position + new Vector3(Mathf.Cos(angulo) * limite.x * EscalaDaOrbita, Mathf.Sin(angulo) * limite.y * EscalaDaOrbita, 0f);
        destino.z = transform.position.z;
        transform.position = destino;
    }

    // Eixo perpendicular à borda preso à câmera (que é o target) e eixo lateral na posição da varredura.
    private void AtualizarPosicaoNaBorda()
    {
        Camera cam = Camera.main;
        float meiaAltura = cam != null ? cam.orthographicSize : MeiaAlturaPadrao;
        float aspecto = cam != null ? cam.aspect : AspectoPadrao;
        bool feixeVertical = direcaoDoFeixe.x == 0f;

        float distancia = (feixeVertical ? meiaAltura : meiaAltura * aspecto) - meiaAlturaDoBoss - MargemDaBorda;
        Vector3 posicao = transform.position;
        if (feixeVertical)
        {
            posicao.x = posicaoDeVarredura;
            posicao.y = target.position.y - direcaoDoFeixe.y * distancia;
        }
        else
        {
            posicao.x = target.position.x - direcaoDoFeixe.x * distancia;
            posicao.y = posicaoDeVarredura;
        }

        transform.position = posicao;
    }

    private void MoverLaser()
    {
        AtualizarPosicaoNaBorda();

        switch (modo)
        {
            case Modo.LaserAviso:
                float alfa = Mathf.FloorToInt(tempoNoModo * PiscadasPorSegundo) % 2 == 1 ? AlfaAvisoAceso : AlfaAvisoApagado;
                MostrarFeixe(larguraDoFeixe, alfa, false);
                if (tempoNoModo >= DuracaoAvisoFeixe)
                {
                    TrocarModo(Modo.LaserFino);
                }
                break;

            case Modo.LaserFino:
                MostrarFeixe(Mathf.Lerp(larguraDoFeixe, LarguraFinaFeixe, tempoNoModo / DuracaoFinoFeixe), 1f, false);
                if (tempoNoModo >= DuracaoFinoFeixe)
                {
                    proximoDanoLaser = 0f;
                    TrocarModo(Modo.LaserAbrindo);
                    AoDispararLaser?.Invoke(this);
                }
                break;

            case Modo.LaserAbrindo:
                float larguraAbrindo = Mathf.Lerp(LarguraFinaFeixe, larguraDoFeixe, tempoNoModo / DuracaoAbrindoFeixe);
                MostrarFeixe(larguraAbrindo, 1f, true);
                AplicarDanoDoLaser(larguraAbrindo);
                if (tempoNoModo >= DuracaoAbrindoFeixe)
                {
                    TrocarModo(Modo.LaserVarrendo);
                }
                break;

            case Modo.LaserVarrendo:
                posicaoDeVarredura += sentidoDaVarredura * VelocidadeVarredura * Time.deltaTime;
                MostrarFeixe(larguraDoFeixe, 1f, true);
                AplicarDanoDoLaser(larguraDoFeixe);
                if (tempoNoModo >= DuracaoVarredura)
                {
                    TrocarModo(Modo.LaserFechando);
                }
                break;

            case Modo.LaserFechando:
                MostrarFeixe(Mathf.Lerp(larguraDoFeixe, 0f, tempoNoModo / DuracaoFechandoFeixe), 1f, true);
                if (tempoNoModo >= DuracaoFechandoFeixe)
                {
                    EsconderLinha();
                    destinoDoSalto = DestinoDoSalto.Orbita;
                    ComecarASumir();
                }
                break;
        }
    }

    private void SaltarParaOOutroLado()
    {
        Vector2 limite = LimiteDaTela();
        Vector2 relativo = transform.position - target.position;
        float angulo = Mathf.Atan2(-relativo.y, -relativo.x) + Random.Range(-DesvioDoSalto, DesvioDoSalto) * Mathf.Deg2Rad;

        Vector3 destino = target.position + new Vector3(Mathf.Cos(angulo) * limite.x, Mathf.Sin(angulo) * limite.y, 0f);
        destino.z = transform.position.z;
        transform.position = destino;
    }

    // Origem do feixe: a base do cone.
    private Vector2 OrigemDoFeixe()
    {
        return (Vector2)transform.position + direcaoDoFeixe * meiaAlturaDoBoss;
    }

    // O dano segue o que aparece na tela: o player precisa estar à frente da base e dentro da largura atual do feixe.
    private void AplicarDanoDoLaser(float largura)
    {
        if (Time.time < proximoDanoLaser)
        {
            return;
        }

        Vector2 relativo = (Vector2)target.position - OrigemDoFeixe();
        float aoLongo = Vector2.Dot(relativo, direcaoDoFeixe);
        float distanciaDaLinha = Mathf.Abs(relativo.x * direcaoDoFeixe.y - relativo.y * direcaoDoFeixe.x);
        if (aoLongo < 0f || aoLongo > ComprimentoLaser || distanciaDaLinha > largura / 2f + RaioDoPlayerNoLaser)
        {
            return;
        }

        proximoDanoLaser = Time.time + IntervaloDanoLaser;
        IDamageable jogador = target.GetComponent<IDamageable>();
        if (jogador != null)
        {
            jogador.TakeDamage(danoDoTiro + DanoLaserAlemDoTiro);
        }
    }

    // ---- Movimento comum ----

    private void MedirVelocidadeDoAlvo()
    {
        float passou = Time.deltaTime;
        if (temPosicaoDoAlvo && passou > 0f)
        {
            velocidadeDoAlvo = (target.position - ultimaPosicaoDoAlvo).magnitude / passou;
        }

        ultimaPosicaoDoAlvo = target.position;
        temPosicaoDoAlvo = true;
    }

    // Parte do ângulo atual do boss: ele chega de onde estiver, sem cruzar a tela por cima do player.
    private void IniciarAnguloDaRonda()
    {
        if (rondaIniciada)
        {
            return;
        }

        Vector2 relativo = transform.position - target.position;
        anguloRondar = relativo.sqrMagnitude > 0.0001f ? Mathf.Atan2(relativo.y, relativo.x) : 0f;
        rondaIniciada = true;
    }

    // Gira em elipse em volta do player.
    private void Circular(float raioX, float raioY, float giro, float velocidadeMaxima)
    {
        IniciarAnguloDaRonda();
        anguloRondar += sentidoDeGiro * giro * Time.deltaTime;
        IrPara(new Vector2(Mathf.Cos(anguloRondar) * raioX, Mathf.Sin(anguloRondar) * raioY), velocidadeMaxima);
    }

    // Anda até um ponto dado em relação ao player, sem deixar o ponto sair da tela.
    private void IrPara(Vector2 relativoDesejado, float velocidadeMaxima)
    {
        Vector2 limite = LimiteDaTela();
        Vector3 destino = target.position + new Vector3(
            Mathf.Clamp(relativoDesejado.x, -limite.x, limite.x),
            Mathf.Clamp(relativoDesejado.y, -limite.y, limite.y),
            0f);
        destino.z = transform.position.z;

        float velocidadeFinal = Mathf.Max(velocidadeMaxima, velocidadeDoAlvo * FolgaSobreOPlayer);
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidadeFinal * Time.deltaTime);
    }

    // Metade do tamanho da tela em unidades, descontada a margem.
    private Vector2 LimiteDaTela()
    {
        Camera cam = Camera.main;
        float meiaAltura = cam != null ? cam.orthographicSize : MeiaAlturaPadrao;
        float aspecto = cam != null ? cam.aspect : AspectoPadrao;
        return new Vector2(Mathf.Max(0.5f, meiaAltura * aspecto - MargemTelaX), Mathf.Max(0.5f, meiaAltura - MargemTelaY));
    }

    // ---- Visual ----

    private void MostrarLinha(Vector2 direcao, float comprimento, float largura, Color cor)
    {
        if (linha == null)
        {
            return;
        }

        Vector3 inicio = transform.position;
        linha.SetPosition(0, inicio);
        linha.SetPosition(1, inicio + (Vector3)(direcao * comprimento));
        linha.startWidth = largura;
        linha.endWidth = largura;
        linha.startColor = cor;
        linha.endColor = cor;
        linha.enabled = true;
    }

    // Laser do Espectro: a camada externa rosa e, opcionalmente, o miolo branco. Sem ponta redonda, para sair reto da base.
    private void MostrarFeixe(float largura, float alfa, bool comCamadaInterna)
    {
        if (linha == null || linhaInterna == null)
        {
            return;
        }

        Color cor = CorDoFeixe;
        cor.a = alfa;
        DesenharCamada(linha, largura, cor);

        linhaInterna.enabled = false;
        if (comCamadaInterna)
        {
            DesenharCamada(linhaInterna, Mathf.Min(largura, Mathf.Max(LarguraMinimaInterna, largura * FracaoDaCamadaInterna)), Color.white);
        }
    }

    private void DesenharCamada(LineRenderer camada, float largura, Color cor)
    {
        Vector3 inicio = OrigemDoFeixe();
        inicio.z = transform.position.z;
        camada.numCapVertices = 0;
        camada.SetPosition(0, inicio);
        camada.SetPosition(1, inicio + (Vector3)(direcaoDoFeixe * ComprimentoLaser));
        camada.startWidth = largura;
        camada.endWidth = largura;
        camada.startColor = cor;
        camada.endColor = cor;
        camada.enabled = true;
    }

    private void EsconderLinha()
    {
        if (linha != null)
        {
            linha.enabled = false;
        }

        if (linhaInterna != null)
        {
            linhaInterna.enabled = false;
        }
    }

    private void PiscarPerigo()
    {
        if (sprite != null)
        {
            sprite.color = Color.Lerp(corOriginal, CorDoAviso, Mathf.PingPong(tempoNoModo * 8f, 1f));
        }
    }

    private void DefinirOpacidade(float opacidade)
    {
        if (sprite != null)
        {
            Color cor = corOriginal;
            cor.a = Mathf.Clamp01(opacidade) * corOriginal.a;
            sprite.color = cor;
        }
    }

    private void RestaurarCor()
    {
        if (sprite != null)
        {
            sprite.color = corOriginal;
        }
    }

    private void AtivarCollider(bool ativo)
    {
        if (meuCollider != null)
        {
            meuCollider.enabled = ativo;
        }
    }

    // Vários tiros iguais ao redor do boss (o Espectro atira assim ao reaparecer).
    private void DispararEmCirculo(int quantidade)
    {
        if (tiroPrefab == null || gun == null)
        {
            return;
        }

        float passo = 360f / Mathf.Max(1, quantidade);
        Quaternion mira = RotacaoDeMira();
        for (int i = 0; i < quantidade; i++)
        {
            CriarTiro(mira * Quaternion.Euler(0f, 0f, i * passo));
        }

        AoAtirar?.Invoke(this);
    }
}
