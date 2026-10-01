using UnityEngine;

// No Player: controla level e XP. Separado do PlayerMove para não misturar
// movimento/combate com progressão.
public class PlayerXp : MonoBehaviour
{
    // Level atual e XP acumulado nele. O XP para subir é xpBase + incremento * (level - 1).
    public int level = 1;
    public int xpAtual = 0;
    public int xpBase = 5;
    public int incremento = 10;

    // (xpAtual, xpNecessario, level) sempre que o XP muda.
    public event System.Action<int, int, int> AoMudarXp;

    // (level novo) toda vez que o player sobe de level.
    public event System.Action<int> AoSubirDeLevel;

    // PlayerStats do Player, usado só para o multiplicador de GanhoXp.
    private PlayerStats stats;

    // Resto fracionário do XP que ainda não virou 1 ponto inteiro (ex.: GanhoXp +10% com gema de valor 1).
    private float xpFracao = 0f;

    // Guarda o PlayerStats (pode ainda não existir; o GanharXp tenta de novo).
    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    // Versão pura/estática da fórmula: não depende de instância, dá pra usar em qualquer lugar.
    public static int XpNecessario(int level, int xpBase, int incremento)
    {
        // Mathf.Max(1, ...): nunca devolve 0 ou negativo, senão o while do GanharXp não terminaria.
        return Mathf.Max(1, xpBase + incremento * (level - 1));
    }

    // Versão de instância: usa os campos deste PlayerXp.
    public int XpNecessario(int level)
    {
        return XpNecessario(level, xpBase, incremento);
    }

    // Soma XP (com o bônus de GanhoXp), sobe de level quantas vezes precisar e avisa os
    // ouvintes pelos eventos.
    public void GanharXp(int quantidade)
    {
        // PlayerStats: GanhoXp multiplica o XP recebido, se o player tiver PlayerStats.
        // O PlayerStats pode ter sido criado depois do nosso Awake (rede de segurança do PlayerMove).
        if (stats == null)
        {
            stats = GetComponent<PlayerStats>();
        }

        float multiplicador = stats != null ? stats.GanhoXp : 1f;

        // Acumula o XP em float e guarda o resto: com gema de valor 1, +10% de XP
        // rende 1 ponto extra a cada 10 gemas, em vez de arredondar para 0 (ou 2) a cada gema.
        xpFracao += Mathf.Max(0f, quantidade * multiplicador);
        int xpGanho = Mathf.FloorToInt(xpFracao);
        xpFracao -= xpGanho;

        xpAtual += xpGanho;

        while (xpAtual >= XpNecessario(level))
        {
            xpAtual -= XpNecessario(level);
            level++;
            AoSubirDeLevel?.Invoke(level);
        }

        AoMudarXp?.Invoke(xpAtual, XpNecessario(level), level);
    }
}
