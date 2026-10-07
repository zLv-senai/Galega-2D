using UnityEngine;

// Controla os bosses da partida. Uma barra de "ameaça" enche com os abates de inimigos e com o tempo;
// quando enche, aparece um alerta e, depois de alguns segundos, o boss nasce fora da tela.
// Derrotando os "maxBosses", o jogo vira o estado Vitoria. Cada boss é mais forte que o anterior (BossController.Fortalecer).
// Fica num objeto da cena (menu Galega > Montar Cena Final cria o "ControladorDeBoss").
//
// Integração (waves): com um GerenciadorDeWaves na cena, ele desliga a ameaça (ameacaAtiva = false) e chama
// SurgirBoss(indice) nas waves de boss; aí quem decide a vitória é o gerenciador, não este controlador.
//
// Integração (bosses aleatórios): há 3 modelos de boss (BOSS1/2/3(FINAL) no Resources, ou os do Inspector).
// Campanha = modelo fixo pela ordem (1º boss = modelo 1, 2º = modelo 2, 3º = modelo 3).
// Infinito = SurgirBoss(..., sortearModelo: true) sorteia o modelo, sem repetir o boss anterior.
// A força continua vindo do índice (Fortalecer), não do modelo.
//
// EVENTOS: são ESTÁTICOS (mesmo padrão do EnemyMove.AoMorrer e do GameManager.AoMudarEstado). Quem assinar
// precisa cancelar no OnDisable. Só existe um ControladorDeBoss por cena.
public class ControladorDeBoss : MonoBehaviour
{
    // Nomes dentro de Assets/Resources, usados se os prefabs não forem arrastados no Inspector.
    private const string BossNoResources = "Boss";
    private const string GemaNoResources = "GemsXp";

    // Integração (bosses aleatórios): os 3 modelos de boss no Resources, na ordem da Campanha (waves 5, 10 e 15).
    private static readonly string[] BossesNoResources = { "BOSS1(FINAL)", "BOSS2(FINAL)", "BOSS3(FINAL)" };

    // Raio (em unidades) em que as gemas do boss se espalham.
    private const float RaioDasGemas = 1.5f;

    // O evento AoMudarAmeaca só dispara quando a barra muda pelo menos isto (0 a 1), para não avisar todo frame.
    private const float MudancaMinimaAmeaca = 0.002f;

    // Etapa do boss atual: acumulando ameaça, alerta antes de o boss nascer, boss vivo em campo e partida concluída.
    private enum Fase { Acumulando, Alerta, BossVivo, Concluido }

    [Header("Prefabs")]
    // Integração (bosses aleatórios): modelos de boss, na ordem da Campanha. Vazio = BOSS1/2/3(FINAL) do Resources.
    [SerializeField] private GameObject[] modelosDeBoss;
    // Reserva: só é usado se não houver nenhum modelo acima. Vazio = Resources/Boss.
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject gemaPrefab;   // solta ao boss morrer; vazio = Resources/GemsXp

    [Header("Bosses")]
    // Quantos bosses a partida tem e quantas gemas de XP cada um solta ao morrer.
    [SerializeField] private int maxBosses = 3;
    [SerializeField] private int gemasAoMorrer = 15;
    [SerializeField] private float duracaoAlerta = 3f;   // segundos entre o aviso e o boss aparecer
    [SerializeField] private float margemSpawn = 3f;     // distância para fora da borda da tela onde o boss nasce

    [Header("Ameaça")]
    // Integração (waves): false = a barra de ameaça fica desligada (o GerenciadorDeWaves desliga no Start).
    // Os bosses então só nascem por SurgirBoss(indice) e a vitória não é decidida aqui.
    public bool ameacaAtiva = true;
    [SerializeField] private float metaInicial = 30f;          // pontos para chamar o 1º boss
    [SerializeField] private float multiplicadorMeta = 1.5f;   // a meta do boss seguinte = meta atual x isto
    [SerializeField] private float pontosPorAbate = 1f;        // por inimigo morto
    [SerializeField] private float pontosPorSegundo = 0.1f;    // só de o tempo passar

    // ---- Eventos (estáticos) ----

    // Fração da barra de ameaça (0 a 1), sempre que ela muda (e quando zera).
    public static event System.Action<float> AoMudarAmeaca;

    // A barra encheu: (índice do boss que vem aí, 0 = primeiro; se é o boss final). Dura "duracaoAlerta" segundos.
    public static event System.Action<int, bool> AoAlerta;

    // O boss acabou de nascer na cena.
    public static event System.Action<BossController> AoBossSurgir;

