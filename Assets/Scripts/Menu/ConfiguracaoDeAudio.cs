using System;
using UnityEngine;

// Volumes escolhidos em Settings (0 a 1, padrão 1), guardados em PlayerPrefs.
//   Geral   = AudioListener.volume (vale para tudo, inclusive o Som.cs do Eduardo).
//   Musica  = multiplicador da música de fundo (GerenciadorDeSom).
//   Efeitos = multiplicador dos efeitos (GerenciadorDeSom e Som.cs).
// AoMudar é ESTÁTICO: quem assinar precisa cancelar no OnDisable.
public static class ConfiguracaoDeAudio
{
    // Chaves do PlayerPrefs de cada volume e o valor padrão (1 = volume máximo).
    private const string ChaveGeral = "Galega_VolGeral";
    private const string ChaveMusica = "Galega_VolMusica";
    private const string ChaveEfeitos = "Galega_VolEfeitos";
    private const float VolumePadrao = 1f;

    // Disparado sempre que um dos três volumes muda.
    public static event Action AoMudar;

    // Cópia em memória dos volumes, lida do PlayerPrefs só na primeira vez.
    private static bool carregado;
    private static float geral = VolumePadrao;
    private static float musica = VolumePadrao;
    private static float efeitos = VolumePadrao;

    // Volumes de 0 a 1 (Geral, Musica, Efeitos): ler carrega os salvos; escrever limita a 0-1, guarda
    // no PlayerPrefs e avisa o AoMudar.
    public static float Geral
    {
        get { Carregar(); return geral; }
        set { Atualizar(ref geral, ChaveGeral, value); }
    }

    public static float Musica
    {
        get { Carregar(); return musica; }
        set { Atualizar(ref musica, ChaveMusica, value); }
    }

    public static float Efeitos
    {
        get { Carregar(); return efeitos; }
        set { Atualizar(ref efeitos, ChaveEfeitos, value); }
    }

    // Ao abrir o jogo: carrega os volumes salvos e já aplica o Geral.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Inicializar()
    {
        carregado = false;   // recarrega a cada execução (vale também com Domain Reload desligado)
        Aplicar();
    }

    // Aplica o volume Geral no AudioListener (global, sobrevive à troca de cena).
    public static void Aplicar()
    {
        Carregar();
        AudioListener.volume = geral;
    }

    // Grava em disco. Os setters só escrevem na memória (o slider muda várias vezes por segundo).
    public static void Salvar()
    {
        PlayerPrefs.Save();
    }

    // Lê os três volumes do PlayerPrefs na primeira chamada, limitando cada um a 0-1.
    private static void Carregar()
    {
        if (carregado)
        {
            return;
        }

        carregado = true;
        geral = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveGeral, VolumePadrao));
        musica = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveMusica, VolumePadrao));
        efeitos = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveEfeitos, VolumePadrao));
    }

    // Troca um volume (se mudou de verdade): guarda no PlayerPrefs, reaplica o Geral e dispara o AoMudar.
    private static void Atualizar(ref float campo, string chave, float valor)
    {
        Carregar();

        float novo = Mathf.Clamp01(valor);
        if (Mathf.Approximately(campo, novo))
        {
            return;
        }

        campo = novo;
        PlayerPrefs.SetFloat(chave, novo);
        AudioListener.volume = geral;
        AoMudar?.Invoke();
    }
}
