using System.Collections.Generic;
using UnityEngine;

// Fica num objeto de sistemas da cena. Quem pede os cards é o GerenciadorDeWaves, quando uma wave é limpa
// (OferecerCartas): pausa o jogo no estado LevelUp, sorteia 3 cards, aplica o que o jogador escolher e,
// no fim, avisa quem pediu (aoTerminar). A tela fica no LevelUpUI.
// Integração: subir de level NÃO abre mais cards (virou bônus passivo, ver BonusPassivoDeLevel).
public class LevelUpManager : MonoBehaviour
{
    // Tempo (unscaled, o jogo está pausado) em que cliques são ignorados depois de mostrar
    // cards novos: evita escolher sem querer por causa de clique rápido do tiro.
    private const float TempoAntesDeAceitarClique = 0.3f;

    [SerializeField] private BancoDeCards banco;
    [SerializeField] private PlayerXp playerXp;
    [SerializeField] private PlayerStats playerStats;

    // (cards oferecidos, escolhas ainda pendentes contando esta) sempre que uma oferta aparece.
    public event System.Action<CardData[], int> AoOferecerCards;

    // Quando o jogador escolhe um card (para sons, etc.).
    public event System.Action<CardData> AoEscolherCard;

    // Quando não há mais o que escolher e o jogo volta a rodar.
    public event System.Action AoEncerrar;

    // Quantas vezes cada card já foi escolhido (para respeitar maxEscolhas).
    private readonly Dictionary<CardData, int> escolhas = new Dictionary<CardData, int>();

    private int pendentes;
    private bool mostrando;
    private CardData[] ofertaAtual;
    private float aceitaCliqueEm;
    private bool avisouBancoVazio;
    private bool avisouCardsEsgotados;

    // Integração (waves): o que chamar quando a oferta termina, e o título que a tela mostra (null = título padrão).
    private System.Action aoTerminar;
    private string tituloAtual;

    // Título pedido em OferecerCartas (ex.: "WAVE 3 CONCLUÍDA"). O LevelUpUI lê ao mostrar a oferta.
    public string TituloAtual => tituloAtual;

    private void OnEnable()
    {
        ResolverReferencias();
    }

    private void Start()
    {
        // Se o Player ainda não existia no OnEnable, tenta de novo aqui.
        ResolverReferencias();
    }

    private void Update()
    {
        if (pendentes <= 0 || mostrando || !EstadoDoJogo.Rodando)
        {
            return;
        }

        IniciarEscolha();
    }

    // Só acha o PlayerStats (onde os cards aplicam os modificadores). Antes este método também assinava o level up do player.
    private void ResolverReferencias()
    {
        if (playerXp == null)
        {
            GameObject jogador = Jogador.Encontrar();
            if (jogador != null)
            {
                playerXp = jogador.GetComponent<PlayerXp>();
            }
        }

        if (playerStats == null && playerXp != null)
        {
            playerStats = playerXp.GetComponent<PlayerStats>();
        }
    }

    // Pede uma escolha de card (3 opções). O jogo pausa quando o Update puder abrir a tela e, depois que o
    // jogador escolher, volta a OnPlay e chama "aoTerminar". Sem cards para oferecer, chama "aoTerminar" direto.
    // "titulo" é opcional (null = título padrão da tela).
    public void OferecerCartas(System.Action aoTerminar, string titulo = null)
    {
        this.aoTerminar += aoTerminar;

        if (!string.IsNullOrEmpty(titulo))
        {
            tituloAtual = titulo;
        }

        pendentes++;
    }

