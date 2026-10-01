using UnityEngine;

// Chama o EnemySpawn de tempos em tempos, ficando mais rápido com o tempo.
// Fica no mesmo objeto do EnemySpawn.
[RequireComponent(typeof(EnemySpawn))]
public class SpawnContinuo : MonoBehaviour
{
    [SerializeField] private float intervaloInicial = 2f;   // segundos entre inimigos no começo
    [SerializeField] private float intervaloMinimo = 0.4f;  // o mais rápido que pode ficar
    [SerializeField] private float reducaoPorSegundo = 0.01f; // quanto o intervalo diminui a cada segundo de jogo
    // Com este tanto de inimigos vivos na cena, o spawn não cria mais.
    [SerializeField] private int maxInimigosVivos = 40;

    // EnemySpawn do mesmo objeto, intervalo atual entre inimigos e o Time.time do próximo spawn.
    private EnemySpawn spawn;
    private float intervaloAtual;
    private float proximoSpawn;

    // Pega o EnemySpawn do mesmo objeto e começa com o intervalo inicial.
    private void Awake()
    {
        spawn = GetComponent<EnemySpawn>();
        intervaloAtual = intervaloInicial;
    }

    // Marca o primeiro spawn para daqui a um intervalo.
    private void Start()
    {
        proximoSpawn = Time.time + intervaloAtual;
    }

    // Com o jogo rodando, vai encurtando o intervalo e, na hora certa e abaixo do limite de inimigos, cria um novo.
    private void Update()
    {
        if (!EstadoDoJogo.Rodando)
        {
            return;
        }

        // Dificuldade: o intervalo cai aos poucos até o mínimo.
        intervaloAtual = Mathf.Max(intervaloMinimo, intervaloAtual - reducaoPorSegundo * Time.deltaTime);

        if (Time.time < proximoSpawn)
        {
            return;
        }

        proximoSpawn = Time.time + intervaloAtual;

        if (FindObjectsByType<EnemyMove>().Length >= maxInimigosVivos)
        {
            return;
        }

        spawn.SpawnEnemy();
    }
}
