using System.Collections.Generic;
using UnityEngine;

// Lista dos power-ups que um baú pode soltar, com sorteio por peso.
[CreateAssetMenu(fileName = "TabelaDePowerUps", menuName = "Galega/Tabela de power-ups")]
public class TabelaDePowerUps : ScriptableObject
{
    // Power-ups que podem sair; a chance de cada um depende do peso no próprio PowerUpData.
    public List<PowerUpData> powerUps = new List<PowerUpData>();

    // Sorteia um power-up pelo peso. Ignora nulos, peso <= 0 e os "somenteNoInfinito" fora do Infinito;
    // devolve null se não sobrar nenhum.
    public PowerUpData Sortear()
    {
        int total = 0;
        foreach (PowerUpData powerUp in powerUps)
        {
            if (PodeSair(powerUp))
            {
                total += powerUp.peso;
            }
        }

        if (total <= 0)
        {
            return null;
        }

        int sorteio = Random.Range(0, total);
        int acumulado = 0;

        foreach (PowerUpData powerUp in powerUps)
        {
            if (!PodeSair(powerUp))
            {
                continue;
            }

            acumulado += powerUp.peso;
            if (sorteio < acumulado)
            {
                return powerUp;
            }
        }

        return null;
    }

    // Entra no sorteio: existe, tem peso e, se for "somenteNoInfinito", a partida está no modo Infinito.
    private static bool PodeSair(PowerUpData powerUp)
    {
        if (powerUp == null || powerUp.peso <= 0)
        {
            return false;
        }

        return !powerUp.somenteNoInfinito || ConfiguracaoDePartida.Modo == ModoDeJogo.Infinito;
    }
}
