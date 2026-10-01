using System.Collections.Generic;
using UnityEngine;
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

    // Registra o OnUIReload, que monta o menu quando o PanelRenderer carrega o UXML.
    private void Awake()
    {
        GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);

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

        alvo.RegisterCallback(aoClicar);
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

    // Começa uma partida no modo Campanha.
    private void OnCampanhaClicked(ClickEvent campanhaEvt)
    {
        IniciarPartida(ModoDeJogo.Campanha);
    }

    // Começa uma partida no modo Infinito.
    private void OnInfinitoClicked(ClickEvent infinitoEvt)
    {
        IniciarPartida(ModoDeJogo.Infinito);
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
            string texto = (i + 1) + ". " + entrada.nome + " - Wave " + entrada.wave + " - " + entrada.DataFormatada();
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
