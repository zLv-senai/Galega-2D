using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Menu principal (Wagner): Play, Leaderboard, Settings e Exit.
// Os outros painéis (escolha do modo, ranking e volumes) estão no mesmo MainMenu.uxml e a troca é
// por style.display, sem trocar de cena.
public class MenuManager : MonoBehaviour
{
    public GameManager gameManager;

    // Escala dos sliders de volume (0 a 100); o ConfiguracaoDeAudio usa 0 a 1.
    private const int SliderMaximo = 100;

    private VisualElement[] paineis;
    private VisualElement painelPrincipal;   // o "containerMenu" do Wagner
    private VisualElement painelModo;
    private VisualElement painelLeaderboard;
    private VisualElement painelSettings;

    private TextField campoNome;
    private VisualElement listaLeaderboard;
    private LinhaDeVolume linhaGeral;
    private LinhaDeVolume linhaMusica;
    private LinhaDeVolume linhaEfeitos;

    private void Awake()
    {
        GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);

    }

    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
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
        linhaGeral = LinhaDeVolume.Criar(root, "Slider_Geral", "Valor_Geral", volume => ConfiguracaoDeAudio.Geral = volume);
        linhaMusica = LinhaDeVolume.Criar(root, "Slider_Musica", "Valor_Musica", volume => ConfiguracaoDeAudio.Musica = volume);
        linhaEfeitos = LinhaDeVolume.Criar(root, "Slider_Efeitos", "Valor_Efeitos", volume => ConfiguracaoDeAudio.Efeitos = volume);
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

    // Campo "Seu nome": máximo de 10 caracteres, começa com o último nome usado ("Piloto" aparece de fundo se vazio).
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
        campoNome.SetValueWithoutNotify(ConfiguracaoDePartida.NomeSalvo);
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

    private void OnLeaderboardClicked(ClickEvent leaderboardEvt)
    {
        RenderizarLeaderboard();
        MostrarPainel(painelLeaderboard);
    }

    // Botão "Reset_btt" (texto "Settings"): era um stub, agora abre os volumes.
    private void OnSettingsClicked(ClickEvent settingsEvt)
    {
        linhaGeral?.Mostrar(ConfiguracaoDeAudio.Geral);
        linhaMusica?.Mostrar(ConfiguracaoDeAudio.Musica);
        linhaEfeitos?.Mostrar(ConfiguracaoDeAudio.Efeitos);
        MostrarPainel(painelSettings);
    }

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

    private void OnCampanhaClicked(ClickEvent campanhaEvt)
    {
        IniciarPartida(ModoDeJogo.Campanha);
    }

    private void OnInfinitoClicked(ClickEvent infinitoEvt)
    {
        IniciarPartida(ModoDeJogo.Infinito);
    }

    // Integração: guarda o nome e o modo escolhidos (o GerenciadorDeWaves lê ao entrar em OnPlay) e começa o jogo.
    private void IniciarPartida(ModoDeJogo modo)
    {
        // Sem o campo (UXML antigo), o nome vira o padrão "Piloto".
        ConfiguracaoDePartida.DefinirNome(campoNome != null ? campoNome.value : null);
        ConfiguracaoDePartida.Modo = modo;

        // Nas cenas em que o campo não foi arrastado no Inspector, usa o GameManager da cena.
        GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("MenuManager: não há GameManager na cena para iniciar a partida.");
            return;
        }

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

    // Um slider (0 a 100) com o número ao lado, ligado a um volume do ConfiguracaoDeAudio.
    private sealed class LinhaDeVolume
    {
        private readonly SliderInt slider;
        private readonly Label valor;

        private LinhaDeVolume(SliderInt slider, Label valor, Action<float> aoMudar)
        {
            this.slider = slider;
            this.valor = valor;

            slider.RegisterValueChangedCallback(evt =>
            {
                valor.text = evt.newValue.ToString();
                aoMudar(evt.newValue / (float)SliderMaximo);
            });
        }

        // null se o slider ou o número não existirem no UXML (avisa no Console).
        public static LinhaDeVolume Criar(VisualElement root, string nomeSlider, string nomeValor, Action<float> aoMudar)
        {
            SliderInt slider = root.Q<SliderInt>(nomeSlider);
            Label valor = root.Q<Label>(nomeValor);
            if (slider == null || valor == null)
            {
                Debug.LogWarning("MenuManager: '" + nomeSlider + "' ou '" + nomeValor + "' não encontrado. Confira se o PanelRenderer usa o MainMenu.uxml atualizado.");
                return null;
            }

            return new LinhaDeVolume(slider, valor, aoMudar);
        }

        // Mostra o volume atual (0 a 1) sem disparar o evento de mudança.
        public void Mostrar(float volume)
        {
            int inteiro = Mathf.RoundToInt(Mathf.Clamp01(volume) * SliderMaximo);
            slider.SetValueWithoutNotify(inteiro);
            valor.text = inteiro.ToString();
        }
    }
}
