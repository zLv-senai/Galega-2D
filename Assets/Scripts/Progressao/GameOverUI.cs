using UnityEngine;
using UnityEngine.UIElements;

// Tela de Game Over (GameOver.uxml). Aparece quando o GameManager muda para o estado GameOver,
// mostra o level alcançado e um botão que reinicia a cena. Usa o PanelRenderer no mesmo padrão
// do MenuManager/HudProgressao (RegisterUIReloadCallback) e esconde/mostra com style.display.
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private PanelRenderer painel;
    [SerializeField] private PlayerXp playerXp;

    private VisualElement raiz;
    private Label levelLabel;
    private Button jogarDeNovo;

    // Se a tela deve aparecer no estado atual do jogo (reaplicado quando a UI recarrega).
    private bool visivel;

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

    private void OnDestroy()
    {
        if (painel != null)
        {
            painel.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnEnable()
    {
        // Evento estático: precisa cancelar no OnDisable.
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
    }

    private void OnDisable()
    {
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
    }

    private void Start()
    {
        // O Player já existe e está ativo aqui. Guardamos a referência agora porque, no Game Over,
        // o Player é desativado e a busca por tag não o encontraria mais.
        BuscarPlayerXp();
    }

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

    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        raiz = root.Q<VisualElement>("GameOverRaiz");
        levelLabel = root.Q<Label>("GameOverLevel");
        jogarDeNovo = root.Q<Button>("JogarDeNovo");

        if (raiz == null || levelLabel == null || jogarDeNovo == null)
        {
            Debug.LogWarning("GameOverUI: algum elemento não foi encontrado. Confira se o PanelRenderer usa o GameOver.uxml.");
            return;
        }

        jogarDeNovo.RegisterCallback<ClickEvent>(OnJogarDeNovoClicked);
        Renderizar();
    }

    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        visivel = estado == GameManager.GameState.GameOver;
        Renderizar();
    }

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

        raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnJogarDeNovoClicked(ClickEvent evt)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameOverUI: não há GameManager na cena para reiniciar.");
            return;
        }

        GameManager.Instance.Restart();
    }
}
