using UnityEngine;

// Os dois modos de jogo. Valor novo sempre no FIM do enum.
public enum ModoDeJogo
{
    Campanha,   // 15 waves, com boss nas waves 5, 10 e 15; limpar a 15 é a vitória
    Infinito    // waves sem fim; o recorde é a maior wave alcançada
}

// Escolha do menu. É estática para sobreviver ao GameManager.Restart (que recarrega a cena).
public static class ConfiguracaoDePartida
{
    // Nome usado quando o jogador não digita nada e limite de caracteres do nome.
    public const string NomePadrao = "Piloto";
    public const int MaxCaracteresNome = 10;

    // Chave do PlayerPrefs onde o último nome fica guardado.
    private const string ChaveNomeJogador = "Galega_NomeJogador";

    // Modo escolhido no menu; o GerenciadorDeWaves lê isto quando a partida começa.
    public static ModoDeJogo Modo = ModoDeJogo.Campanha;

    // Nome do jogador (até 10 caracteres). Entra no ranking do modo Infinito (Leaderboard).
    public static string NomeJogador = NomePadrao;

    // Último nome usado, guardado entre execuções do jogo ("" se nunca houve um).
    public static string NomeSalvo => PlayerPrefs.GetString(ChaveNomeJogador, "");

    // Tira os espaços das pontas, limita a 10 caracteres e usa "Piloto" se sobrar nada.
    public static string NormalizarNome(string nome)
    {
        string limpo = string.IsNullOrEmpty(nome) ? "" : nome.Trim();
        if (limpo.Length > MaxCaracteresNome)
        {
            limpo = limpo.Substring(0, MaxCaracteresNome).TrimEnd();
        }

        return limpo.Length == 0 ? NomePadrao : limpo;
    }

    // Define o nome da partida e guarda em PlayerPrefs para preencher o campo na próxima vez.
    public static void DefinirNome(string nome)
    {
        NomeJogador = NormalizarNome(nome);
        PlayerPrefs.SetString(ChaveNomeJogador, NomeJogador);
        PlayerPrefs.Save();
    }
}
