using UnityEngine;

// No Player: quando o VidaMax muda (upgrade, etc.), soma a diferença na vida atual
// do PlayerMove, em vez de curar/matar o player sem querer.
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerMove))]
public class SincronizarVida : MonoBehaviour
{
    // Componentes do mesmo Player: os stats (de onde vem o VidaMax) e o PlayerMove (onde fica a vida atual).
    private PlayerStats stats;
    private PlayerMove playerMove;

    // Pega o PlayerStats e o PlayerMove do próprio Player.
    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        playerMove = GetComponent<PlayerMove>();
    }

    // Começa a ouvir as mudanças de stat do PlayerStats.
    private void OnEnable()
    {
        if (stats != null)
        {
            stats.AoMudarStat += TratarMudancaDeStat;
        }
    }

    // Para de ouvir as mudanças de stat (evita chamada em objeto desligado).
    private void OnDisable()
    {
        if (stats != null)
        {
            stats.AoMudarStat -= TratarMudancaDeStat;
        }
    }

    // Quando o VidaMax muda, soma a diferença (novo - antigo) na vida atual do PlayerMove; outros stats são ignorados.
    private void TratarMudancaDeStat(StatTipo stat, float antigo, float novo)
    {
        if (stat != StatTipo.VidaMax || playerMove == null)
        {
            return;
        }

        // Mantém a vida entre 1 e o novo máximo: mudar stat nunca mata o player.
        int novaVida = playerMove.vida + Mathf.RoundToInt(novo - antigo);
        playerMove.vida = Mathf.Clamp(novaVida, 1, stats.VidaMax);
    }
}
