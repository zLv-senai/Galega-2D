using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Menu principal (Wagner): Play, Leaderboard, Settings e Exit.
// Os outros painéis (escolha do modo, ranking e volumes) estão no mesmo MainMenu.uxml e a troca é
// por style.display, sem trocar de cena.
public class MenuManager : MonoBehaviour
{
    // GameManager da cena (arrastado no Inspector); se faltar, usa o GameManager.Instance.
    public GameManager gameManager;

    // Cursor do campo de nome: o UI Toolkit não faz ele piscar, então a cor alterna por esta classe (Menu.uss).
    private const string ClasseCursorApagado = "campo-nome--cursor-apagado";
    private const long IntervaloPiscarCursorMs = 530;

    // Painéis do MainMenu.uxml: só um fica visível por vez.
    private VisualElement[] paineis;

    // Integração: tela de controles (objeto GameControls da cena, começa desativado). Aparece depois de escolher o modo.
    // O "Jogar novamente" não passa pelo menu, então ela não aparece de novo.
    [SerializeField] private GameObject telaDeControles;
    private const string BotaoIniciar = "Iniciar";
    private ModoDeJogo modoEscolhido;
    private int versaoUiControles = -1;
    private VisualElement raizDoMenu;

    private VisualElement painelPrincipal;   // o "containerMenu" do Wagner
    private VisualElement painelModo;
    private VisualElement painelLeaderboard;
    private VisualElement painelSettings;

    // Campo de nome com o cursor piscando, lista do ranking, barras de volume e a versão da UI já ligada
    // (evita ligar os cliques duas vezes).
    private TextField campoNome;
    private IVisualElementScheduledItem piscarCursor;
    private VisualElement listaLeaderboard;
    private PainelDeVolumes volumes;
    private int versaoUi = -1;

    // Menu em roda (MenuRoda.uxml): só existe se o UXML tiver o elemento "RodaOpcoes"; sem ele (MainMenu.uxml) tudo
    // funciona como antes, só pelos cliques.
    private RodaDeOpcoes roda;
    // Gameplay decorativa de fundo (só com a roda): criada com o menu e destruída ao começar a partida.
    private FundoDoMenu fundo;
    private const float CentroYDaRoda = 425f;   // meio da área acima do rodapé, na referência 1920x1080
    private static readonly string[] DescricoesDaRoda =
    {
        "Escolha o modo e comece a partida.",
        "Veja as maiores ondas do modo Infinito.",
        "Ajuste os volumes e a tela cheia.",
        "Fecha o jogo.",
    };

    // Registra o OnUIReload, que monta o menu quando o PanelRenderer carrega o UXML.
    private void Awake()
    {
        GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);

