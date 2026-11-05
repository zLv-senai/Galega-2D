using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Roda de opções do menu principal e da pausa: os botões ficam em arco ao lado de uma bola grande (parte fora da
// tela), e o selecionado vai para o meio. Só posiciona e destaca os botões; a movimentação suave é a transição do
// USS (.roda-item). A opção muda só por scroll ou setas; Confirmar dispara o clique registrado por quem montou o menu.
public sealed class RodaDeOpcoes
{
    // Geometria na resolução de referência 1920x1080 (relativa ao canto superior esquerdo da tela).
    private const float Raio = 650f;
    private const float CentroX = -125f;
    private const float AnguloPorPasso = 25f;
    private const float AlturaDoItem = 110f;

    private const float EscalaSelecionado = 1.16f;
    private const float PerdaDeEscalaPorPasso = 0.11f;
    private const float OpacidadeMinima = 0.22f;

    // O scroll muda no máximo 1 passo por intervalo (a roda do mouse dispara vários eventos por clique).
    private const float IntervaloDoScroll = 0.13f;

    // Giro do planeta por passo da roda (acumulado: a volta circular das opções não inverte o sentido).
    private const float GrausPorPasso = 30f;

    private const string ClasseItem = "roda-item";
    private const string ClasseSelecionado = "roda-item--selecionado";

    private readonly List<Button> botoes;
    private readonly string[] descricoes;
    private readonly Label rotuloDescricao;
    private readonly float centroY;
    private readonly VisualElement bola;
    private float anguloDaBola;
    private int selecionado;
    private float proximoScroll;

    // botoes na ordem da roda; descricoes (opcional) e rotuloDescricao mostram o texto da opção selecionada.
    public RodaDeOpcoes(List<Button> botoes, string[] descricoes, Label rotuloDescricao, float centroY, VisualElement bola = null)
    {
        this.bola = bola;
        this.botoes = botoes;
        this.descricoes = descricoes;
        this.rotuloDescricao = rotuloDescricao;
        this.centroY = centroY;

        for (int i = 0; i < botoes.Count; i++)
        {
            botoes[i].AddToClassList(ClasseItem);
            // O mouse não escolhe a opção: sem hover nem clique próprio. Quem muda é o scroll/teclado e quem confirma
            // é o Enter ou o botão esquerdo (ver LerConfirmacao).
            botoes[i].pickingMode = PickingMode.Ignore;
        }

        Aplicar(0);
    }

    public int Selecionado => selecionado;

    // Seleciona a opção i (com volta circular) e toca o som de navegação se a seleção mudou.
    public void Selecionar(int i)
    {
        int novo = ((i % botoes.Count) + botoes.Count) % botoes.Count;
        if (novo == selecionado)
        {
            return;
        }

        Aplicar(novo);
        GerenciadorDeSom.TocarNavegacao();
    }

    // Setas, W/S e scroll do mouse mudam a opção (quem chama decide quando a roda pode ouvir as teclas).
    public void LerNavegacao()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.downArrowKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame)
            {
                Passo(1);
            }
            else if (teclado.upArrowKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame)
            {
                Passo(-1);
            }
        }

        Mouse mouse = Mouse.current;
        float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (scroll != 0f && Time.unscaledTime >= proximoScroll)
        {
            proximoScroll = Time.unscaledTime + IntervaloDoScroll;
            Passo(scroll < 0f ? 1 : -1);
        }
    }

    // Enter ou botão esquerdo do mouse confirmam a opção selecionada, onde quer que esteja o ponteiro. Usa o pressionar
    // do mouse: o clique do "Voltar" dispara ao soltar, então não confirma a roda por engano. Devolve true se confirmou.
    public bool LerConfirmacao()
    {
        Keyboard teclado = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool enter = teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame);
        bool clique = mouse != null && mouse.leftButton.wasPressedThisFrame;
        if (!enter && !clique)
        {
            return false;
        }

        Confirmar();
        return true;
    }

    // Volta para a primeira opção sem tocar som (ao reabrir o menu de pausa).
    public void Reiniciar()
    {
        Aplicar(0);
    }

    // direcao +1 = opção de baixo (planeta gira no sentido horário), -1 = opção de cima (anti-horário).
    public void Passo(int direcao)
    {
        int antes = selecionado;
        Selecionar(selecionado + direcao);
        if (bola != null && selecionado != antes)
        {
            anguloDaBola += Mathf.Sign(direcao) * GrausPorPasso;
            bola.style.rotate = new Rotate(new Angle(anguloDaBola, AngleUnit.Degree));
        }
    }

    // Dispara o clique do botão selecionado, como se o jogador tivesse clicado nele.
    public void Confirmar()
    {
        Button botao = botoes[selecionado];
        using (ClickEvent clique = ClickEvent.GetPooled())
        {
            clique.target = botao;
            botao.SendEvent(clique);
        }
    }

    // Posiciona cada botão no arco e destaca o selecionado.
    private void Aplicar(int novoSelecionado)
    {
        selecionado = novoSelecionado;
        int n = botoes.Count;

        for (int i = 0; i < n; i++)
        {
            int d = (i - selecionado + n) % n;
            if (d > n / 2)
            {
                d -= n;
            }

            float angulo = d * AnguloPorPasso * Mathf.Deg2Rad;
            float x = CentroX + Raio * Mathf.Cos(angulo);
            float y = centroY + Raio * Mathf.Sin(angulo) - AlturaDoItem / 2f;
            int distancia = Mathf.Abs(d);
            bool ehSelecionado = distancia == 0;

            Button botao = botoes[i];
            botao.style.translate = new Translate(x, y);
            float escala = ehSelecionado ? EscalaSelecionado : 1f - PerdaDeEscalaPorPasso * distancia;
            botao.style.scale = new Scale(new Vector3(escala, escala, 1f));
            botao.style.opacity = ehSelecionado ? 1f : Mathf.Max(OpacidadeMinima, 0.72f - 0.26f * (distancia - 1));
            botao.EnableInClassList(ClasseSelecionado, ehSelecionado);
        }

        if (rotuloDescricao != null && descricoes != null && selecionado < descricoes.Length)
        {
            rotuloDescricao.text = descricoes[selecionado];
        }
    }
}
