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

    private PlayerXp playerXp;
    private PlayerStats stats;
    private PlayerMove playerMove;

    private void Awake()
    {
        playerXp = GetComponent<PlayerXp>();
        stats = GetComponent<PlayerStats>();
        playerMove = GetComponent<PlayerMove>();
    }

    private void OnEnable()
    {
        if (playerXp != null)
        {
            playerXp.AoSubirDeLevel += AplicarBonus;
        }
    }

    private void OnDisable()
    {
        if (playerXp != null)
        {
            playerXp.AoSubirDeLevel -= AplicarBonus;
        }
    }

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
