using UnityEngine;

// No Player: quando o VidaMax muda (upgrade, etc.), soma a diferença na vida atual
// do PlayerMove, em vez de curar/matar o player sem querer.
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerMove))]
public class SincronizarVida : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerMove playerMove;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        playerMove = GetComponent<PlayerMove>();
    }

    private void OnEnable()
    {
        if (stats != null)
        {
            stats.AoMudarStat += TratarMudancaDeStat;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.AoMudarStat -= TratarMudancaDeStat;
        }
    }

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