    // Um boss morreu: (quantos já foram derrotados, total de bosses).
    public static event System.Action<int, int> AoBossDerrotado;

    // O último boss morreu (logo depois do GameManager ir para o estado Vitoria).
    public static event System.Action AoVitoria;

    // Estado: etapa atual, meta e valor da barra de ameaça, última fração avisada ao HUD, contagem do alerta,
    // bosses já derrotados e o boss em campo.
    private Fase fase = Fase.Acumulando;
    private float meta;
    private float ameaca;
    private float ultimaFracao;
    private float tempoAlerta;
    private int bossesDerrotados;
    private BossController bossAtual;

    // Integração (waves): índice (0 = primeiro) e se é o boss final, do boss que está no alerta ou em campo.
    private int indiceAtual;
    private bool ehFinalAtual;

    // Integração (bosses aleatórios): modelos carregados no Awake, se o boss do alerta é sorteado (Infinito)
    // ou fixo pelo índice (Campanha) e o último modelo sorteado (-1 = nenhum ainda), para não repetir.
    private GameObject[] modelos;
    private bool sortearAtual;
    private int ultimoModeloSorteado = -1;

    // ---- Leitura para o HUD (o HudProgressao lê isto por polling) ----

    // Integração (waves): com a ameaça desligada, a barra fica sempre em 0.
    public float Ameaca01 => ameacaAtiva && meta > 0f ? Mathf.Clamp01(ameaca / meta) : 0f;
    // Diz se o aviso está rodando (a contagem para o boss nascer).
    public bool EmAlerta => fase == Fase.Alerta;
    // Se o boss do alerta ou em campo é o último (o que fecha a partida).
    public bool EhBossFinal => ameacaAtiva ? bossesDerrotados >= MaxBosses - 1 : ehFinalAtual;
    public int BossesDerrotados => bossesDerrotados;
    // Total de bosses da partida (nunca menos que 1).
    public int MaxBosses => Mathf.Max(1, maxBosses);
    // O boss vivo em campo (null se não há nenhum).
    public BossController BossAtual => bossAtual;

    // Número (1, 2, 3...) do boss que está no alerta ou em campo. Com waves vem do índice pedido (no Infinito passa de MaxBosses).
    public int NumeroDoBoss => ameacaAtiva ? Mathf.Min(bossesDerrotados + 1, MaxBosses) : indiceAtual + 1;

    // Define a meta da primeira barra de ameaça (nunca menos que 1) e carrega os modelos de boss.
    private void Awake()
    {
        meta = Mathf.Max(1f, metaInicial);
        modelos = SorteioDeModelos.Carregar(modelosDeBoss, BossesNoResources, this);
    }

    // Passa a ouvir as mortes de inimigos (enchem a ameaça) e de bosses (encerram o boss atual).
    private void OnEnable()
    {
        EnemyMove.AoMorrer += TratarInimigoMorreu;
        BossController.AoMorrer += TratarBossMorreu;
    }

    // Para de ouvir esses eventos estáticos.
    private void OnDisable()
    {
        EnemyMove.AoMorrer -= TratarInimigoMorreu;
        BossController.AoMorrer -= TratarBossMorreu;
    }

    // Segue a etapa atual: acumula ameaça com o tempo, conta o alerta até o boss nascer
    // ou confere se o boss em campo ainda existe.
    private void Update()
    {
        // Pausa, menu, level up, game over e vitória: nada anda (o alerta usa Time.deltaTime, então também congela).
        if (!EstadoDoJogo.Rodando)
        {
            return;
        }

        switch (fase)
        {
            case Fase.Acumulando:
                AdicionarAmeaca(pontosPorSegundo * Time.deltaTime);
                break;

            case Fase.Alerta:
                tempoAlerta -= Time.deltaTime;
                if (tempoAlerta <= 0f)
                {
                    InstanciarBoss();
                }
                break;

            case Fase.BossVivo:
                // O boss sumiu sem morrer (destruído por outro script): conta como derrotado para não travar a partida.
                if (bossAtual == null)
                {
                    FinalizarBoss();
                }
                break;
        }
    }

    // Atalho para testar: clique com o botão direito no componente, em Play Mode.
    [ContextMenu("Teste: Encher ameaça")]
    private void TesteEncherAmeaca()
    {
        AdicionarAmeaca(meta);
    }

    // Cada inimigo morto durante a partida soma pontosPorAbate na barra de ameaça.
    private void TratarInimigoMorreu(EnemyMove inimigo)
    {
        if (EstadoDoJogo.Rodando)
        {
            AdicionarAmeaca(pontosPorAbate);
        }
    }

