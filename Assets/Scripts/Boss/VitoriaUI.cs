using UnityEngine;
using UnityEngine.UIElements;

// Tela de Vitória (Vitoria.uxml). Aparece quando o GameManager muda para o estado Vitoria e tem um botão
// que reinicia a cena. Mesmo padrão do GameOverUI: PanelRenderer + RegisterUIReloadCallback (o Unity pode
// recriar a UI, então os elementos são buscados de novo a cada reload) e style.display para mostrar/esconder.
// O GameOverUI não foi reaproveitado porque ele é fixo no estado GameOver e no texto "Level alcançado".
public class VitoriaUI : MonoBehaviour
{
    [SerializeField] private PanelRenderer painel;

    // Elementos da tela de vitória: raiz, subtítulo e botão de jogar de novo (buscados a cada reload da UI).
    private VisualElement raiz;
    private Label subtitulo;
    private Button jogarDeNovo;

    // Se a tela deve aparecer no estado atual do jogo (reaplicado quando a UI recarrega).
    private bool visivel;

    // Total de bosses, para o subtítulo ("Você derrotou os 3 bosses"). Vem do ControladorDeBoss da cena.
    private int totalBosses = 3;

    // Integração (waves): na Campanha por waves o subtítulo é "Você venceu as 15 waves". Vem do GerenciadorDeWaves da cena.
    private GerenciadorDeWaves waves;

    // Acha o PanelRenderer do mesmo objeto e registra o callback de recarga da UI.
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

    // Cancela o registro do callback de recarga da UI.
    private void OnDestroy()
    {
        if (painel != null)
        {
            painel.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    // Passa a ouvir as mudanças de estado do jogo para mostrar ou esconder a tela.
    private void OnEnable()
    {
        // Evento estático: precisa cancelar no OnDisable.
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
    }

    // Para de ouvir as mudanças de estado do jogo.
    private void OnDisable()
    {
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
    }

    // Pega o total de bosses e o gerenciador de waves da cena, vê se já está em Vitoria e desenha a tela.
    private void Start()
    {
        ControladorDeBoss controlador = FindAnyObjectByType<ControladorDeBoss>();
        if (controlador != null)
        {
            totalBosses = controlador.MaxBosses;
        }

        waves = FindAnyObjectByType<GerenciadorDeWaves>();

        if (GameManager.Instance != null)
        {
            visivel = GameManager.Instance.gameState == GameManager.GameState.Vitoria;
        }

        Renderizar();
    }

    // Quando a UI é (re)criada: busca os elementos de novo, liga o clique do botão e redesenha a tela.
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

    // A tela só fica visível no estado Vitoria.
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        visivel = estado == GameManager.GameState.Vitoria;
        Renderizar();
    }

    // Atualiza o subtítulo e mostra ou esconde a tela conforme 'visivel'.
    private void Renderizar()
    {
        if (raiz == null)
        {
            return;
        }

        if (visivel && subtitulo != null)
        {
            subtitulo.text = TextoDoSubtitulo();
        }

        raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Integração (waves): com GerenciadorDeWaves na Campanha, conta as waves; senão, o texto antigo dos bosses.
    private string TextoDoSubtitulo()
    {
        if (waves == null)
        {
            waves = FindAnyObjectByType<GerenciadorDeWaves>();
        }

        if (waves != null && waves.TotalDeWaves > 0)
        {
            return "Você venceu as " + waves.TotalDeWaves + " waves";
        }

        return totalBosses == 1
            ? "Você derrotou o boss"
            : "Você derrotou os " + totalBosses + " bosses";
    }

    // Clique no botão: pede ao GameManager para jogar de novo (recarrega a cena).
    private void OnJogarDeNovoClicked(ClickEvent evt)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("VitoriaUI: não há GameManager na cena para reiniciar.");
            return;
        }

        GameManager.Instance.JogarDeNovo();
    }
}
