using System.Collections.Generic;
using UnityEngine;

// Fica num objeto de sistemas da cena. Escuta o level up do player, conta os levels
// pendentes (vários de uma vez = várias escolhas em sequência), pausa o jogo no estado
// LevelUp, sorteia 3 cards e aplica o que o jogador escolher. A tela fica no LevelUpUI.
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

    // Guarda em quem já assinou, para não assinar duas vezes nem desassinar o objeto errado.
    private PlayerXp xpAssinado;

    private void OnEnable()
    {
        Assinar();
    }

    private void Start()
    {
        // Se o Player ainda não existia no OnEnable, tenta de novo aqui.
        Assinar();
    }

    private void OnDisable()
    {
        if (xpAssinado != null)
        {
            xpAssinado.AoSubirDeLevel -= TratarLevelUp;
            xpAssinado = null;
        }
    }

    private void Update()
    {
        if (pendentes <= 0 || mostrando || !EstadoDoJogo.Rodando)
        {
            return;
        }

        IniciarEscolha();
    }

    private void Assinar()
    {
        if (playerXp == null)
        {
            GameObject jogador = GameObject.FindWithTag("Player");
            if (jogador != null)
            {
                playerXp = jogador.GetComponent<PlayerXp>();
            }
        }

        if (playerXp == null)
        {
            return;
        }

        if (playerStats == null)
        {
            playerStats = playerXp.GetComponent<PlayerStats>();
        }

        if (xpAssinado == null)
        {
            playerXp.AoSubirDeLevel += TratarLevelUp;
            xpAssinado = playerXp;
        }
    }

    // Cada level novo vira uma escolha pendente. O Update abre a tela quando o jogo estiver rodando.
    private void TratarLevelUp(int novoLevel)
    {
        pendentes++;
    }

    // Sorteia e mostra uma oferta. Na primeira da sequência, pausa o jogo (estado LevelUp).
    private void IniciarEscolha()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("LevelUpManager: não há GameManager na cena, então não dá para pausar e mostrar os cards.");
            pendentes = 0;
            return;
        }

        // Banco não configurado é erro de montagem: guarda os levels pendentes (não perde nada)
        // e avisa uma vez só, para o Update não encher o Console tentando de novo todo frame.
        if (!BancoTemCards())
        {
            if (!avisouBancoVazio)
            {
                Debug.LogError("LevelUpManager: o campo Banco está vazio ou o BancoDeCards não tem cards. " +
                    "Arraste o asset Assets/Data/BancoDeCards (não o script) no campo Banco. " +
                    "Os levels ficam guardados e os cards aparecem quando o banco for preenchido.", this);
                avisouBancoVazio = true;
            }

            return;
        }

        avisouBancoVazio = false;

        ofertaAtual = SorteadorDeCards.Sortear(banco, escolhas);
        if (ofertaAtual.Length == 0)
        {
            // Aqui o banco tem cards, mas todos já chegaram no maxEscolhas: não há o que oferecer.
            Debug.Log("LevelUpManager: todos os cards já chegaram no limite de escolhas; level up sem cards.");
            pendentes = 0;
            ofertaAtual = null;
            if (mostrando)
            {
                Encerrar();
            }

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

    // Fecha a tela e volta a OnPlay, a não ser que o estado já tenha mudado (ex.: Game Over).
    private void Encerrar()
    {
        mostrando = false;
        ofertaAtual = null;
        AoEncerrar?.Invoke();

        if (GameManager.Instance != null && GameManager.Instance.gameState == GameManager.GameState.LevelUp)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.OnPlay);
        }
    }

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
