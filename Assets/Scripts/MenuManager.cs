using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    public GameManager gameManager;
    
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
        Button playBtt = root.Q<Button>("Play_btt");
        playBtt.RegisterCallback<ClickEvent>(OnPlayClicked);


        Button resetBtt = root.Q<Button>("Reset_btt");
        resetBtt.RegisterCallback<ClickEvent>(OnResetClicked);

        Button exitBtt = root.Q<Button>("Quit_btt");
        exitBtt.RegisterCallback<ClickEvent>(OnExitClicked);
        
    }

    private void OnPlayClicked(ClickEvent playEvt)
    {
        Debug.Log("Play Clicked!");
        gameManager.SetGameState(GameManager.GameState.OnPlay);
    }

    private void OnExitClicked(ClickEvent exitEvt)
    {
        Debug.Log("Exit Clicked!");
        //Criar tela de confirmação do exit no panel renderer.
        Application.Quit();
    }

    private void OnResetClicked(ClickEvent resetEvt)
    {
        Debug.Log("Reset Clicked!");
    }
    }
