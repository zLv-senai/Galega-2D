using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Controla o estado do jogo (menu, jogando, pausa, level up, game over, vitória), o Time.timeScale
// e o reinício da cena.
public class GameManager : MonoBehaviour
{
    // Singleton simples: outros scripts acessam via GameManager.Instance
    // quando não tiverem a referência arrastada no Inspector.
    public static GameManager Instance { get; private set; }

    // Dispara depois de SetGameState aplicar o estado novo. Quem assinar (UI de Game Over,
    // HUD, etc.) deve cancelar a assinatura no OnDisable, porque o evento é estático.
    public static event System.Action<GameState> AoMudarEstado;

    // Painel do menu principal; se não estiver atribuído (cena de teste), o jogo começa direto em OnPlay.
    public PanelRenderer menuPanel;
    // Estados do jogo. Menu: tela inicial; OnPlay: jogando; GameOver e Vitoria: fim de partida;
    // Pause e LevelUp: jogo parado.
    public enum GameState
    {
        Menu,
        OnPlay,
        GameOver,
        Pause,
        // Novo valor sempre no FIM do enum: o estado é salvo como número nas cenas.
        LevelUp,
        // Integração: fim de jogo por vitória (derrotou todos os bosses, ver ControladorDeBoss). Também no FIM do enum.
        Vitoria,

    }

    // Estado atual. Para mudar, use SetGameState (ele acerta o tempo e avisa quem escuta o evento).
    public GameState gameState;

    // "Jogar de novo" pede que a próxima carga da cena pule o menu. Estático para sobreviver ao LoadScene;
    // é zerado ao entrar em Play porque o projeto roda sem Domain Reload (Enter Play Mode Options).
    private static bool pularMenuNaProximaCarga;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ZerarEstaticos()
    {
        pularMenuNaProximaCarga = false;
    }

    // Registra o singleton; um segundo GameManager na cena remove só o próprio componente.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy(this) remove só o componente duplicado: o GameManager
            // fica na Main Camera, e Destroy(gameObject) apagaria a câmera junto.
            Destroy(this);
            return;
        }

        Instance = this;
    }

    // Libera o singleton quando este GameManager é destruído.
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Escolhe o estado inicial: Menu (se há menuPanel e não veio de "Jogar de novo") ou direto OnPlay.
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bool pularMenu = pularMenuNaProximaCarga;
        pularMenuNaProximaCarga = false;

        // Cenas com menu configurado começam nele (menos depois de "Jogar de novo"). Cenas de teste sem menu
        // atribuído (menuPanel nulo) vão direto para o jogo.
        if (menuPanel != null && !pularMenu)
        {
            SetGameState(GameState.Menu);
        }
        else
        {
            SetGameState(GameState.OnPlay);
        }
    }

    // Troca o estado e acerta o tempo: Menu liga o painel do menu e congela; OnPlay desliga o menu e libera;
    // os demais congelam. No fim dispara o AoMudarEstado.
    public void SetGameState(GameState currentGameState)
    {
        gameState = currentGameState;

        switch (gameState)
        {
            case GameState.Menu:
            MenuState(true);
            break;
            case GameState.OnPlay:
            if (menuPanel != null)
            {
                menuPanel.enabled = false;
            }
            Time.timeScale = 1f;
            break;
            case GameState.GameOver:
            Time.timeScale = 0f;
            break;
            case GameState.Pause:
            case GameState.LevelUp:
            case GameState.Vitoria:
            Time.timeScale = 0f;
            break;
        }

        AoMudarEstado?.Invoke(gameState);
    }

    // Se há menuPanel, congela o tempo e liga o painel do menu.
    private void MenuState(bool state)
    {
        if (state && menuPanel != null)
        {
            Time.timeScale = 0f;
            menuPanel.enabled = true;
        }
    }

    // Recarrega a cena ativa e restaura a velocidade normal do tempo
    // (necessário porque GameOver/Pause deixam Time.timeScale em 0).
    public void Restart()
    {
        Time.timeScale = 1f;

        Scene cenaAtiva = SceneManager.GetActiveScene();
        if (cenaAtiva.buildIndex >= 0)
        {
            SceneManager.LoadScene(cenaAtiva.buildIndex);
            return;
        }

#if UNITY_EDITOR
        // Cena fora das Build Settings (ex.: cenas de teste): o LoadScene comum falha,
        // então no Editor carrega pelo caminho do arquivo da cena.
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            cenaAtiva.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(cenaAtiva.name);
#endif
    }

    // Reinicia no mesmo modo e com o mesmo nome (ConfiguracaoDePartida é estática), sem passar pelo menu.
    public void JogarDeNovo()
    {
        pularMenuNaProximaCarga = true;
        Restart();
    }
}