        PanelRenderer painelDeControles = telaDeControles != null ? telaDeControles.GetComponent<PanelRenderer>() : null;
        if (painelDeControles != null)
        {
            painelDeControles.RegisterUIReloadCallback(OnControlesReload);
        }
    }

    // A tela de controles é recriada ao ser ativada: liga o botão Iniciar da árvore nova.
    private void OnControlesReload(PanelRenderer panel, VisualElement root, int version)
    {
        if (version == versaoUiControles)
        {
            return;
        }

        versaoUiControles = version;
        SomDeInterface.Ligar(root);
        LigarClique(root, BotaoIniciar, iniciarEvt => ComecarDepoisDosControles());
    }

    // Na tela de controles: Enter começa a partida e ESC volta para a escolha do modo.
    // É LateUpdate para o mesmo Enter não pausar o jogo: o MenuDePausa já rodou neste frame, ainda sem partida.
    private void LateUpdate()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null)
        {
            return;
        }

        if (telaDeControles == null || !telaDeControles.activeSelf)
        {
            LerEntradaDaRoda(teclado);
            return;
        }

        if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame)
        {
            ComecarDepoisDosControles();
        }
        else if (teclado.escapeKey.wasPressedThisFrame)
        {
            VoltarDosControles();
        }
    }

    private void OnDestroy()
    {
        DestruirFundo();
    }

    private void CriarFundo()
    {
        if (roda != null && fundo == null)
        {
            fundo = new GameObject("FundoDoMenu").AddComponent<FundoDoMenu>();
        }
    }

    private void DestruirFundo()
    {
        if (fundo != null)
        {
            Destroy(fundo.gameObject);
        }
    }

    // Menu em roda: com o painel principal à mostra (sem subpainel e sem tela de controles), as setas, W/S e o scroll
    // mudam a opção e Enter confirma. Com um subpainel aberto as teclas não mexem na roda (o campo de nome recebe texto).
    private void LerEntradaDaRoda(Keyboard teclado)
    {
        if (roda == null || !PainelPrincipalVisivel())
        {
            return;
        }

        roda.LerNavegacao();

        if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame)
        {
            roda.Confirmar();
        }
    }

    // True no estado Menu, com o painel principal visível (os outros painéis e a tela de controles fechados).
    private bool PainelPrincipalVisivel()
    {
        GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
        if (gm != null && gm.gameState != GameManager.GameState.Menu)
        {
            return false;
        }

        return painelPrincipal != null && raizDoMenu != null
            && raizDoMenu.resolvedStyle.display != DisplayStyle.None
            && painelPrincipal.resolvedStyle.display != DisplayStyle.None;
    }

    // Monta a roda se o UXML tiver o "RodaOpcoes"; devolve null no MainMenu.uxml.
    private static RodaDeOpcoes CriarRoda(VisualElement root)
    {
        if (root.Q<VisualElement>("RodaOpcoes") == null)
        {
            return null;
        }

        List<Button> botoes = new List<Button>();
        foreach (string nome in new[] { "Play_btt", "Leaderboard_btt", "Reset_btt", "Quit_btt" })
        {
            Button botao = root.Q<Button>(nome);
            if (botao == null)
            {
                Debug.LogWarning("MenuManager: botão  + nome +  não encontrado, a roda do menu não foi criada.");
                return null;
            }

            botoes.Add(botao);
        }

        return new RodaDeOpcoes(botoes, DescricoesDaRoda, root.Q<Label>("RodaDescricao"), CentroYDaRoda);
    }

    // Roda quando a UI é criada ou recriada: busca os painéis, liga os botões e volta para o menu principal.
    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        // Mesma árvore (o callback pode vir de novo com a mesma version): os cliques e o cursor já estão ligados.
        if (version == versaoUi)
        {
            return;
        }

        versaoUi = version;
        raizDoMenu = root;

        painelPrincipal = root.Q<VisualElement>("containerMenu");
        painelModo = root.Q<VisualElement>("PainelModo");
        painelLeaderboard = root.Q<VisualElement>("PainelLeaderboard");
        painelSettings = root.Q<VisualElement>("PainelSettings");
        listaLeaderboard = root.Q<VisualElement>("ListaLeaderboard");
        paineis = new[] { painelPrincipal, painelModo, painelLeaderboard, painelSettings };

        if (painelPrincipal == null || painelModo == null || painelLeaderboard == null
            || painelSettings == null || listaLeaderboard == null)
        {
            Debug.LogWarning("MenuManager: algum painel não foi encontrado. Confira se o PanelRenderer usa o MainMenu.uxml atualizado.");
        }

        // Menu principal. "Reset_btt" é o nome antigo do botão, que agora abre o Settings.
        LigarClique(root, "Play_btt", OnPlayClicked);
        LigarClique(root, "Leaderboard_btt", OnLeaderboardClicked);
        LigarClique(root, "Reset_btt", OnSettingsClicked);
        LigarClique(root, "Quit_btt", OnExitClicked);

        // Escolha do modo
        ConfigurarCampoNome(root);
        LigarClique(root, "Card_Campanha", OnCampanhaClicked);
        LigarClique(root, "Card_Infinito", OnInfinitoClicked);
        LigarClique(root, "VoltarModo_btt", OnVoltarClicked);

        // Leaderboard
        LigarClique(root, "VoltarLeaderboard_btt", OnVoltarClicked);

        // Settings
        volumes = new PainelDeVolumes(root);
        LigarClique(root, "VoltarSettings_btt", OnVoltarSettingsClicked);

        roda = CriarRoda(root);
        CriarFundo();
        SomDeInterface.Ligar(root);

        // A árvore nova começa no menu principal.
        MostrarPainel(painelPrincipal);
    }

    // Registra o clique em um botão ou card pelo name do UXML (avisa no Console se não existir).
    private static void LigarClique(VisualElement root, string nome, EventCallback<ClickEvent> aoClicar)
    {
        VisualElement alvo = root.Q<VisualElement>(nome);
        if (alvo == null)
        {
            Debug.LogWarning("MenuManager: elemento '" + nome + "' não encontrado. Confira se o PanelRenderer usa o MainMenu.uxml atualizado.");
            return;
        }

        alvo.RegisterCallback<ClickEvent>(cliqueEvt =>
        {
            GerenciadorDeSom.TocarClique();
            aoClicar(cliqueEvt);
        });
    }

    // Campo "Seu nome": máximo de 10 caracteres, começa com o último nome usado. "Piloto" fica só de fundo
    // (placeholder) e some ao clicar, deixando o cursor piscando para o jogador digitar.
    private void ConfigurarCampoNome(VisualElement root)
    {
        campoNome = root.Q<TextField>("CampoNome");
        if (campoNome == null)
        {
            Debug.LogWarning("MenuManager: campo 'CampoNome' não encontrado. A partida vai usar o nome padrão.");
            return;
        }

        campoNome.maxLength = ConfiguracaoDePartida.MaxCaracteresNome;
        campoNome.textEdition.placeholder = ConfiguracaoDePartida.NomePadrao;
        campoNome.textEdition.hidePlaceholderOnFocus = true;

        // Partida jogada sem nome salva "Piloto": ele volta como placeholder, não como texto que o jogador teria que apagar.
        string nomeSalvo = ConfiguracaoDePartida.NomeSalvo;
        campoNome.SetValueWithoutNotify(nomeSalvo == ConfiguracaoDePartida.NomePadrao ? "" : nomeSalvo);

        ConfigurarCursorPiscando();
    }

    // Com o campo em foco o cursor pisca; enquanto o jogador digita ele fica aceso.
    private void ConfigurarCursorPiscando()
    {
        piscarCursor = campoNome.schedule
            .Execute(() => campoNome.ToggleInClassList(ClasseCursorApagado))
            .Every(IntervaloPiscarCursorMs);
        piscarCursor.Pause();

        campoNome.RegisterCallback<FocusInEvent>(focoEvt => ReiniciarPiscarCursor());
        campoNome.RegisterCallback<FocusOutEvent>(focoEvt => PararPiscarCursor());
        campoNome.RegisterValueChangedCallback(textoEvt => ReiniciarPiscarCursor());
    }

    // Deixa o cursor aceso e só volta a piscar depois de um intervalo (não pisca enquanto o jogador digita).
    private void ReiniciarPiscarCursor()
    {
        campoNome.RemoveFromClassList(ClasseCursorApagado);
        piscarCursor.ExecuteLater(IntervaloPiscarCursorMs);
    }

    // Para de piscar quando o campo perde o foco e deixa o cursor aceso.
    private void PararPiscarCursor()
    {
        piscarCursor.Pause();
        campoNome.RemoveFromClassList(ClasseCursorApagado);
    }

    // Mostra só o painel pedido (os outros ficam com display none).
    private void MostrarPainel(VisualElement alvo)
    {
        if (paineis == null)
        {
            return;
        }

        foreach (VisualElement painel in paineis)
        {
            if (painel != null)
            {
                painel.style.display = painel == alvo ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }

    // Play abre a escolha do modo (a partida só começa ao clicar em um dos cards).
    private void OnPlayClicked(ClickEvent playEvt)
    {
        MostrarPainel(painelModo);
    }

    // Atualiza a lista do ranking e abre o painel do Leaderboard.
    private void OnLeaderboardClicked(ClickEvent leaderboardEvt)
    {
        RenderizarLeaderboard();
        MostrarPainel(painelLeaderboard);
    }

    // Botão "Reset_btt" (texto "Settings"): era um stub, agora abre os volumes.
    private void OnSettingsClicked(ClickEvent settingsEvt)
    {
        volumes?.Mostrar();
        MostrarPainel(painelSettings);
    }

    // Volta para o menu principal (botões Voltar da escolha de modo e do ranking).
    private void OnVoltarClicked(ClickEvent voltarEvt)
    {
        MostrarPainel(painelPrincipal);
    }

    // Os sliders só mexem na memória enquanto arrastam; ao voltar, grava os volumes em disco.
    private void OnVoltarSettingsClicked(ClickEvent voltarEvt)
    {
        ConfiguracaoDeAudio.Salvar();
        MostrarPainel(painelPrincipal);
    }

    // Escolhe o modo Campanha e mostra os controles.
    private void OnCampanhaClicked(ClickEvent campanhaEvt)
    {
        EscolherModo(ModoDeJogo.Campanha);
    }

    // Escolhe o modo Infinito e mostra os controles.
    private void OnInfinitoClicked(ClickEvent infinitoEvt)
    {
        EscolherModo(ModoDeJogo.Infinito);
    }

    // Guarda o modo, esconde os painéis do menu e abre a tela de controles. Sem a tela na cena, começa direto.
    private void EscolherModo(ModoDeJogo modo)
    {
        modoEscolhido = modo;
        DestruirFundo();

        if (telaDeControles == null)
        {
            IniciarPartida(modo);
            return;
        }

        // Esconde a raiz inteira, não só os painéis: ela ocupa a tela toda e ficaria com os cliques do botão Iniciar.
        raizDoMenu.style.display = DisplayStyle.None;
        telaDeControles.SetActive(true);
    }

    private void ComecarDepoisDosControles()
    {
        telaDeControles.SetActive(false);
        IniciarPartida(modoEscolhido);
    }

    private void VoltarDosControles()
    {
        telaDeControles.SetActive(false);
        raizDoMenu.style.display = DisplayStyle.Flex;
        MostrarPainel(painelModo);
        CriarFundo();
    }

    // Integração: guarda o nome e o modo escolhidos (o GerenciadorDeWaves lê ao entrar em OnPlay) e começa o jogo.
    private void IniciarPartida(ModoDeJogo modo)
    {
        // Nas cenas em que o campo não foi arrastado no Inspector, usa o GameManager da cena.
        GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("MenuManager: não há GameManager na cena para iniciar a partida.");
            return;
        }

        // Um menu duplicado/ativado por engano não pode jogar o jogo em OnPlay a partir de LevelUp ou GameOver.
        if (gm.gameState != GameManager.GameState.Menu)
        {
            return;
        }

        // Nome e modo só são gravados se a partida realmente vai começar.
        // Sem o campo (UXML antigo), o nome vira o padrão "Piloto".
        ConfiguracaoDePartida.DefinirNome(campoNome != null ? campoNome.value : null);
        ConfiguracaoDePartida.Modo = modo;

        gm.SetGameState(GameManager.GameState.OnPlay);
    }

    // Top 5 do modo Infinito: "1. Nome - Wave N - dd/MM/aaaa" (sem travessão: a fonte pode não ter).
    private void RenderizarLeaderboard()
    {
        if (listaLeaderboard == null)
        {
            return;
        }

        listaLeaderboard.Clear();

        IReadOnlyList<EntradaDeRanking> entradas = Leaderboard.Entradas;
        if (entradas.Count == 0)
        {
            AdicionarLinhaDoRanking("Nenhuma partida ainda", "linha-ranking-vazio");
            return;
        }

        for (int i = 0; i < entradas.Count; i++)
        {
            EntradaDeRanking entrada = entradas[i];
            string texto = (i + 1) + ". " + entrada.nome + " - Onda " + entrada.wave + " - " + entrada.DataFormatada();
            AdicionarLinhaDoRanking(texto, i == 0 ? "linha-ranking-topo" : null);
        }
    }

    // Cria uma linha de texto da lista do ranking, com uma classe USS extra opcional (ex.: destaque do 1º lugar).
    private void AdicionarLinhaDoRanking(string texto, string classeExtra)
    {
        Label linha = new Label(texto);
        linha.AddToClassList("linha-ranking");
        if (classeExtra != null)
        {
            linha.AddToClassList(classeExtra);
        }

        // O nome vem do jogador: não deixa ele usar tags de rich text (<b>, <size>...).
        linha.enableRichText = false;
        linha.pickingMode = PickingMode.Ignore;
        listaLeaderboard.Add(linha);
    }

    // Grava os volumes e fecha o jogo (no Editor, para o Play Mode).
    private void OnExitClicked(ClickEvent exitEvt)
    {
        //Criar tela de confirmação do exit no panel renderer.
        ConfiguracaoDeAudio.Salvar();
        Application.Quit();

#if UNITY_EDITOR
        // No Editor o Application.Quit não faz nada: para o Play Mode.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