    // Só soma enquanto está acumulando: não enche durante o alerta, com boss vivo nem depois de vencer.
    private void AdicionarAmeaca(float pontos)
    {
        // Integração (waves): com a ameaça desligada ela não enche (nem pelo ContextMenu de teste).
        if (!ameacaAtiva || fase != Fase.Acumulando || pontos <= 0f)
        {
            return;
        }

        ameaca = Mathf.Min(meta, ameaca + pontos);
        bool encheu = ameaca >= meta;
        NotificarAmeaca(encheu);

        if (encheu)
        {
            IniciarAlerta(bossesDerrotados, bossesDerrotados >= MaxBosses - 1);
        }
    }

    // Avisa o HUD (AoMudarAmeaca) da fração atual da barra, só se mudou o bastante ou se 'forcar' for true.
    private void NotificarAmeaca(bool forcar)
    {
        float fracao = Ameaca01;
        if (!forcar && Mathf.Abs(fracao - ultimaFracao) < MudancaMinimaAmeaca)
        {
            return;
        }

        ultimaFracao = fracao;
        AoMudarAmeaca?.Invoke(fracao);
    }

    // Entra no alerta: guarda qual boss vem aí, começa a contagem de duracaoAlerta e avisa o HUD (AoAlerta).
    private void IniciarAlerta(int indice, bool ehFinal)
    {
        indiceAtual = indice;
        ehFinalAtual = ehFinal;
        fase = Fase.Alerta;
        tempoAlerta = Mathf.Max(0f, duracaoAlerta);
        AoAlerta?.Invoke(indiceAtual, ehFinalAtual);
    }

    // Integração (waves): chamado pelo GerenciadorDeWaves nas waves de boss. Faz o alerta (AoAlerta) e, depois de
    // "duracaoAlerta" segundos, o boss nasce fora da tela já fortalecido (Fortalecer(indice)).
    // Integração (bosses aleatórios): sortearModelo = true (Infinito) sorteia o modelo; false (Campanha) usa o
    // modelo da posição "indice" da lista.
    // Devolve false (e não faz nada) se já há um alerta ou um boss em andamento.
    public bool SurgirBoss(int indice, bool ehFinal = false, bool sortearModelo = false)
    {
        if (fase == Fase.Alerta || fase == Fase.BossVivo)
        {
            Debug.LogWarning("ControladorDeBoss: já há um boss em andamento; o pedido do boss " + (indice + 1) + " foi ignorado.", this);
            return false;
        }

        sortearAtual = sortearModelo;
        IniciarAlerta(Mathf.Max(0, indice), ehFinal);
        return true;
    }

    // Integração (waves, usado pelos testes do gerenciador): apaga o boss atual (sem contar como derrotado) e cancela o alerta.
    public void CancelarBoss()
    {
        if (bossAtual != null)
        {
            Destroy(bossAtual.gameObject);
        }

        bossAtual = null;
        fase = Fase.Acumulando;
    }

    // Antes se chamava SurgirBoss (privado): agora é só a parte que cria o boss, quando o alerta acaba.
    private void InstanciarBoss()
    {
        GameObject prefab = EscolherModelo();
        if (prefab == null)
        {
            // Sem prefab não há boss: volta a acumular em vez de ficar preso no alerta.
            Debug.LogWarning("ControladorDeBoss: sem modelos de boss (BOSS1/2/3(FINAL)), sem bossPrefab e sem 'Boss' em Assets/Resources. Rode o menu Galega > Criar Prefab do Boss.", this);
            AbandonarBoss();
            return;
        }

        GameObject criado = Instantiate(prefab, PosicaoDeSpawn(), Quaternion.identity);
        BossController boss = criado.GetComponent<BossController>();
        if (boss == null)
        {
            Debug.LogWarning("ControladorDeBoss: o prefab do boss não tem o BossController.", this);
            Destroy(criado);
            AbandonarBoss();
            return;
        }

        // Integração: usa o índice guardado no alerta (com a ameaça ligada é igual a bossesDerrotados, como antes).
        boss.Fortalecer(indiceAtual);
        bossAtual = boss;
        fase = Fase.BossVivo;
        AoBossSurgir?.Invoke(boss);
    }

