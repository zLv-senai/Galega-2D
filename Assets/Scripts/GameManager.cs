using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    // Singleton simples: outros scripts acessam via GameManager.Instance
    // quando não tiverem a referência arrastada no Inspector.
    public static GameManager Instance { get; private set; }

    public PanelRenderer menuPanel;
    public enum GameState
    {
        Menu,
        OnPlay,
        GameOver,
        Pause,

    }

    public GameState gameState;

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Cenas com menu configurado começam nele. Cenas de teste sem menu
        // atribuído (menuPanel nulo) vão direto para o jogo.
        if (menuPanel != null)
        {
            SetGameState(GameState.Menu);
        }
        else
        {
            SetGameState(GameState.OnPlay);
        }
    }

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
            Time.timeScale = 0f;
            break;
        }
    }

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
        }
        else
        {
            SceneManager.LoadScene(cenaAtiva.name);
        }
    }
}
