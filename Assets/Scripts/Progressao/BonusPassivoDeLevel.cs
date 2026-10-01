using UnityEngine;

// No Player: cada level up dá um bônus passivo pequeno (os cards agora vêm por wave, ver LevelUpManager).
// Acumula a cada level, com este componente como "fonte" dos modificadores no PlayerStats,
// e cura um pouco de vida. Não pausa o jogo (o HUD já mostra o "LEVEL UP!").
[RequireComponent(typeof(PlayerXp))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerMove))]
public class BonusPassivoDeLevel : MonoBehaviour
{
    [Header("Bônus por level")]
    [SerializeField] private float vidaMaxPorLevel = 5f;              // soma na vida máxima
    [SerializeField] private float tirosPorSegundoPorLevel = 0.03f;   // +3% de cadência (percentual)
    [SerializeField] private float velocidadePorLevel = 0.02f;        // +2% de velocidade (percentual)
    [SerializeField] private int curaPorLevel = 10;                   // vida curada, sem passar de VidaMax

    // Componentes do próprio Player, pegos no Awake.
    private PlayerXp playerXp;
    private PlayerStats stats;
    private PlayerMove playerMove;

    // Pega do Player os componentes que o bônus usa (XP, stats e movimento).
    private void Awake()
    {
        playerXp = GetComponent<PlayerXp>();
        stats = GetComponent<PlayerStats>();
        playerMove = GetComponent<PlayerMove>();
    }

    // Passa a ouvir o level up do PlayerXp para aplicar o bônus.
    private void OnEnable()
    {
        if (playerXp != null)
        {
            playerXp.AoSubirDeLevel += AplicarBonus;
        }
    }

    // Para de ouvir o level up (cancela o que foi assinado no OnEnable).
    private void OnDisable()
    {
        if (playerXp != null)
        {
            playerXp.AoSubirDeLevel -= AplicarBonus;
        }
    }

    // Chamado a cada level up: soma vida máxima, cadência de tiro e velocidade nos stats
    // e cura um pouco a vida do player.
    private void AplicarBonus(int novoLevel)
    {
        if (stats == null || playerMove == null)
        {
            return;
        }

        // Mesma fonte (este componente) em todo level up: os modificadores se acumulam.
        stats.AdicionarModificadores(new[]
        {
            new ModificadorDeStat { stat = StatTipo.VidaMax, tipo = TipoModificador.Somar, valor = vidaMaxPorLevel },
            new ModificadorDeStat { stat = StatTipo.TirosPorSegundo, tipo = TipoModificador.Percentual, valor = tirosPorSegundoPorLevel },
            new ModificadorDeStat { stat = StatTipo.Velocidade, tipo = TipoModificador.Percentual, valor = velocidadePorLevel }
        }, this);

        // Cura depois dos modificadores (o SincronizarVida já soma a diferença do VidaMax na vida atual).
        playerMove.vida = Mathf.Min(stats.VidaMax, playerMove.vida + curaPorLevel);
    }
}
