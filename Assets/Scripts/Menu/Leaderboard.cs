using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Uma linha do ranking. Só campos públicos: é o que o JsonUtility salva.
[Serializable]
public class EntradaDeRanking
{
    // Nome do jogador e a wave que ele alcançou na partida.
    public string nome;
    public int wave;
    public long dataTicks;   // DateTime.Now.Ticks de quando a partida terminou

    // "dd/MM/aaaa" (ex.: 01/10/2026).
    public string DataFormatada()
    {
        long ticks = Math.Max(DateTime.MinValue.Ticks, Math.Min(DateTime.MaxValue.Ticks, dataTicks));
        return new DateTime(ticks).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}

// Top 5 LOCAL do modo Infinito (maior wave alcançada). Fica em PlayerPrefs como JSON.
// Ordem: wave maior primeiro; em empate, a partida mais antiga primeiro.
public static class Leaderboard
{
    // Tamanho do ranking e a chave do PlayerPrefs onde o JSON fica salvo.
    public const int MaxEntradas = 5;

    private const string ChaveRanking = "Galega_Leaderboard";

    // O JsonUtility não salva lista solta: precisa de uma classe que a contenha.
    [Serializable]
    private sealed class DadosSalvos
    {
        public List<EntradaDeRanking> entradas = new List<EntradaDeRanking>();
    }

    // Disparado a cada Registrar, com a posição (1 a 5) ou 0 se ficou fora do top.
    public static event Action<int> AoRegistrar;

    // Posição da última partida registrada (0 = fora do top ou nenhuma ainda). Usado pelo Game Over.
    public static int UltimaPosicao { get; private set; }

    // Cópia do ranking atual (já ordenado). Mexer na lista não altera o que está salvo.
    public static IReadOnlyList<EntradaDeRanking> Entradas => Carregar().AsReadOnly();

    // Guarda a partida e devolve a posição no ranking (1 a 5), ou 0 se não entrou.
    public static int Registrar(string nome, int wave)
    {
        if (wave < 1)
        {
            UltimaPosicao = 0;
            AoRegistrar?.Invoke(0);
            return 0;
        }

        List<EntradaDeRanking> lista = Carregar();
        EntradaDeRanking nova = new EntradaDeRanking
        {
            nome = ConfiguracaoDePartida.NormalizarNome(nome),
            wave = wave,
            dataTicks = DateTime.Now.Ticks
        };

        lista.Add(nova);
        Ordenar(lista);

        if (lista.Count > MaxEntradas)
        {
            lista.RemoveRange(MaxEntradas, lista.Count - MaxEntradas);
        }

        // Posição 1-based; IndexOf devolve -1 se a nova foi cortada (fica 0 = fora do top).
        int posicao = lista.IndexOf(nova) + 1;

        Salvar(lista);

        UltimaPosicao = posicao;
        AoRegistrar?.Invoke(posicao);
        return posicao;
    }

    // Deixa a lista em ordem de ranking: wave maior primeiro; empate pela partida mais antiga.
    private static void Ordenar(List<EntradaDeRanking> lista)
    {
        lista.Sort((a, b) =>
        {
            int porWave = b.wave.CompareTo(a.wave);
            return porWave != 0 ? porWave : a.dataTicks.CompareTo(b.dataTicks);
        });
    }

    // Lê o JSON do PlayerPrefs e devolve as entradas válidas, ordenadas e cortadas no top 5
    // (vazio se não há nada salvo ou se o dado está corrompido).
    private static List<EntradaDeRanking> Carregar()
    {
        string json = PlayerPrefs.GetString(ChaveRanking, "");
        List<EntradaDeRanking> validas = new List<EntradaDeRanking>();
        if (string.IsNullOrEmpty(json))
        {
            return validas;
        }

        try
        {
            DadosSalvos dados = JsonUtility.FromJson<DadosSalvos>(json);
            if (dados == null || dados.entradas == null)
            {
                return validas;
            }

            foreach (EntradaDeRanking entrada in dados.entradas)
            {
                if (entrada != null && entrada.wave >= 1)
                {
                    validas.Add(entrada);
                }
            }
        }
        catch (ArgumentException ex)
        {
            // Dado salvo corrompido: recomeça vazio em vez de quebrar o menu.
            Debug.LogWarning("Leaderboard: dados salvos inválidos, o ranking recomeça vazio. " + ex.Message);
            return new List<EntradaDeRanking>();
        }

        Ordenar(validas);
        if (validas.Count > MaxEntradas)
        {
            validas.RemoveRange(MaxEntradas, validas.Count - MaxEntradas);
        }

        return validas;
    }

    // Grava a lista como JSON no PlayerPrefs e já escreve em disco.
    private static void Salvar(List<EntradaDeRanking> lista)
    {
        DadosSalvos dados = new DadosSalvos { entradas = lista };
        PlayerPrefs.SetString(ChaveRanking, JsonUtility.ToJson(dados));
        PlayerPrefs.Save();
    }
}
