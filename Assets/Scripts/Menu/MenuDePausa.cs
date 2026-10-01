using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Menu de pausa (Pausa.uxml), no visual do menu principal: Continuar, Settings, Menu e Exit.
// ESC ou P pausa e despausa. Só abre durante a partida (estado OnPlay): na escolha de card, no Game Over e na
// Vitória a tecla não faz nada. Usa o estado Pause do GameManager, que já congela o tempo e o gameplay
// (EstadoDoJogo.Rodando), e troca os painéis por style.display, no mesmo padrão do MenuManager.
public class MenuDePausa : MonoBehaviour
{
    // Tempo (unscaled, o jogo está pausado) em que Menu e Exit ignoram o clique depois de pausar:
    // evita sair da partida sem querer por causa de clique rápido do tiro.
    private const float TempoAntesDeAceitarClique = 0.3f;

    // O PanelRenderer com o Pausa.uxml (se faltar, pega do mesmo objeto).
    [SerializeField] private PanelRenderer painel;

    // Elementos do Pausa.uxml: o painel inteiro, a tela principal da pausa, o Settings e as barras de volume.
    private VisualElement raiz;
    private VisualElement painelPrincipal;
    private VisualElement painelSettings;
    private PainelDeVolumes volumes;

    // Se o menu deve aparecer no estado atual do jogo (reaplicado quando a UI recarrega).
    private bool visivel;
    // Até quando Menu e Exit ignoram clique, e a versão da UI já ligada (evita registrar os cliques duas vezes).
    private float aceitaCliqueEm;
    private int versaoUi = -1;

    // Acha o PanelRenderer e registra o OnUIReload, que monta o menu quando a UI carrega.
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
            Debug.LogWarning("MenuDePausa: PanelRenderer não encontrado. Arraste no Inspector ou coloque este script no mesmo objeto.");
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

    // Ao ativar, passa a ouvir as mudanças de estado do jogo e já aplica o estado atual (se o jogo já estiver pausado).
    private void OnEnable()
    {
        // Evento estático: precisa cancelar no OnDisable.
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
        if (GameManager.Instance != null)
        {
            TratarMudancaDeEstado(GameManager.Instance.gameState);
        }
    }

    // Ao desativar, para de ouvir as mudanças de estado.
    private void OnDisable()
    {
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
    }

    // O Update roda mesmo com Time.timeScale em 0, então a mesma tecla também despausa.
    private void Update()
    {
        if (!TeclaDePausaApertada())
        {
            return;
        }

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        if (gm.gameState == GameManager.GameState.OnPlay)
        {
            gm.SetGameState(GameManager.GameState.Pause);
        }
        else if (gm.gameState == GameManager.GameState.Pause)
        {
            Continuar();
        }
    }

    // True no frame em que ESC ou P foi apertado (precisa de teclado conectado).
    private static bool TeclaDePausaApertada()
    {
        Keyboard teclado = Keyboard.current;
        return teclado != null && (teclado.escapeKey.wasPressedThisFrame || teclado.pKey.wasPressedThisFrame);
    }

    // Roda quando a UI é criada ou recriada: busca os painéis, liga os botões (Continuar, Settings, Menu e Exit)
    // e desenha.
    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        // Mesma árvore (o callback pode vir de novo com a mesma version): os cliques já estão ligados.
        if (version == versaoUi)
        {
            Renderizar();
            return;
        }

        versaoUi = version;

        raiz = root.Q<VisualElement>("PausaRaiz");
        painelPrincipal = root.Q<VisualElement>("ContainerPausa");
        painelSettings = root.Q<VisualElement>("PainelSettingsPausa");

        if (raiz == null || painelPrincipal == null || painelSettings == null)
        {
            Debug.LogWarning("MenuDePausa: algum painel não foi encontrado. Confira se o PanelRenderer usa o Pausa.uxml.");
            return;
        }

        LigarClique(root, "Continuar_btt", evt => Continuar());
        LigarClique(root, "SettingsPausa_btt", OnSettingsClicked);
        LigarClique(root, "MenuPausa_btt", OnMenuClicked);
        LigarClique(root, "SairPausa_btt", OnExitClicked);

        volumes = new PainelDeVolumes(root);
        LigarClique(root, "VoltarSettingsPausa_btt", OnVoltarSettingsClicked);

        Renderizar();
    }

    // Registra o clique em um botão pelo name do UXML (avisa no Console se não existir).
    private static void LigarClique(VisualElement root, string nome, EventCallback<ClickEvent> aoClicar)
    {
        VisualElement alvo = root.Q<VisualElement>(nome);
        if (alvo == null)
        {
            Debug.LogWarning("MenuDePausa: elemento '" + nome + "' não encontrado. Confira se o PanelRenderer usa o Pausa.uxml.");
            return;
        }

        alvo.RegisterCallback(aoClicar);
    }

    // Mostra o menu só no estado Pause; ao pausar, começa a contar o tempo em que os cliques de Menu e Exit
    // são ignorados.
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        visivel = estado == GameManager.GameState.Pause;
        if (visivel)
        {
            aceitaCliqueEm = Time.unscaledTime + TempoAntesDeAceitarClique;
        }

        Renderizar();
    }

    // Toda vez que pausa, abre na tela principal da pausa (não no Settings que ficou aberto antes).
    private void Renderizar()
    {
        if (raiz == null)
        {
            return;
        }

        if (visivel)
        {
            MostrarPainel(painelPrincipal);
        }

        raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Mostra só o painel pedido (principal ou Settings) e esconde o outro.
    private void MostrarPainel(VisualElement alvo)
    {
        painelPrincipal.style.display = alvo == painelPrincipal ? DisplayStyle.Flex : DisplayStyle.None;
        painelSettings.style.display = alvo == painelSettings ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Volta ao jogo. Grava os volumes, caso o jogador tenha mexido no Settings e despausado direto pela tecla.
    private void Continuar()
    {
        ConfiguracaoDeAudio.Salvar();

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.gameState == GameManager.GameState.Pause)
        {
            gm.SetGameState(GameManager.GameState.OnPlay);
        }
    }

    // Abre o painel de volumes já com os valores atuais nas barras.
    private void OnSettingsClicked(ClickEvent settingsEvt)
    {
        volumes?.Mostrar();
        MostrarPainel(painelSettings);
    }

    // Grava os volumes e volta para a tela principal da pausa.
    private void OnVoltarSettingsClicked(ClickEvent voltarEvt)
    {
        ConfiguracaoDeAudio.Salvar();
        MostrarPainel(painelPrincipal);
    }

    // Volta ao menu principal recarregando a cena (o GameManager começa no Menu). A partida em andamento
    // é abandonada: no Infinito ela não entra no ranking, que só registra no Game Over. O Recorde de wave
    // (PlayerPrefs) já foi gravado pelo GerenciadorDeWaves ao começar cada wave, então continua valendo.
    private void OnMenuClicked(ClickEvent menuEvt)
    {
        if (Time.unscaledTime < aceitaCliqueEm)
        {
            return;
        }

        ConfiguracaoDeAudio.Salvar();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuDePausa: não há GameManager na cena para voltar ao menu.");
            return;
        }

        GameManager.Instance.Restart();
    }

    // Grava os volumes e fecha o jogo (no Editor, para o Play Mode); ignora cliques logo depois de pausar.
    private void OnExitClicked(ClickEvent exitEvt)
    {
        if (Time.unscaledTime < aceitaCliqueEm)
        {
            return;
        }

        ConfiguracaoDeAudio.Salvar();
        Application.Quit();

#if UNITY_EDITOR
        // No Editor o Application.Quit não faz nada: para o Play Mode.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
