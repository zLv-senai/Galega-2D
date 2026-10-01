using UnityEngine;

// No Player: a cada frame (com o jogo rodando), procura Coletavel (gemas etc.) dentro
// do raio de coleta dos stats e os atrai até o player.
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerXp))]
public class Coletor : MonoBehaviour
{
    // Layers que a busca considera coletáveis (configurar no Inspector).
    [SerializeField] private LayerMask mascaraColetaveis;

    // Garante que o aviso de máscara não configurada apareça só uma vez.
    private bool avisouMascaraNaoConfigurada = false;

    // Atalhos para os componentes do Player; os coletáveis usam o Xp para dar XP (ex.: GemaDeXp).
    public PlayerXp Xp { get; private set; }
    public PlayerStats Stats { get; private set; }

    // Guarda os componentes PlayerXp e PlayerStats do Player.
    private void Awake()
    {
        Xp = GetComponent<PlayerXp>();
        Stats = GetComponent<PlayerStats>();
    }

    // Todo frame com o jogo rodando: acha os coletáveis dentro do raio de coleta e manda
    // cada um voar até o player.
    private void Update()
    {
        if (!EstadoDoJogo.Rodando || Stats == null)
        {
            return;
        }

        LayerMask mascara = ObterMascara();

        Collider2D[] encontrados = Physics2D.OverlapCircleAll(transform.position, Stats.RaioColeta, mascara);
        foreach (Collider2D col in encontrados)
        {
            Coletavel coletavel = col.GetComponent<Coletavel>();
            if (coletavel != null)
            {
                coletavel.Atrair(this);
            }
        }
    }

    // Máscara em 0 = ninguém configurou no Inspector ainda; usa todas as layers e avisa uma vez.
    private LayerMask ObterMascara()
    {
        if (mascaraColetaveis.value == 0)
        {
            if (!avisouMascaraNaoConfigurada)
            {
                Debug.LogWarning("Coletor: 'mascaraColetaveis' não configurada no Inspector. Usando todas as layers.");
                avisouMascaraNaoConfigurada = true;
            }

            return Physics2D.AllLayers;
        }

        return mascaraColetaveis;
    }

    // Só no Editor, com o Player selecionado: desenha um círculo ciano com o raio de coleta.
    private void OnDrawGizmosSelected()
    {
        PlayerStats stats = Stats != null ? Stats : GetComponent<PlayerStats>();
        float raio = stats != null ? stats.RaioColeta : 1.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}
