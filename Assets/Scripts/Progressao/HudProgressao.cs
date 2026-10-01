using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// HUD de progressão: level, barra de XP, vida, escudo, o aviso "LEVEL UP!", o aviso do power-up pego
// e a lista dos power-ups ativos com o tempo que falta (à direita do painel de level).
// Com GerenciadorDeWaves na cena, mostra também a wave atual, os inimigos que faltam e o anúncio "WAVE N".
// Usa o PanelRenderer no mesmo padrão do MenuManager: o Unity pode recriar a UI
// (reload), então os elementos são buscados de novo em OnUIReload.
public class HudProgressao : MonoBehaviour
{
    // Quanto tempo (em segundos) o aviso de level up fica na tela.
    private const float DuracaoLevelUp = 1.5f;

    // Quanto tempo (em segundos) o nome do power-up pego fica na tela.
    private const float DuracaoPowerUp = 1.5f;

    // Integração (boss): enfeite do alerta. O "⚠" pode não existir na fonte padrão do UI Toolkit (vira um quadradinho),
    // por isso "!!". Se a fonte do projeto tiver o glifo, troque aqui.
    private const string IconeAlerta = "!!";
    private const string TextoAlertaBoss = IconeAlerta + " BOSS SE APROXIMANDO " + IconeAlerta;
    private const string TextoAlertaBossFinal = IconeAlerta + " BOSS FINAL " + IconeAlerta;

    // Integração (boss): o alerta pisca (1 ciclo = PeriodoPiscar segundos, aceso na primeira parte do ciclo).
    private const float PeriodoPiscar = 0.8f;
    private const float FracaoAcesoPiscar = 0.6f;
    private const float OpacidadeApagado = 0.2f;

    [SerializeField] private PanelRenderer painel;
    [SerializeField] private PlayerXp playerXp;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private PlayerPowerUps playerPowerUps;

    private Label levelLabel;
    private VisualElement xpPreenchimento;
    private Label vidaLabel;
    private Label escudoLabel;
    private VisualElement levelUpPainel;
    private Label levelUpNivel;
    private Label powerUpAviso;
    private VisualElement powerUpsAtivos;

    // Integração (boss): barra de ameaça, alerta e barra de vida do boss. Os dados vêm do ControladorDeBoss
    // (achado no Start); sem ele, os elementos ficam escondidos.
    private ControladorDeBoss controladorBoss;
    private VisualElement ameacaBloco;
    private VisualElement ameacaPreenchimento;
    private Label bossAlerta;
    private VisualElement bossBarra;
    private Label bossTitulo;
    private VisualElement bossVidaPreenchimento;

    // Integração (waves): "Wave N/15" (ou com recorde), inimigos que faltam e o anúncio central. Os dados vêm do
    // GerenciadorDeWaves (achado no Start); sem ele, esses elementos ficam escondidos e a barra de ameaça volta.
    private GerenciadorDeWaves waves;
    private VisualElement waveBloco;
    private Label waveLabel;
    private Label inimigosLabel;
    private Label waveAnuncio;
    private int ultimaWave = -1;
    private int ultimoTotalWaves = -1;
    private int ultimosInimigos = -1;
    private int ultimoAnuncio = -1;
    private bool ultimoAnuncioBoss;

    // Últimos valores escritos na tela, para só mexer nos elementos quando mudar (-1 = ainda não escrito).
    private float ultimaAmeaca = -1f;
    private float ultimaFracaoVidaBoss = -1f;
    private int ultimoNumeroBoss = -1;
    private int ultimoTotalBoss = -1;

    // Uma linha na lista por power-up ativo, e os segundos escritos nela (para só mexer no texto quando mudar).
    private readonly Dictionary<PowerUpData, Label> linhasPowerUp = new Dictionary<PowerUpData, Label>();
    private readonly Dictionary<PowerUpData, int> segundosEscritos = new Dictionary<PowerUpData, int>();
    private readonly List<PowerUpData> linhasParaRemover = new List<PowerUpData>();

    // Raiz do HUD inteiro e se ele deve aparecer no estado atual do jogo (some no Menu e no Game Over).
    private VisualElement hudRaiz;
    private bool hudVisivel = true;

    // Time.unscaledTime em que o aviso some (-1 = não está aparecendo). Unscaled: funciona mesmo pausado.
    private float esconderLevelUpEm = -1f;
    private float esconderPowerUpEm = -1f;

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

