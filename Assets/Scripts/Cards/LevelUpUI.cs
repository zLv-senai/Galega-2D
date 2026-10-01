using UnityEngine;
using UnityEngine.UIElements;

// Tela de escolha de cards do level up (LevelUp.uxml). Usa o PanelRenderer no mesmo padrão
// do MenuManager/HudProgressao: o Unity pode recriar a UI (reload), então os elementos são
// buscados de novo em OnUIReload e o estado atual (oferta) é reaplicado.
// Esconde/mostra com style.display; funciona com Time.timeScale = 0 (UI não depende do tempo do jogo).
public class LevelUpUI : MonoBehaviour
{
    // Uma classe USS por raridade, na mesma ordem do enum Raridade.
    private static readonly string[] ClassesDeRaridade =
    {
        "raridade-comum", "raridade-incomum", "raridade-raro",
        "raridade-epico", "raridade-lendario", "raridade-mitico"
    };

    // O PanelRenderer (se faltar, pega do mesmo objeto) e o LevelUpManager (se faltar, procura na cena).
    [SerializeField] private PanelRenderer painel;
    [SerializeField] private LevelUpManager manager;

    // Integração (waves): título da tela quando o pedido de cards não traz um (ex.: "WAVE 3 CONCLUÍDA").
    private const string TituloPadrao = "ESCOLHA UM CARD";

    // Elementos do LevelUp.uxml (buscados a cada reload): painel inteiro, título, contador de escolhas
    // e um botão por card.
    private VisualElement raiz;
    private Label tituloLabel;
    private Label restantesLabel;
    private readonly Button[] botoes = new Button[SorteadorDeCards.CardsPorOferta];

    // Oferta que está na tela (null = tela escondida) e quantas escolhas faltam, para reaplicar no reload.
    private CardData[] ofertaAtual;
    private int pendentes;

    // Manager em que os eventos estão assinados, para cancelar certo no OnDisable.
    private LevelUpManager managerAssinado;

    // Acha o PanelRenderer e registra o OnUIReload, que monta a tela quando a UI carrega.
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
            Debug.LogWarning("LevelUpUI: PanelRenderer não encontrado. Arraste no Inspector ou coloque este script no mesmo objeto.");
        }
    }

    // Cancela o registro do callback de reload da UI.
    private void OnDestroy()
    {
        if (painel != null)
        {
            painel.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    // Ao ativar, passa a ouvir o LevelUpManager.
    private void OnEnable()
    {
        Assinar();
    }

    private void Start()
    {
        // Se o LevelUpManager não foi arrastado nem existia no OnEnable, procura de novo.
        Assinar();
    }

    // Ao desativar, para de ouvir o LevelUpManager.
    private void OnDisable()
    {
        if (managerAssinado != null)
        {
            managerAssinado.AoOferecerCards -= MostrarOferta;
            managerAssinado.AoEncerrar -= Esconder;
            managerAssinado = null;
        }
    }

    // Acha o LevelUpManager e passa a ouvi-lo: mostra a tela quando há oferta e esconde quando a escolha termina.
    private void Assinar()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<LevelUpManager>();
        }

        if (manager == null)
        {
            Debug.LogWarning("LevelUpUI: nenhum LevelUpManager na cena.");
            return;
        }

        if (managerAssinado == null)
        {
            manager.AoOferecerCards += MostrarOferta;
            manager.AoEncerrar += Esconder;
            managerAssinado = manager;
        }
    }

    // Roda quando a UI é criada ou recriada: busca os elementos pelo name, liga o clique de cada card e redesenha.
    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        raiz = root.Q<VisualElement>("LevelUpRaiz");
        tituloLabel = root.Q<Label>("LevelUpTitulo");
        restantesLabel = root.Q<Label>("LevelUpRestantes");

        for (int i = 0; i < botoes.Length; i++)
        {
            int indice = i; // cópia para o clique lembrar qual card é
            botoes[i] = root.Q<Button>("Card" + i);

            if (botoes[i] == null)
            {
                Debug.LogWarning("LevelUpUI: botão Card" + i + " não encontrado. Confira se o PanelRenderer usa o LevelUp.uxml.");
                continue;
            }

            botoes[i].RegisterCallback<ClickEvent>(evt => EscolherIndice(indice));
        }

        Renderizar();
    }

    // Chegou uma oferta nova: guarda os cards e quantas escolhas faltam, e desenha na tela.
    private void MostrarOferta(CardData[] oferta, int escolhasPendentes)
    {
        ofertaAtual = oferta;
        pendentes = escolhasPendentes;
        Renderizar();
    }

    // A escolha terminou: limpa a oferta, o que esconde a tela.
    private void Esconder()
    {
        ofertaAtual = null;
        Renderizar();
    }

    // Clique no botão de número "indice": manda o card correspondente para o LevelUpManager aplicar.
    private void EscolherIndice(int indice)
    {
        if (manager == null || ofertaAtual == null || indice >= ofertaAtual.Length)
        {
            return;
        }

        manager.Escolher(ofertaAtual[indice]);
    }

    // Desenha o estado atual: some se não há oferta; senão preenche cada botão com seu card.
    private void Renderizar()
    {
        if (raiz == null)
        {
            return;
        }

        bool visivel = ofertaAtual != null && ofertaAtual.Length > 0;
        raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;

        if (!visivel)
        {
            return;
        }

        if (tituloLabel != null)
        {
            // Integração (waves): "WAVE N CONCLUÍDA — escolha um card" no lugar do antigo "LEVEL UP — escolha um card".
            string titulo = manager != null ? manager.TituloAtual : null;
            tituloLabel.text = string.IsNullOrEmpty(titulo) ? TituloPadrao : titulo;
        }

        if (restantesLabel != null)
        {
            restantesLabel.text = pendentes > 1 ? "Escolhas restantes: " + pendentes : "";
        }

        for (int i = 0; i < botoes.Length; i++)
        {
            if (botoes[i] == null)
            {
                continue;
            }

            // Menos de 3 cards elegíveis: os botões que sobram ficam escondidos.
            bool temCard = i < ofertaAtual.Length && ofertaAtual[i] != null;
            botoes[i].style.display = temCard ? DisplayStyle.Flex : DisplayStyle.None;

            if (temCard)
            {
                PreencherBotao(botoes[i], ofertaAtual[i]);
            }
        }
    }

    // Preenche um botão com a cor da raridade (classe USS), o nome, a descrição e o ícone do card.
    private static void PreencherBotao(Button botao, CardData card)
    {
        foreach (string classe in ClassesDeRaridade)
        {
            botao.RemoveFromClassList(classe);
        }

        botao.AddToClassList(ClassesDeRaridade[(int)card.raridade]);

        Label raridade = botao.Q<Label>("CardRaridade");
        Label nome = botao.Q<Label>("CardNome");
        Label descricao = botao.Q<Label>("CardDescricao");
        VisualElement icone = botao.Q<VisualElement>("CardIcone");

        if (raridade != null)
        {
            raridade.text = InfoDeRaridade.Nome(card.raridade).ToUpper();
        }

        if (nome != null)
        {
            nome.text = card.nome;
        }

        if (descricao != null)
        {
            descricao.text = card.descricao;
        }

        if (icone != null)
        {
            bool temIcone = card.icone != null;
            icone.style.display = temIcone ? DisplayStyle.Flex : DisplayStyle.None;
            icone.style.backgroundImage = temIcone ? new StyleBackground(card.icone) : new StyleBackground(StyleKeyword.None);
        }
    }
}