    // Integração (bosses aleatórios): qual prefab nasce agora.
    //   Sorteado (Infinito): qualquer modelo, menos o do boss anterior.
    //   Fixo (Campanha e barra de ameaça): o modelo da posição do índice; passou do fim da lista, repete o último.
    // Sem nenhum modelo, usa o bossPrefab (ou Resources/Boss), como antes.
    private GameObject EscolherModelo()
    {
        if (modelos == null || modelos.Length == 0)
        {
            return bossPrefab != null ? bossPrefab : Resources.Load<GameObject>(BossNoResources);
        }

        if (!sortearAtual)
        {
            return modelos[Mathf.Clamp(indiceAtual, 0, modelos.Length - 1)];
        }

        ultimoModeloSorteado = SorteioDeModelos.SortearSemRepetir(modelos.Length, ultimoModeloSorteado);
        return modelos[ultimoModeloSorteado];
    }

    // Integração: não deu para criar o boss. Com a ameaça ligada volta a acumular (como antes); com waves conta como
    // derrotado, para a wave não ficar esperando um boss que nunca vai nascer.
    private void AbandonarBoss()
    {
        if (ameacaAtiva)
        {
            VoltarAAcumular();
            return;
        }

        FinalizarBoss();
    }

    // Zera a barra de ameaça e volta a acumular para chamar o próximo boss.
    private void VoltarAAcumular()
    {
        ameaca = 0f;
        fase = Fase.Acumulando;
        NotificarAmeaca(true);
    }

    // Quando o boss deste controlador morre: solta as gemas e conta como derrotado.
    private void TratarBossMorreu(BossController boss)
    {
        // Ignora bosses que não são os deste controlador (ex.: um boss colocado à mão em outra cena de teste).
        if (fase != Fase.BossVivo || boss != bossAtual)
        {
            return;
        }

        SoltarGemas(boss.transform.position);
        FinalizarBoss();
    }

    // Conta o boss como derrotado e decide: vitória, ou meta maior e volta a acumular ameaça.
    private void FinalizarBoss()
    {
        bossAtual = null;
        bossesDerrotados++;
        AoBossDerrotado?.Invoke(bossesDerrotados, MaxBosses);

        // Integração (waves): quem decide a vitória (e o que vem depois) é o GerenciadorDeWaves; aqui só volta ao repouso.
        if (!ameacaAtiva)
        {
            fase = Fase.Acumulando;
            return;
        }

        if (bossesDerrotados >= MaxBosses)
        {
            Vencer();
            return;
        }

        meta *= Mathf.Max(1f, multiplicadorMeta);
        VoltarAAcumular();
    }

    // Encerra a partida: muda o jogo para o estado Vitoria e avisa AoVitoria.
    private void Vencer()
    {
        fase = Fase.Concluido;

        // Não troca um Game Over que já tenha acontecido neste frame.
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.gameState != GameManager.GameState.GameOver)
        {
            gm.SetGameState(GameManager.GameState.Vitoria);
        }

        AoVitoria?.Invoke();
    }

    // Cria gemasAoMorrer gemas de XP espalhadas em volta de onde o boss morreu.
    private void SoltarGemas(Vector3 centro)
    {
        GameObject prefab = gemaPrefab != null ? gemaPrefab : Resources.Load<GameObject>(GemaNoResources);
        if (prefab == null)
        {
            Debug.LogWarning("ControladorDeBoss: sem gemaPrefab e sem 'GemsXp' em Assets/Resources. O boss não soltou gemas.", this);
            return;
        }

        for (int i = 0; i < gemasAoMorrer; i++)
        {
            Vector2 deslocamento = Random.insideUnitCircle * RaioDasGemas;
            Instantiate(prefab, centro + (Vector3)deslocamento, Quaternion.identity);
        }
    }

    // Um ponto fora da tela, em um dos quatro lados e relativo à câmera (ela segue o player).
    // Mesma ideia do EnemySpawn.SpawnPosition, mas com margem própria (o boss é grande).
    private Vector3 PosicaoDeSpawn()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return transform.position;
        }

        float altura = cam.orthographicSize;
        float largura = altura * cam.aspect;

        Vector2 deslocamento;
        switch (Random.Range(0, 4))
        {
            case 0: // cima
                deslocamento = new Vector2(Random.Range(-largura, largura), altura + margemSpawn);
                break;
            case 1: // baixo
                deslocamento = new Vector2(Random.Range(-largura, largura), -altura - margemSpawn);
                break;
            case 2: // esquerda
                deslocamento = new Vector2(-largura - margemSpawn, Random.Range(-altura, altura));
                break;
            default: // direita
                deslocamento = new Vector2(largura + margemSpawn, Random.Range(-altura, altura));
                break;
        }

        Vector3 centro = cam.transform.position;
        return new Vector3(centro.x + deslocamento.x, centro.y + deslocamento.y, 0f);
    }
}
