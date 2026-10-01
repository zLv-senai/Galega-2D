using UnityEngine;

// Chama o EnemySpawn (Wagner) de tempos em tempos, ficando mais rápido com o tempo.
// Fica no mesmo objeto do EnemySpawn.
[RequireComponent(typeof(EnemySpawn))]
public class SpawnContinuo : MonoBehaviour
{
    [SerializeField] private float intervaloInicial = 2f;   // segundos entre inimigos no começo
    [SerializeField] private float intervaloMinimo = 0.4f;  // o mais rápido que pode ficar
    [SerializeField] private float reducaoPorSegundo = 0.01f; // quanto o intervalo diminui a cada segundo de jogo
    [SerializeField] private int maxInimigosVivos = 40;

    private EnemySpawn spawn;
    private float intervaloAtual;
    private float proximoSpawn;

    private void Awake()
    {
        spawn = GetComponent<EnemySpawn>();
        intervaloAtual = intervaloInicial;
    }

    private void Start()
    {
        proximoSpawn = Time.time + intervaloAtual;
    }

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

        if (FindObjectsByType<EnemyMove>(FindObjectsSortMode.None).Length >= maxInimigosVivos)
        {
            return;
        }

        spawn.SpawnEnemy();
    }
}
