using UnityEngine;
using UnityEngine.UIElements;

// Tela de Game Over (GameOver.uxml). Aparece quando o GameManager muda para o estado GameOver,
// mostra o level alcançado e um botão que reinicia a cena. Usa o PanelRenderer no mesmo padrão
// do MenuManager/HudProgressao (RegisterUIReloadCallback) e esconde/mostra com style.display.
public class GameOverUI : MonoBehaviour
{
    // Painel da UI da tela de Game Over e o PlayerXp, de onde sai o level alcançado.
    [SerializeField] private PanelRenderer painel;
    [SerializeField] private PlayerXp playerXp;

    // Elementos do GameOver.uxml, buscados pelo nome em OnUIReload.
    private VisualElement raiz;
    private Label levelLabel;
    private Button jogarDeNovo;

    // Integração (waves): "Wave alcançada" e, no Infinito, "Recorde". Sem GerenciadorDeWaves ficam escondidos.
    private Label waveLabel;
    private Label recordeLabel;
    private Label rankingLabel;   // "Você entrou no ranking em Nº X" (só no Infinito, se entrou no top 5)
    private GerenciadorDeWaves waves;

    // Se a tela deve aparecer no estado atual do jogo (reaplicado quando a UI recarrega).
    private bool visivel;

    // Acha o PanelRenderer e o PlayerXp e registra o callback que monta a UI a cada reload.
    private void Awake()
    {
        if (painel == null)
        {
            painel = GetComponent<PanelRenderer>();
        }

        BuscarPlayerXp();

        if (painel != null)
        {
            painel.RegisterUIReloadCallback(OnUIReload);
        }
        else
        {
            Debug.LogWarning("GameOverUI: PanelRenderer não encontrado. Arraste no Inspector ou coloque este script no mesmo objeto.");
        }
    }

    // Remove o callback de reload registrado no Awake.
    private void OnDestroy()
    {
        if (painel != null)
        {
            painel.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    // Passa a ouvir a mudança de estado do jogo e o registro no ranking.
    private void OnEnable()
    {
        // Evento estático: precisa cancelar no OnDisable.
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
        Leaderboard.AoRegistrar += TratarRankingRegistrado;
    }

    // Cancela o que foi assinado no OnEnable.
    private void OnDisable()
    {
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
        Leaderboard.AoRegistrar -= TratarRankingRegistrado;
    }

    // Guarda o PlayerXp e acha o GerenciadorDeWaves (opcional) para mostrar a wave e o recorde.
    private void Start()
    {
        // O Player já existe e está ativo aqui. Guardamos a referência agora porque, no Game Over,
        // o Player é desativado e a busca por tag não o encontraria mais.
        BuscarPlayerXp();

        // Integração (waves)
        waves = FindAnyObjectByType<GerenciadorDeWaves>();
    }

    // Se o PlayerXp não foi arrastado no Inspector, pega o do Player.
    private void BuscarPlayerXp()
    {
        if (playerXp != null)
        {
            return;
        }

        GameObject jogador = Jogador.Encontrar();
        if (jogador != null)
        {
            playerXp = jogador.GetComponent<PlayerXp>();
        }
    }

    // Chamado quando a UI é criada/recriada: busca os elementos pelo nome, liga o botão e
    // desenha a tela.
    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        raiz = root.Q<VisualElement>("GameOverRaiz");
        levelLabel = root.Q<Label>("GameOverLevel");
        jogarDeNovo = root.Q<Button>("JogarDeNovo");

        // Integração (waves): opcionais; se faltarem, só não aparecem.
        waveLabel = root.Q<Label>("GameOverWave");
        recordeLabel = root.Q<Label>("GameOverRecorde");
        rankingLabel = root.Q<Label>("GameOverRanking");

        if (raiz == null || levelLabel == null || jogarDeNovo == null)
        {
            Debug.LogWarning("GameOverUI: algum elemento não foi encontrado. Confira se o PanelRenderer usa o GameOver.uxml.");
            return;
        }

        jogarDeNovo.RegisterCallback<ClickEvent>(OnJogarDeNovoClicked);
        Renderizar();
    }

    // A tela só fica visível quando o estado do jogo é GameOver.
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        visivel = estado == GameManager.GameState.GameOver;
        Renderizar();
    }

    // A ordem entre este script e o GerenciadorDeWaves no Game Over não é garantida: se o ranking só for
    // registrado depois de a tela abrir, redesenha para mostrar a posição certa.
    private void TratarRankingRegistrado(int posicao)
    {
        if (visivel)
        {
            Renderizar();
        }
    }

    // Escreve o level (e a wave) e mostra ou esconde a tela inteira conforme 'visivel'.
    private void Renderizar()
    {
        if (raiz == null)
        {
            return;
        }

        if (visivel && levelLabel != null)
        {
            levelLabel.text = playerXp != null ? "Level alcançado: " + playerXp.level : "Fim de jogo";
        }

        if (visivel)
        {
            RenderizarWaves();
        }

        raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Integração (waves): "Wave alcançada: N" e, só no Infinito, "Recorde: R".
    private void RenderizarWaves()
    {
        if (waves == null)
        {
            waves = FindAnyObjectByType<GerenciadorDeWaves>();
        }

        bool temWaves = waves != null;
        bool infinito = temWaves && waves.Modo == ModoDeJogo.Infinito;

        if (waveLabel != null)
        {
            waveLabel.style.display = temWaves ? DisplayStyle.Flex : DisplayStyle.None;
            if (temWaves)
            {
                waveLabel.text = "Wave alcançada: " + waves.WaveAtual;
            }
        }

        if (recordeLabel != null)
        {
            recordeLabel.style.display = infinito ? DisplayStyle.Flex : DisplayStyle.None;
            if (infinito)
            {
                recordeLabel.text = "Recorde: " + GerenciadorDeWaves.Recorde;
            }
        }

        // Só aparece no Infinito e se a partida entrou no top 5 (posição 0 = fora do ranking).
        if (rankingLabel != null)
        {
            int posicao = Leaderboard.UltimaPosicao;
            bool entrou = infinito && posicao > 0;
            rankingLabel.style.display = entrou ? DisplayStyle.Flex : DisplayStyle.None;
            if (entrou)
            {
                rankingLabel.text = "Você entrou no ranking em Nº " + posicao;
            }
        }
    }

    // Clique em 'Jogar de novo': pede ao GameManager para reiniciar a partida.
    private void OnJogarDeNovoClicked(ClickEvent evt)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameOverUI: não há GameManager na cena para reiniciar.");
            return;
        }

        GameManager.Instance.JogarDeNovo();
    }
}