        // Evento estático: precisa cancelar no OnDisable.
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
        if (GameManager.Instance != null)
        {
            TratarMudancaDeEstado(GameManager.Instance.gameState);
        }
    }

    private void OnDisable()
    {
        Desassinar();
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
    }

    // HUD visível em OnPlay, Pause e LevelUp; escondido no Menu, no Game Over e na Vitória.
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        // Integração: a Vitória também esconde o HUD (antes só Menu e GameOver).
        hudVisivel = estado != GameManager.GameState.Menu
            && estado != GameManager.GameState.GameOver
            && estado != GameManager.GameState.Vitoria;
        Mostrar(hudRaiz, hudVisivel);
    }

    private void Start()
    {
        // Aqui os Awake do Player já rodaram (ex.: PlayerMove pode ter criado o PlayerStats
        // que faltava). Busca de novo o que ficou nulo no Awake e assina o que faltou.
        BuscarReferenciasDoPlayer();
        Assinar();

        // Integração: sem ControladorDeBoss na cena, a barra de ameaça e a do boss não aparecem.
        controladorBoss = FindAnyObjectByType<ControladorDeBoss>();

        // Integração (waves): com GerenciadorDeWaves aparecem os dados da wave e some a barra de ameaça.
        waves = FindAnyObjectByType<GerenciadorDeWaves>();
        AplicarVisibilidadeBoss();
        AplicarVisibilidadeWaves();

        AtualizarTudo();
    }

    // Guarda em quem já assinou, para não assinar duas vezes nem desassinar o objeto errado.
    private PlayerXp xpAssinado;
    private PlayerStats statsAssinado;
    private PlayerPowerUps powerUpsAssinado;

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

        if (playerPowerUps != null && powerUpsAssinado == null)
        {
            playerPowerUps.AoAtivar += MostrarPowerUp;
            powerUpsAssinado = playerPowerUps;
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

        if (powerUpsAssinado != null)
        {
            powerUpsAssinado.AoAtivar -= MostrarPowerUp;
            powerUpsAssinado = null;
        }
    }

    private void Update()
    {
        AtualizarVida();
        AtualizarBoss();
        AtualizarWaves();

        if (esconderLevelUpEm >= 0f && Time.unscaledTime >= esconderLevelUpEm)
        {
            esconderLevelUpEm = -1f;
            Mostrar(levelUpPainel, false);
        }

        if (esconderPowerUpEm >= 0f && Time.unscaledTime >= esconderPowerUpEm)
        {
            esconderPowerUpEm = -1f;
            Mostrar(powerUpAviso, false);
        }

        AtualizarPowerUpsAtivos();
    }

    // Cria/remove as linhas conforme os power-ups ativos e escreve o tempo que falta (arredondado para cima).
    private void AtualizarPowerUpsAtivos()
    {
        if (powerUpsAtivos == null)
        {
            return;
        }

        IReadOnlyDictionary<PowerUpData, float> ativos = playerPowerUps != null ? playerPowerUps.ExpiraEm : null;

        linhasParaRemover.Clear();
        foreach (PowerUpData dados in linhasPowerUp.Keys)
        {
            if (ativos == null || !ativos.ContainsKey(dados))
            {
                linhasParaRemover.Add(dados);
            }
        }

        foreach (PowerUpData dados in linhasParaRemover)
        {
            linhasPowerUp[dados].RemoveFromHierarchy();
            linhasPowerUp.Remove(dados);
            segundosEscritos.Remove(dados);
        }

        if (ativos != null)
        {
            foreach (PowerUpData dados in ativos.Keys)
            {
                if (dados == null)
                {
                    continue;
                }

                if (!linhasPowerUp.TryGetValue(dados, out Label linha))
                {
                    linha = new Label();
                    linha.AddToClassList("hud-texto");
                    linha.AddToClassList("powerup-linha");
                    linha.pickingMode = PickingMode.Ignore;
                    linha.style.color = dados.cor;
                    powerUpsAtivos.Add(linha);
                    linhasPowerUp[dados] = linha;
                }

                int segundos = Mathf.CeilToInt(playerPowerUps.TempoRestante(dados));
                if (!segundosEscritos.TryGetValue(dados, out int escritos) || escritos != segundos)
                {
                    string nome = string.IsNullOrEmpty(dados.nome) ? dados.name : dados.nome;
                    linha.text = nome + "  " + segundos + "s";
                    segundosEscritos[dados] = segundos;
                }
            }
        }

        Mostrar(powerUpsAtivos, linhasPowerUp.Count > 0);
    }

    private void OnUIReload(
        PanelRenderer panel,
        VisualElement root,
        int version
    )
    {
        hudRaiz = root.Q<VisualElement>("HudRaiz");
        Mostrar(hudRaiz, hudVisivel);

        levelLabel = root.Q<Label>("LevelLabel");
        xpPreenchimento = root.Q<VisualElement>("XpPreenchimento");
        vidaLabel = root.Q<Label>("VidaLabel");
        escudoLabel = root.Q<Label>("EscudoLabel");
        levelUpPainel = root.Q<VisualElement>("LevelUpPainel");
        levelUpNivel = root.Q<Label>("LevelUpNivel");
        powerUpAviso = root.Q<Label>("PowerUpAviso");
        powerUpsAtivos = root.Q<VisualElement>("PowerUpsAtivos");

        // Integração (boss)
        ameacaBloco = root.Q<VisualElement>("AmeacaBloco");
        ameacaPreenchimento = root.Q<VisualElement>("AmeacaPreenchimento");
        bossAlerta = root.Q<Label>("BossAlerta");
        bossBarra = root.Q<VisualElement>("BossBarra");
        bossTitulo = root.Q<Label>("BossTitulo");
        bossVidaPreenchimento = root.Q<VisualElement>("BossVidaPreenchimento");

        if (ameacaBloco == null || ameacaPreenchimento == null || bossAlerta == null
            || bossBarra == null || bossTitulo == null || bossVidaPreenchimento == null)
        {
            Debug.LogWarning("HudProgressao: elementos do boss (AmeacaBloco, BossAlerta, BossBarra...) não encontrados. Confira se o PanelRenderer usa o HudProgressao.uxml atualizado.");
        }

        // Integração (waves)
        waveBloco = root.Q<VisualElement>("WaveBloco");
        waveLabel = root.Q<Label>("WaveLabel");
        inimigosLabel = root.Q<Label>("InimigosLabel");
        waveAnuncio = root.Q<Label>("WaveAnuncio");

        if (waveBloco == null || waveLabel == null || inimigosLabel == null || waveAnuncio == null)
        {
            Debug.LogWarning("HudProgressao: elementos das waves (WaveBloco, WaveLabel, InimigosLabel, WaveAnuncio) não encontrados. Confira se o PanelRenderer usa o HudProgressao.uxml atualizado.");
        }

        // A árvore nova começa com os valores do UXML: força reescrever a ameaça, a vida do boss e os dados da wave.
        ultimaAmeaca = -1f;
        ultimaFracaoVidaBoss = -1f;
        ultimoNumeroBoss = -1;
        ultimoTotalBoss = -1;
        ultimaWave = -1;
        ultimoTotalWaves = -1;
        ultimosInimigos = -1;
        ultimoAnuncio = -1;
        AplicarVisibilidadeBoss();
        AplicarVisibilidadeWaves();

        // As linhas antigas eram da árvore que foi recriada: começa a lista do zero.
        linhasPowerUp.Clear();
        segundosEscritos.Clear();

        if (levelLabel == null || xpPreenchimento == null || vidaLabel == null
            || escudoLabel == null || levelUpPainel == null || levelUpNivel == null || powerUpAviso == null
            || powerUpsAtivos == null)
        {
            Debug.LogWarning("HudProgressao: algum elemento não foi encontrado. Confira se o PanelRenderer usa o HudProgressao.uxml.");
        }

        // A árvore nova começa com os textos do UXML: reescreve tudo com os valores atuais.
        esconderLevelUpEm = -1f;
        esconderPowerUpEm = -1f;
        ultimaVida = int.MinValue;
        AtualizarTudo();
    }

    // Se as referências não foram arrastadas no Inspector, procura no objeto com a tag Player.
    private void BuscarReferenciasDoPlayer()
    {
        if (playerXp != null && playerStats != null && playerMove != null && playerPowerUps != null)
        {
            return;
        }

        GameObject jogador = Jogador.Encontrar();
        if (jogador == null)
        {
            Debug.LogWarning("HudProgressao: nenhum Player (PlayerMove) na cena.");
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

        if (playerPowerUps == null)
        {
            playerPowerUps = jogador.GetComponent<PlayerPowerUps>();
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

    // Integração (boss): sem ControladorDeBoss, esconde tudo do boss. Com ele, o AtualizarBoss controla o resto.
    private void AplicarVisibilidadeBoss()
    {
        bool temControlador = controladorBoss != null;

        // Integração (waves): com GerenciadorDeWaves a barra de ameaça fica escondida (os bosses vêm pelas waves).
        Mostrar(ameacaBloco, temControlador && waves == null);

        if (!temControlador)
        {
            Mostrar(bossAlerta, false);
            Mostrar(bossBarra, false);
        }
    }

    // Integração (waves): sem GerenciadorDeWaves, o bloco da wave e o anúncio ficam escondidos.
    private void AplicarVisibilidadeWaves()
    {
        bool temWaves = waves != null;
        Mostrar(waveBloco, temWaves);

        if (!temWaves)
        {
            Mostrar(waveAnuncio, false);
        }
    }

    // Integração (waves): lido por polling todo frame (como o boss). Só reescreve o texto quando o valor muda.
    private void AtualizarWaves()
    {
        if (waves == null)
        {
            return;
        }

        int wave = waves.WaveAtual;
        int total = waves.TotalDeWaves;

        // Antes da 1ª wave começar (ex.: logo após o Restart) não há wave para mostrar.
        Mostrar(waveBloco, wave > 0);

        if (waveLabel != null && (wave != ultimaWave || total != ultimoTotalWaves))
        {
            ultimaWave = wave;
            ultimoTotalWaves = total;
            waveLabel.text = total > 0
                ? "Wave " + wave + "/" + total
                : "Wave " + wave + "  Recorde " + GerenciadorDeWaves.Recorde;
        }

        int inimigos = waves.Restantes;
        if (inimigosLabel != null && inimigos != ultimosInimigos)
        {
            ultimosInimigos = inimigos;
            inimigosLabel.text = "Inimigos: " + inimigos;
        }

        AtualizarAnuncioDaWave(wave);
    }

    // "WAVE N" (ou "WAVE N - BOSS", em vermelho) durante o anúncio da wave.
    private void AtualizarAnuncioDaWave(int wave)
    {
        if (waveAnuncio == null)
        {
            return;
        }

        bool emAnuncio = waves.EmAnuncio;
        if (emAnuncio)
        {
            bool ehBoss = waves.WaveAtualEhBoss;
            if (wave != ultimoAnuncio || ehBoss != ultimoAnuncioBoss)
            {
                ultimoAnuncio = wave;
                ultimoAnuncioBoss = ehBoss;
                waveAnuncio.text = ehBoss ? "WAVE " + wave + " - BOSS" : "WAVE " + wave;
                waveAnuncio.EnableInClassList("wave-anuncio-boss", ehBoss);
            }
        }

        Mostrar(waveAnuncio, emAnuncio);
    }

    // Integração (boss): lido por polling todo frame (barato e à prova de reload da UI).
    // Ameaça: barra laranja. Alerta: texto piscando. Boss vivo: barra de vida no topo.
    private void AtualizarBoss()
    {
        if (controladorBoss == null)
        {
            return;
        }

        float ameaca = controladorBoss.Ameaca01;
        if (ameacaPreenchimento != null && !Mathf.Approximately(ameaca, ultimaAmeaca))
        {
            ultimaAmeaca = ameaca;
            ameacaPreenchimento.style.width = new Length(ameaca * 100f, LengthUnit.Percent);
        }

        bool emAlerta = controladorBoss.EmAlerta;
        if (bossAlerta != null && emAlerta)
        {
            bossAlerta.text = controladorBoss.EhBossFinal ? TextoAlertaBossFinal : TextoAlertaBoss;

            // Time.unscaledTime: o piscar não depende do timeScale.
            bool aceso = Mathf.Repeat(Time.unscaledTime, PeriodoPiscar) < PeriodoPiscar * FracaoAcesoPiscar;
            bossAlerta.style.opacity = aceso ? 1f : OpacidadeApagado;
        }

        Mostrar(bossAlerta, emAlerta);

        BossController boss = controladorBoss.BossAtual;
        bool bossVisivel = boss != null && boss.vida > 0;
        Mostrar(bossBarra, bossVisivel);

        if (bossVisivel)
        {
            AtualizarBarraDoBoss(boss);
        }
    }

    private void AtualizarBarraDoBoss(BossController boss)
    {
        int numero = controladorBoss.NumeroDoBoss;

        // Integração (waves): na Campanha o total vem das waves (3); no Infinito não há total ("BOSS 4").
        int total = waves != null ? waves.TotalDeBosses : controladorBoss.MaxBosses;
        if (bossTitulo != null && (numero != ultimoNumeroBoss || total != ultimoTotalBoss))
        {
            ultimoNumeroBoss = numero;
            ultimoTotalBoss = total;
            bossTitulo.text = total > 0 ? "BOSS " + numero + "/" + total : "BOSS " + numero;
        }

        float fracao = boss.VidaMaxima > 0 ? Mathf.Clamp01(boss.vida / (float)boss.VidaMaxima) : 0f;
        if (bossVidaPreenchimento != null && !Mathf.Approximately(fracao, ultimaFracaoVidaBoss))
        {
            ultimaFracaoVidaBoss = fracao;
            bossVidaPreenchimento.style.width = new Length(fracao * 100f, LengthUnit.Percent);
        }
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

    // "+ Tiro Triplo" na cor do power-up. Pegar outro antes de sumir troca o texto e reinicia o tempo.
    private void MostrarPowerUp(PowerUpData dados)
    {
        if (powerUpAviso == null || dados == null)
        {
            return;
        }

        string nome = string.IsNullOrEmpty(dados.nome) ? dados.name : dados.nome;
        powerUpAviso.text = "+ " + nome;
        powerUpAviso.style.color = dados.cor;
        Mostrar(powerUpAviso, true);
        esconderPowerUpEm = Time.unscaledTime + DuracaoPowerUp;
    }

    private static void Mostrar(VisualElement elemento, bool mostrar)
    {
        if (elemento != null)
        {
            elemento.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
