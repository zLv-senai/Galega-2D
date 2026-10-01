using System.Collections.Generic;
using UnityEngine;

// Organiza a partida em waves, nos dois modos (ConfiguracaoDePartida.Modo):
//   Campanha = 15 waves; limpar a 15ª (boss final) vence o jogo.
//   Infinito = não acaba; guarda como recorde a maior wave alcançada (PlayerPrefs).
//
// Fluxo de cada wave: anúncio "WAVE N" (2 s) -> combate -> wave limpa -> oferta de 3 cards (LevelUpManager) -> próxima wave.
// Combate normal: 6 + 3*(N-1) inimigos, 1 a cada 0,6 s (EnemySpawn.SpawnEnemy), no máximo 40 vivos. A cada 3 waves os
// inimigos nascem com +1 de vida. Waves múltiplas de 5 têm boss (ControladorDeBoss.SurgirBoss) + escoltas (metade do normal).
//
// Este objeto desliga o SpawnContinuo e a barra de ameaça do ControladorDeBoss (as waves assumem esse papel).
// Só começa a contar quando o jogo entra em OnPlay (o modo vem do menu), e tudo respeita a pausa (EstadoDoJogo.Rodando).
//
// EVENTOS: são ESTÁTICOS (mesmo padrão do EnemyMove.AoMorrer). Quem assinar precisa cancelar no OnDisable.
public class GerenciadorDeWaves : MonoBehaviour
{
    // Chave do recorde (maior wave alcançada no Infinito).
    private const string ChaveRecorde = "Galega_RecordeWave";

    // Intervalo mínimo aceito entre spawns, para um valor errado no Inspector não spawnar todo frame.
    private const float IntervaloMinimoDeSpawn = 0.05f;

    // Dano usado nos testes para matar de uma vez.
    private const int DanoDeTeste = 999999;

    private enum Fase { AguardandoInicio, Anuncio, Combate, Oferta, Concluido }

    [Header("Referências (vazio = procura na cena)")]
    [SerializeField] private EnemySpawn spawn;
    [SerializeField] private ControladorDeBoss controladorBoss;
    [SerializeField] private LevelUpManager levelUpManager;

    [Header("Waves")]
    [SerializeField] private int wavesDaCampanha = 15;
    [SerializeField] private int wavesPorBoss = 5;           // a cada quantas waves vem um boss
    [SerializeField] private float duracaoAnuncio = 2f;      // segundos do aviso "WAVE N"

    [Header("Inimigos")]
    [SerializeField] private int inimigosNaPrimeiraWave = 6;
    [SerializeField] private int inimigosAMaisPorWave = 3;   // total da wave N = primeira + isto x (N-1)
    [SerializeField] private float intervaloEntreSpawns = 0.6f;
    [SerializeField] private int maxVivos = 40;
    [SerializeField] private int wavesPorBonusDeVida = 3;    // a cada quantas waves os inimigos ganham +1 de vida

    // ---- Eventos (estáticos) ----

    // Uma wave vai começar (dura "duracaoAnuncio" segundos antes do combate): (número da wave, se é de boss).
    public static event System.Action<int, bool> AoAnunciarWave;

    // Quantos inimigos (e o boss) ainda faltam para limpar a wave, sempre que o número muda.
    public static event System.Action<int> AoMudarRestantes;

    // A wave foi limpa (antes da oferta de cards, ou da vitória na última da Campanha).
    public static event System.Action<int> AoWaveLimpa;

    private Fase fase = Fase.AguardandoInicio;
    private ModoDeJogo modo = ModoDeJogo.Campanha;
    private bool ehBoss;
    private float tempoAnuncio;
    private int aSpawnar;          // inimigos da wave que ainda não nasceram
    private int bonusDeVida;
    private float proximoSpawn;
    private bool bossPendente;     // o boss da wave ainda não morreu
    private int ultimosRestantes = -1;
    private bool rankingRegistrado;   // a partida já entrou no ranking (o Game Over vale uma vez só)

    // Avisos que só aparecem uma vez, para não encher o Console.
    private bool avisouSpawnFalhou;
    private bool avisouSemBoss;
    private bool avisouSemCards;

    // Só os inimigos criados por este gerenciador (os outros, se houver, não contam para a wave).
    private readonly HashSet<EnemyMove> vivos = new HashSet<EnemyMove>();

