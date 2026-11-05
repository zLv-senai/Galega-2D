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
    private Button voltarAoMenu;
    private Button sair;

    // Tempo (unscaled, o jogo está parado) em que os botões ignoram clique depois de a tela abrir:
    // evita escolher sem querer por causa de clique rápido do tiro.
    private const float TempoAntesDeAceitarClique = 0.5f;
    // Classe que dispara a entrada animada da tela (transição definida no GameOver.uss).
    private const string ClasseVisivel = "go-raiz--visivel";
    private float aceitaCliqueEm;

    // Integração (waves): "Wave alcançada" e, no Infinito, "Recorde". Sem GerenciadorDeWaves ficam escondidos.
    private Label waveLabel;
    private Label recordeLabel;
    private Label rankingLabel;   // "Você entrou no ranking em Nº X" (só no Infinito, se entrou no top 5)
    private GerenciadorDeWaves waves;

    // Resumo da partida: frase de fechamento, abates e tempo jogado (Time.time não anda nas pausas, então elas não contam).
    private Label subtituloLabel;
    private Label abatesLabel;
    private Label tempoLabel;
    private int abates;
    private float inicioDaPartida = -1f;
    private float tempoDePartida;

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
        EnemyMove.AoMorrer += ContarAbate;
        BossController.AoMorrer += ContarAbate;
    }

    // Cancela o que foi assinado no OnEnable.
    private void OnDisable()
    {
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
        Leaderboard.AoRegistrar -= TratarRankingRegistrado;
        EnemyMove.AoMorrer -= ContarAbate;
        BossController.AoMorrer -= ContarAbate;
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
        voltarAoMenu = root.Q<Button>("VoltarAoMenu");
        sair = root.Q<Button>("Sair");

        // Integração (waves): opcionais; se faltarem, só não aparecem.
        waveLabel = root.Q<Label>("GameOverWave");
        recordeLabel = root.Q<Label>("GameOverRecorde");
        rankingLabel = root.Q<Label>("GameOverRanking");
        subtituloLabel = root.Q<Label>("GameOverSubtitulo");
        abatesLabel = root.Q<Label>("GameOverAbates");
        tempoLabel = root.Q<Label>("GameOverTempo");

        if (raiz == null || levelLabel == null || jogarDeNovo == null)
        {
            Debug.LogWarning("GameOverUI: algum elemento não foi encontrado. Confira se o PanelRenderer usa o GameOver.uxml.");
            return;
        }

        SomDeInterface.Ligar(root);
        jogarDeNovo.RegisterCallback<ClickEvent>(OnJogarDeNovoClicked);

        // Opcionais: se o UXML não tiver esses botões, a tela continua funcionando só com o "Jogar novamente".
        voltarAoMenu?.RegisterCallback<ClickEvent>(OnVoltarAoMenuClicked);
        sair?.RegisterCallback<ClickEvent>(OnSairClicked);
        Renderizar();
    }

    // A tela só fica visível quando o estado do jogo é GameOver.
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.OnPlay && inicioDaPartida < 0f)
        {
            inicioDaPartida = Time.time;
        }

        if (estado == GameManager.GameState.GameOver && inicioDaPartida >= 0f)
        {
            tempoDePartida = Time.time - inicioDaPartida;
        }

        visivel = estado == GameManager.GameState.GameOver;
        aceitaCliqueEm = Time.unscaledTime + TempoAntesDeAceitarClique;
        Renderizar();
    }

    private void ContarAbate(EnemyMove inimigo)
    {
        abates++;
    }

    private void ContarAbate(BossController boss)
    {
        abates++;
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
            levelLabel.text = playerXp != null ? playerXp.level.ToString() : "-";
        }

        if (visivel)
        {
            RenderizarWaves();
            RenderizarResumo();
        }

        raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;

        // A classe entra no frame seguinte ao display: é a mudança de valor que faz a transição do USS rodar.
        if (!visivel)
        {
            raiz.RemoveFromClassList(ClasseVisivel);
        }
        else if (!raiz.ClassListContains(ClasseVisivel))
        {
            raiz.schedule.Execute(() => raiz.AddToClassList(ClasseVisivel));
        }
    }

    // Abates, tempo jogado (mm:ss) e a frase de fechamento.
    private void RenderizarResumo()
    {
        if (abatesLabel != null)
        {
            abatesLabel.text = abates.ToString();
        }

        if (tempoLabel != null)
        {
            int segundos = Mathf.FloorToInt(tempoDePartida);
            tempoLabel.text = (segundos / 60).ToString("00") + ":" + (segundos % 60).ToString("00");
        }

        if (subtituloLabel != null)
        {
            subtituloLabel.text = FraseDeFechamento();
        }
    }

    private string FraseDeFechamento()
    {
        if (waves == null || waves.TotalDeWaves == 0)
        {
            return "Sua nave foi destruída.";
        }

        int faltam = waves.TotalDeWaves - waves.WaveAtual;
        if (faltam <= 0)
        {
            return "Você caiu diante do chefe final. Foi por pouco!";
        }

        return faltam == 1 ? "Faltou só 1 onda para o chefe final." : "Faltaram " + faltam + " ondas para o chefe final.";
    }

    // Integração (waves): wave alcançada (card "WAVE") e, só no Infinito, "Recorde: R".
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
            // Esconde o card inteiro, não só o número.
            waveLabel.parent.style.display = temWaves ? DisplayStyle.Flex : DisplayStyle.None;
            if (temWaves)
            {
                waveLabel.text = waves.TotalDeWaves > 0 ? waves.WaveAtual + "/" + waves.TotalDeWaves : waves.WaveAtual.ToString();
            }
        }

        if (recordeLabel != null)
        {
            recordeLabel.style.display = infinito ? DisplayStyle.Flex : DisplayStyle.None;
            if (infinito)
            {
                recordeLabel.text = "Recorde: onda " + GerenciadorDeWaves.Recorde;
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
        if (Time.unscaledTime < aceitaCliqueEm)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameOverUI: não há GameManager na cena para reiniciar.");
            return;
        }

        GerenciadorDeSom.TocarClique();
        GameManager.Instance.JogarDeNovo();
    }

    // Clique em 'Voltar ao menu principal': recarrega a cena começando pelo menu.
    private void OnVoltarAoMenuClicked(ClickEvent evt)
    {
        if (Time.unscaledTime < aceitaCliqueEm)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameOverUI: não há GameManager na cena para voltar ao menu.");
            return;
        }

        GerenciadorDeSom.TocarClique();
        GameManager.Instance.VoltarAoMenu();
    }

    // Clique em 'Sair': grava os volumes e fecha o jogo.
    private void OnSairClicked(ClickEvent evt)
    {
        if (Time.unscaledTime < aceitaCliqueEm)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameOverUI: não há GameManager na cena para sair.");
            return;
        }

        GerenciadorDeSom.TocarClique();
        GameManager.Instance.Sair();
    }
}
