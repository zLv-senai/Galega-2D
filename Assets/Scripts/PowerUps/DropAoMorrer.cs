using UnityEngine;

// Fica num GameObject da cena (ex.: "Sistemas"): sempre que um inimigo morre, tem "chancePowerUp"
// de soltar um power-up sorteado da tabela no lugar dele. A gema de XP (100%) continua vindo do GeradorDeGemas.
public class DropAoMorrer : MonoBehaviour
{
    // Chance (0 a 1) de cair power-up por inimigo morto, o prefab do item e a tabela do sorteio.
    [SerializeField, Range(0f, 1f)] private float chancePowerUp = 0.2f;
    // Balanceamento: da wave de início até a final, a chance cai até este fator. As waves ficam maiores, então sem isso
    // o player passa o fim do jogo com todos os buffs ligados o tempo todo.
    [SerializeField, Range(0f, 1f)] private float fatorDaChanceNoFim = 0.5f;
    [SerializeField] private int waveDoInicioDaQueda = 6;
    [SerializeField] private int waveDoFatorFinal = 15;
    [SerializeField] private PowerUpPickup pickupPrefab;
    [SerializeField] private TabelaDePowerUps tabela;

    private GerenciadorDeWaves waves;

    // Passa a ouvir a morte de inimigos (evento estático EnemyMove.AoMorrer).
    private void OnEnable()
    {
        EnemyMove.AoMorrer += TentarSoltar;
    }

    // Para de ouvir a morte de inimigos.
    private void OnDisable()
    {
        EnemyMove.AoMorrer -= TentarSoltar;
    }

    // Chance do momento. Sem GerenciadorDeWaves na cena (cenas de teste) vale a chance cheia.
    private float ChanceAtual()
    {
        if (waves == null)
        {
            waves = FindAnyObjectByType<GerenciadorDeWaves>();
        }

        if (waves == null)
        {
            return chancePowerUp;
        }

        float progresso = Mathf.Clamp01((waves.WaveAtual - (float)waveDoInicioDaQueda) / Mathf.Max(1, waveDoFatorFinal - waveDoInicioDaQueda));
        return chancePowerUp * Mathf.Lerp(1f, fatorDaChanceNoFim, progresso);
    }

    // A cada inimigo morto, sorteia a chance; se passar, cria um pickup de power-up sorteado
    // onde ele morreu.
    private void TentarSoltar(EnemyMove inimigo)
    {
        if (inimigo == null || Random.value >= ChanceAtual())
        {
            return;
        }

        if (pickupPrefab == null || tabela == null)
        {
            Debug.LogWarning("DropAoMorrer: faltam o Pickup Prefab ou a Tabela. Nenhum power-up foi solto.", this);
            return;
        }

        PowerUpData sorteado = tabela.Sortear();
        if (sorteado == null)
        {
            return;
        }

        PowerUpPickup pickup = Instantiate(pickupPrefab, inimigo.transform.position, Quaternion.identity);
        pickup.Configurar(sorteado);
    }
}