    // Guardado num campo para o RemoveWhere de todo frame não criar lixo (o "== null" do Unity vale para objeto destruído).
    private static readonly System.Predicate<EnemyMove> EstaDestruido = inimigo => inimigo == null;

    // ---- Leitura para HUD, telas e som ----

    public int WaveAtual { get; private set; }

    // 15 na Campanha; 0 = Infinito (sem fim).
    public int TotalDeWaves => modo == ModoDeJogo.Campanha ? Mathf.Max(1, wavesDaCampanha) : 0;

    // Quantos bosses a Campanha tem (3 com 15 waves); 0 no Infinito.
    public int TotalDeBosses => TotalDeWaves > 0 ? TotalDeWaves / WavesPorBoss : 0;

    // Inimigos que ainda faltam nascer + os vivos (+ o boss, se ainda não morreu).
    public int Restantes => Mathf.Max(0, aSpawnar) + vivos.Count + (bossPendente ? 1 : 0);

    public bool EmAnuncio => fase == Fase.Anuncio;
    public bool WaveAtualEhBoss => ehBoss;
    public ModoDeJogo Modo => modo;

    // Maior wave alcançada no Infinito (0 = nenhuma ainda).
    public static int Recorde => PlayerPrefs.GetInt(ChaveRecorde, 0);

    private int WavesPorBoss => Mathf.Max(1, wavesPorBoss);

    private void Awake()
    {
        // Antes do Play o modo já fica certo para quem ler (o HUD); ComecarPartida lê de novo ao entrar em OnPlay.
        modo = ConfiguracaoDePartida.Modo;
    }

    private void OnEnable()
    {
        EnemyMove.AoMorrer += TratarInimigoMorreu;
        ControladorDeBoss.AoBossDerrotado += TratarBossDerrotado;
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
    }

    private void OnDisable()
    {
        EnemyMove.AoMorrer -= TratarInimigoMorreu;
        ControladorDeBoss.AoBossDerrotado -= TratarBossDerrotado;
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;
    }

    // Game Over no Infinito: a wave alcançada entra no ranking local (Leaderboard), uma vez por partida.
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        if (estado != GameManager.GameState.GameOver || rankingRegistrado || modo != ModoDeJogo.Infinito)
        {
            return;
        }

