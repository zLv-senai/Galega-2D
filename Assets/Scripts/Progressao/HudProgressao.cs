using UnityEngine;
using UnityEngine.UIElements;

// HUD de progressão: level, barra de XP, vida, escudo e o aviso "LEVEL UP!".
// Usa o PanelRenderer no mesmo padrão do MenuManager: o Unity pode recriar a UI
// (reload), então os elementos são buscados de novo em OnUIReload.
public class HudProgressao : MonoBehaviour
{
    // Quanto tempo (em segundos) o aviso de level up fica na tela.
    private const float DuracaoLevelUp = 1.5f;

    [SerializeField] private PanelRenderer painel;
    [SerializeField] private PlayerXp playerXp;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMove playerMove;

    private Label levelLabel;
    private VisualElement xpPreenchimento;
    private Label vidaLabel;
    private Label escudoLabel;
    private VisualElement levelUpPainel;
    private Label levelUpNivel;

    // Time.unscaledTime em que o aviso some (-1 = não está aparecendo). Unscaled: funciona mesmo pausado.
    private float esconderLevelUpEm = -1f;

    // Última vida escrita na tela, para só mexer no texto quando mudar.
    private int ultimaVida = int.MinValue;
    private int ultimaVidaMax = int.MinValue;

    private void Awake()
    {
        if (painel == null)
        {
            painel = GetComponent<PanelRenderer>();
        }

        BuscarReferenciasDoPlayer();

        if (painel != null)
        {
            painel.RegisterUIReloadCallback(OnUIReload);
        }
        else
        {
            Debug.LogWarning("HudProgressao: PanelRenderer não encontrado. Arraste no Inspector ou coloque este script no mesmo objeto.");
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
        Assinar();
    }

    private void OnDisable()
    {
        Desassinar();
    }

    private void Start()
    {
        // Aqui os Awake do Player já rodaram (ex.: PlayerMove pode ter criado o PlayerStats
        // que faltava). Busca de novo o que ficou nulo no Awake e assina o que faltou.
        BuscarReferenciasDoPlayer();
        Assinar();
        AtualizarTudo();
    }

    // Guarda em quem já assinou, para não assinar duas vezes nem desassinar o objeto errado.
    private PlayerXp xpAssinado;
    private PlayerStats statsAssinado;

    private void Assinar()
    {
        if (playerXp != null && xpAssinado == null)
        {
            playerXp.AoMudarXp += AtualizarXp;
            playerXp.AoSubirDeLevel += MostrarLevelUp;
            xpAssinado = playerXp;
        }

        if (playerStats != null && statsAssinado == null)
        {
            playerStats.AoMudarCargasEscudo += AtualizarEscudo;
            statsAssinado = playerStats;
        }
    }

    private void Desassinar()
    {
        if (xpAssinado != null)
        {
            xpAssinado.AoMudarXp -= AtualizarXp;
            xpAssinado.AoSubirDeLevel -= MostrarLevelUp;
            xpAssinado = null;
        }

        if (statsAssinado != null)
        {
            statsAssinado.AoMudarCargasEscudo -= AtualizarEscudo;
            statsAssinado = null;
        }
    }

    private void Update()
    {
        AtualizarVida();

        if (esconderLevelUpEm >= 0f && Time.unscaledTime >= esconderLevelUpEm)
        {
            esconderLevelUpEm = -1f;
            Mostrar(levelUpPainel, false);
        }
    }

    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        levelLabel = root.Q<Label>("LevelLabel");
        xpPreenchimento = root.Q<VisualElement>("XpPreenchimento");
        vidaLabel = root.Q<Label>("VidaLabel");
        escudoLabel = root.Q<Label>("EscudoLabel");
        levelUpPainel = root.Q<VisualElement>("LevelUpPainel");
        levelUpNivel = root.Q<Label>("LevelUpNivel");

        if (levelLabel == null || xpPreenchimento == null || vidaLabel == null
            || escudoLabel == null || levelUpPainel == null || levelUpNivel == null)
        {
            Debug.LogWarning("HudProgressao: algum elemento não foi encontrado. Confira se o PanelRenderer usa o HudProgressao.uxml.");
        }

        // A árvore nova começa com os textos do UXML: reescreve tudo com os valores atuais.
        esconderLevelUpEm = -1f;
        ultimaVida = int.MinValue;
        AtualizarTudo();
    }

    // Se as referências não foram arrastadas no Inspector, procura no objeto com a tag Player.
    private void BuscarReferenciasDoPlayer()
    {
        if (playerXp != null && playerStats != null && playerMove != null)
        {
            return;
        }

        GameObject jogador = GameObject.FindWithTag("Player");
        if (jogador == null)
        {
            Debug.LogWarning("HudProgressao: nenhum objeto com a tag Player na cena.");
            return;
        }

        if (playerXp == null)
        {
            playerXp = jogador.GetComponent<PlayerXp>();
        }

        if (playerStats == null)
        {
            playerStats = jogador.GetComponent<PlayerStats>();
        }

        if (playerMove == null)
        {
            playerMove = jogador.GetComponent<PlayerMove>();
        }
    }

    private void AtualizarTudo()
    {
        if (playerXp != null)
        {
            AtualizarXp(playerXp.xpAtual, playerXp.XpNecessario(playerXp.level), playerXp.level);
        }

        if (playerStats != null)
        {
            AtualizarEscudo(playerStats.CargasEscudo);
        }

        AtualizarVida();
    }

    // Level e barra de XP (largura do preenchimento em %).
    private void AtualizarXp(int xpAtual, int xpNecessario, int level)
    {
        if (levelLabel != null)
        {
            levelLabel.text = "Level " + level;
        }

        if (xpPreenchimento != null)
        {
            float fracao = xpNecessario > 0 ? Mathf.Clamp01(xpAtual / (float)xpNecessario) : 0f;
            xpPreenchimento.style.width = new Length(fracao * 100f, LengthUnit.Percent);
        }
    }

    // A vida é lida por polling (o PlayerMove não tem evento de vida).
    private void AtualizarVida()
    {
        if (vidaLabel == null || playerMove == null || playerStats == null)
        {
            return;
        }

        int vida = Mathf.Max(0, playerMove.vida);
        int vidaMax = playerStats.VidaMax;
        if (vida == ultimaVida && vidaMax == ultimaVidaMax)
        {
            return;
        }

        ultimaVida = vida;
        ultimaVidaMax = vidaMax;
        vidaLabel.text = "Vida " + vida + "/" + vidaMax;
    }

    // "Escudo x2"; some quando não há cargas.
    private void AtualizarEscudo(int cargas)
    {
        if (escudoLabel != null)
        {
            escudoLabel.text = "Escudo x" + cargas;
        }

        Mostrar(escudoLabel, cargas > 0);
    }

    private void MostrarLevelUp(int novoLevel)
    {
        if (levelUpNivel != null)
        {
            levelUpNivel.text = "Level " + novoLevel;
        }

        Mostrar(levelUpPainel, true);
        esconderLevelUpEm = Time.unscaledTime + DuracaoLevelUp;
    }

    private static void Mostrar(VisualElement elemento, bool mostrar)
    {
        if (elemento != null)
        {
            elemento.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
