using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{

    public PanelRenderer menuPanel;
    public enum GameState
    {
        Menu,
        OnPlay,
        GameOver,
        Pause,

    }

    public GameState gameState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuPanel.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        SetGameState(gameState);
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
            menuPanel.enabled = false;
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
        if (state)
        {
            Time.timeScale = 0f;
            menuPanel.enabled = true;
        }
    }
}