        rankingRegistrado = true;
        Leaderboard.Registrar(ConfiguracaoDePartida.NomeJogador, WaveAtual);
    }

    private void Start()
    {
        ResolverReferencias();
        DesligarSistemasAntigos();
    }

    private void Update()
    {
        // Pausa, menu, escolha de cards, game over e vitória: nada anda.
        if (!EstadoDoJogo.Rodando)
        {
            return;
        }

        switch (fase)
        {
            case Fase.AguardandoInicio:
                ComecarPartida();
                break;

            case Fase.Anuncio:
                tempoAnuncio -= Time.deltaTime;
                if (tempoAnuncio <= 0f)
                {
                    IniciarCombate();
                }
                break;

            case Fase.Combate:
                AtualizarCombate();
                break;
        }

        NotificarRestantes(false);
    }

    private void ResolverReferencias()
    {
        if (spawn == null)
        {
            spawn = FindAnyObjectByType<EnemySpawn>();
        }

        if (controladorBoss == null)
        {
            controladorBoss = FindAnyObjectByType<ControladorDeBoss>();
        }

        if (levelUpManager == null)
        {
            levelUpManager = FindAnyObjectByType<LevelUpManager>();
        }

        if (spawn == null)
        {
            Debug.LogError("GerenciadorDeWaves: não achei o EnemySpawn na cena. Sem ele nenhum inimigo nasce.", this);
        }
    }

    // As waves assumem o lugar do spawn contínuo e da barra de ameaça (o script deles continua no projeto).
    private void DesligarSistemasAntigos()
    {
        SpawnContinuo continuo = spawn != null ? spawn.GetComponent<SpawnContinuo>() : null;
        if (continuo == null)
        {
            continuo = FindAnyObjectByType<SpawnContinuo>();
        }

        if (continuo != null)
        {
            continuo.enabled = false;
        }

        if (controladorBoss != null)
        {
            controladorBoss.ameacaAtiva = false;
        }
    }

    // O jogo entrou em OnPlay pela primeira vez: o modo escolhido no menu já está em ConfiguracaoDePartida.
    private void ComecarPartida()
    {
        modo = ConfiguracaoDePartida.Modo;
        IniciarWave(1);
    }

    private void IniciarWave(int numero)
    {
        WaveAtual = numero;
        ehBoss = numero % WavesPorBoss == 0;
        bonusDeVida = (numero - 1) / Mathf.Max(1, wavesPorBonusDeVida);

        // Na wave de boss os inimigos comuns são as escoltas: metade da contagem normal.
        bool temBoss = ehBoss && controladorBoss != null;
        if (ehBoss && !temBoss && !avisouSemBoss)
        {
            Debug.LogWarning("GerenciadorDeWaves: wave de boss sem ControladorDeBoss na cena; ela vai ser uma wave normal.", this);
            avisouSemBoss = true;
        }

        int total = ContagemNormal(numero);
        aSpawnar = temBoss ? Mathf.Max(1, total / 2) : total;
        bossPendente = temBoss;
        vivos.Clear();

        fase = Fase.Anuncio;
        tempoAnuncio = Mathf.Max(0f, duracaoAnuncio);

        AtualizarRecorde(numero);
        NotificarRestantes(true);
        AoAnunciarWave?.Invoke(numero, ehBoss);
    }

    // Total de inimigos de uma wave normal: 6 + 3*(N-1) com os valores padrão.
    private int ContagemNormal(int numero)
    {
        return Mathf.Max(1, inimigosNaPrimeiraWave + inimigosAMaisPorWave * (numero - 1));
    }

    private void IniciarCombate()
    {
        fase = Fase.Combate;
        proximoSpawn = Time.time;

        if (bossPendente)
        {
            int indiceDoBoss = WaveAtual / WavesPorBoss - 1;
            bool ehFinal = modo == ModoDeJogo.Campanha && WaveAtual >= TotalDeWaves;
            bossPendente = controladorBoss.SurgirBoss(indiceDoBoss, ehFinal);
        }
    }

    private void AtualizarCombate()
    {
        // Inimigo destruído sem morrer (ex.: fora da tela) não pode travar a wave.
        vivos.RemoveWhere(EstaDestruido);

        TentarSpawnar();

        if (aSpawnar <= 0 && vivos.Count == 0 && !bossPendente)
        {
            ConcluirWave();
        }
    }

    private void TentarSpawnar()
    {
        if (aSpawnar <= 0 || Time.time < proximoSpawn || vivos.Count >= Mathf.Max(1, maxVivos))
        {
            return;
        }

        proximoSpawn = Time.time + Mathf.Max(IntervaloMinimoDeSpawn, intervaloEntreSpawns);
        aSpawnar--;

        GameObject criado = spawn != null ? spawn.SpawnEnemy() : null;
        EnemyMove inimigo = criado != null ? criado.GetComponent<EnemyMove>() : null;

        if (inimigo == null)
        {
            // Conta como nascido mesmo assim: a wave não pode ficar esperando um inimigo que não existe.
            if (criado != null)
            {
                Destroy(criado);
            }

            if (!avisouSpawnFalhou)
            {
                Debug.LogError("GerenciadorDeWaves: o EnemySpawn não criou um inimigo com EnemyMove (confira Resources/Enemy).", this);
                avisouSpawnFalhou = true;
            }

            return;
        }

        // Dificuldade: +1 de vida a cada "wavesPorBonusDeVida" waves.
        inimigo.vida += bonusDeVida;
        vivos.Add(inimigo);
    }

    private void TratarInimigoMorreu(EnemyMove inimigo)
    {
        if (vivos.Remove(inimigo))
        {
            NotificarRestantes(false);
        }
    }

    // O boss da wave morreu (ou sumiu): a wave só termina de vez quando as escoltas também acabarem.
    private void TratarBossDerrotado(int derrotados, int total)
    {
        if (!bossPendente)
        {
            return;
        }

        bossPendente = false;
        NotificarRestantes(false);
    }

    private void ConcluirWave()
    {
        fase = Fase.Oferta;
        NotificarRestantes(true);
        AoWaveLimpa?.Invoke(WaveAtual);

        // Última wave da Campanha: vitória, sem oferta de cards.
        if (modo == ModoDeJogo.Campanha && WaveAtual >= TotalDeWaves)
        {
            Vencer();
            return;
        }

        if (levelUpManager == null)
        {
            if (!avisouSemCards)
            {
                Debug.LogWarning("GerenciadorDeWaves: não há LevelUpManager na cena; as waves seguem sem oferecer cards.", this);
                avisouSemCards = true;
            }

            IniciarProximaWave();
            return;
        }

        levelUpManager.OferecerCartas(IniciarProximaWave, "WAVE " + WaveAtual + " CONCLUÍDA — escolha um card");
    }

    private void IniciarProximaWave()
    {
        // Só vale uma vez por oferta (e não depois de uma vitória ou de um Game Over).
        if (fase != Fase.Oferta)
        {
            return;
        }

        IniciarWave(WaveAtual + 1);
    }

    private void Vencer()
    {
        fase = Fase.Concluido;

        // Não troca um Game Over que já tenha acontecido neste frame.
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.gameState != GameManager.GameState.GameOver)
        {
            gm.SetGameState(GameManager.GameState.Vitoria);
        }
    }

    // No Infinito: a maior wave que o jogador alcançou fica salva. Atualiza ao começar cada wave.
    private void AtualizarRecorde(int numero)
    {
        if (modo != ModoDeJogo.Infinito || numero <= Recorde)
        {
            return;
        }

        PlayerPrefs.SetInt(ChaveRecorde, numero);
        PlayerPrefs.Save();
    }

    private void NotificarRestantes(bool forcar)
    {
        int restantes = Restantes;
        if (!forcar && restantes == ultimosRestantes)
        {
            return;
        }

        ultimosRestantes = restantes;
        AoMudarRestantes?.Invoke(restantes);
    }

    // ---- Testes (botão direito no componente, em Play Mode) ----

    // Mata todos os inimigos da wave (e o boss, se já nasceu) como se o jogador tivesse atirado, e para de spawnar.
    [ContextMenu("Teste: Limpar wave")]
    private void TesteLimparWave()
    {
        if (!Application.isPlaying || fase != Fase.Combate)
        {
            Debug.Log("GerenciadorDeWaves: use em Play Mode, durante o combate (depois do anúncio da wave).");
            return;
        }

        aSpawnar = 0;

        // Cópia: o TakeDamage mexe no conjunto (via AoMorrer) enquanto percorremos.
        foreach (EnemyMove inimigo in new List<EnemyMove>(vivos))
        {
            if (inimigo != null)
            {
                inimigo.TakeDamage(DanoDeTeste);
            }
        }

        BossController boss = controladorBoss != null ? controladorBoss.BossAtual : null;
        if (boss != null)
        {
            boss.TakeDamage(DanoDeTeste);
        }
        else if (bossPendente)
        {
            Debug.Log("GerenciadorDeWaves: o boss ainda está no alerta; use de novo quando ele nascer.");
        }
    }

    // Pula para a próxima wave de boss (a seguinte à atual, múltipla de wavesPorBoss), apagando o que estiver em campo.
    [ContextMenu("Teste: Pular para wave de boss")]
    private void TestePularParaWaveDeBoss()
    {
        if (!Application.isPlaying || (fase != Fase.Anuncio && fase != Fase.Combate))
        {
            Debug.Log("GerenciadorDeWaves: use em Play Mode, com uma wave em andamento (não durante a escolha de cards).");
            return;
        }

        int proxima = (WaveAtual / WavesPorBoss + 1) * WavesPorBoss;
        if (modo == ModoDeJogo.Campanha && proxima > TotalDeWaves)
        {
            Debug.Log("GerenciadorDeWaves: não há mais wave de boss nesta Campanha.");
            return;
        }

        foreach (EnemyMove inimigo in vivos)
        {
            if (inimigo != null)
            {
                Destroy(inimigo.gameObject);
            }
        }

        if (controladorBoss != null)
        {
            controladorBoss.CancelarBoss();
        }

        IniciarWave(proxima);
    }

    [ContextMenu("Teste: Zerar recorde")]
    private void TesteZerarRecorde()
    {
        PlayerPrefs.DeleteKey(ChaveRecorde);
        PlayerPrefs.Save();
    }
}
