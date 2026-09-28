using UnityEngine;

// No Player: controla level e XP. Separado do PlayerMove para não misturar
// movimento/combate com progressão.
public class PlayerXp : MonoBehaviour
{
    public int level = 1;
    public int xpAtual = 0;
    public int xpBase = 5;
    public int incremento = 10;

    // (xpAtual, xpNecessario, level) sempre que o XP muda.
    public event System.Action<int, int, int> AoMudarXp;

    // (level novo) toda vez que o player sobe de level.
    public event System.Action<int> AoSubirDeLevel;

    private PlayerStats stats;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    // Versão pura/estática da fórmula: não depende de instância, dá pra usar em qualquer lugar.
    public static int XpNecessario(int level, int xpBase, int incremento)
    {
        return xpBase + incremento * (level - 1);
    }

    // Versão de instância: usa os campos deste PlayerXp.
    public int XpNecessario(int level)
    {
        return XpNecessario(level, xpBase, incremento);
    }

    public void GanharXp(int quantidade)
    {
        // PlayerStats: GanhoXp multiplica o XP recebido, se o player tiver PlayerStats.
        float multiplicador = stats != null ? stats.GanhoXp : 1f;
        int xpGanho = Mathf.RoundToInt(quantidade * multiplicador);

        xpAtual += xpGanho;

        while (xpAtual >= XpNecessario(level))
        {
            xpAtual -= XpNecessario(level);
            level++;
            Debug.Log("Level up! Novo level: " + level);
            AoSubirDeLevel?.Invoke(level);
        }

        AoMudarXp?.Invoke(xpAtual, XpNecessario(level), level);
    }
}