    // Sorteia e mostra uma oferta. Na primeira da sequência, pausa o jogo (estado LevelUp).
    private void IniciarEscolha()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("LevelUpManager: não há GameManager na cena, então não dá para pausar e mostrar os cards.");
            pendentes = 0;
            ConcluirOferta();
            return;
        }

        // Banco não configurado é erro de montagem: avisa uma vez só e segue sem cards,
        // para a wave não ficar esperando uma escolha que nunca vai acontecer.
        if (!BancoTemCards())
        {
            if (!avisouBancoVazio)
            {
                Debug.LogError("LevelUpManager: o campo Banco está vazio ou o BancoDeCards não tem cards. " +
                    "Arraste o asset Assets/Data/BancoDeCards (não o script) no campo Banco. " +
                    "Enquanto isso, as waves seguem sem oferecer cards.", this);
                avisouBancoVazio = true;
            }

            pendentes = 0;
            FinalizarSemOferta();
            return;
        }

        avisouBancoVazio = false;

        ofertaAtual = SorteadorDeCards.Sortear(banco, escolhas);
        if (ofertaAtual.Length == 0)
        {
            // Aqui o banco tem cards, mas todos já chegaram no maxEscolhas: não há o que oferecer.
            // Avisa uma vez só: a cada wave limpa cairia aqui de novo.
            if (!avisouCardsEsgotados)
            {
                Debug.Log("LevelUpManager: todos os cards já chegaram no limite de escolhas; seguindo sem cards.");
                avisouCardsEsgotados = true;
            }

            pendentes = 0;
            ofertaAtual = null;
            FinalizarSemOferta();
            return;
        }

        if (!mostrando)
        {
            mostrando = true;
            GameManager.Instance.SetGameState(GameManager.GameState.LevelUp);
        }

        aceitaCliqueEm = Time.unscaledTime + TempoAntesDeAceitarClique;
        AoOferecerCards?.Invoke(ofertaAtual, pendentes);
    }

    // Sem oferta para mostrar: se a tela já estava aberta (2ª escolha em diante), fecha; senão só avisa quem pediu.
    private void FinalizarSemOferta()
    {
        if (mostrando)
        {
            Encerrar();
        }
        else
        {
            ConcluirOferta();
        }
    }

    private bool BancoTemCards()
    {
        if (banco == null || banco.cards == null)
        {
            return false;
        }

        foreach (CardData card in banco.cards)
        {
            if (card != null)
            {
                return true;
            }
        }

        return false;
    }

    // Chamado pela UI quando o jogador clica num card.
    public void Escolher(CardData card)
    {
        if (!mostrando || card == null || ofertaAtual == null || System.Array.IndexOf(ofertaAtual, card) < 0)
        {
            return;
        }

        if (Time.unscaledTime < aceitaCliqueEm)
        {
            return;
        }

        // A fonte do modificador é o próprio card: escolher o mesmo card de novo soma de novo.
        if (playerStats != null)
        {
            playerStats.AdicionarModificadores(card.modificadores, card);
        }

        escolhas.TryGetValue(card, out int vezes);
        escolhas[card] = vezes + 1;

        pendentes = Mathf.Max(0, pendentes - 1);
        AoEscolherCard?.Invoke(card);

        if (pendentes > 0)
        {
            IniciarEscolha();
        }
        else
        {
            Encerrar();
        }
    }

    // Fecha a tela e volta a OnPlay, a não ser que o estado já tenha mudado (ex.: Game Over). Depois avisa quem pediu.
    private void Encerrar()
    {
        mostrando = false;
        ofertaAtual = null;
        AoEncerrar?.Invoke();

        if (GameManager.Instance != null && GameManager.Instance.gameState == GameManager.GameState.LevelUp)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.OnPlay);
        }

        ConcluirOferta();
    }

    // Chama (uma vez) o que foi pedido em OferecerCartas e limpa o título.
    private void ConcluirOferta()
    {
        System.Action terminar = aoTerminar;
        aoTerminar = null;
        tituloAtual = null;
        terminar?.Invoke();
    }

    // Teste: abre uma oferta de cards sem wave nenhuma (o jogo só volta ao normal, sem aoTerminar).
    [ContextMenu("Forçar Level Up")]
    private void ForcarLevelUp()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        pendentes++;
    }
}
