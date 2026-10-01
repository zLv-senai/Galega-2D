using UnityEngine;
using UnityEngine.UIElements;

// Tela de Vitória (Vitoria.uxml). Aparece quando o GameManager muda para o estado Vitoria e tem um botão
// que reinicia a cena. Mesmo padrão do GameOverUI: PanelRenderer + RegisterUIReloadCallback (o Unity pode
// recriar a UI, então os elementos são buscados de novo a cada reload) e style.display para mostrar/esconder.
// O GameOverUI não foi reaproveitado porque ele é fixo no estado GameOver e no texto "Level alcançado".
public class VitoriaUI : MonoBehaviour
{
    [SerializeField] private PanelRenderer painel;

    private VisualElement raiz;
    private Label subtitulo;
    private Button jogarDeNovo;

    // Se a tela deve aparecer no estado atual do jogo (reaplicado quando a UI recarrega).
    private bool visivel;

    // Total de bosses, para o subtítulo ("Você derrotou os 3 bosses"). Vem do ControladorDeBoss da cena.
    private int totalBosses = 3;

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
            Debug.LogWarning("VitoriaUI: PanelRenderer não encontrado. Arraste no Inspector ou coloque este script no mesmo objeto.");
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
        ControladorDeBoss controlador = FindAnyObjectByType<ControladorDeBoss>();
        if (controlador != null)
        {
            totalBosses = controlador.MaxBosses;
        }

        if (GameManager.Instance != null)
        {
            visivel = GameManager.Instance.gameState == GameManager.GameState.Vitoria;
        }

        Renderizar();
    }

    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        raiz = root.Q<VisualElement>("VitoriaRaiz");
        subtitulo = root.Q<Label>("VitoriaSubtitulo");
        jogarDeNovo = root.Q<Button>("VitoriaJogarDeNovo");

        if (raiz == null || subtitulo == null || jogarDeNovo == null)
        {
            Debug.LogWarning("VitoriaUI: algum elemento não foi encontrado. Confira se o PanelRenderer usa o Vitoria.uxml.");
            return;
        }

        jogarDeNovo.RegisterCallback<ClickEvent>(OnJogarDeNovoClicked);
        Renderizar();
    }

    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        visivel = estado == GameManager.GameState.Vitoria;
        Renderizar();
    }

    private void Renderizar()
    {
        if (raiz == null)
        {
            return;
        }

        if (visivel && subtitulo != null)
        {
            subtitulo.text = totalBosses == 1
                ? "Você derrotou o boss"
                : "Você derrotou os " + totalBosses + " bosses";
        }

        raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnJogarDeNovoClicked(ClickEvent evt)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("VitoriaUI: não há GameManager na cena para reiniciar.");
            return;
        }

        GameManager.Instance.Restart();
    }
}
