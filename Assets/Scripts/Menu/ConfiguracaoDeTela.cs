using UnityEngine;

// Modo de tela escolhido em Configurações (tela cheia ou janela), guardado em PlayerPrefs (padrão: tela cheia).
public static class ConfiguracaoDeTela
{
    private const string ChaveTelaCheia = "TelaCheia";
    private const int JanelaLargura = 1280;
    private const int JanelaAltura = 720;

    public static bool TelaCheia => PlayerPrefs.GetInt(ChaveTelaCheia, 1) == 1;

    // Ao abrir o jogo: o Unity guarda o último modo de tela no registro do PC, então garantimos aqui a tela
    // cheia (ou a escolha salva). No Editor não mexe, para não bagunçar a Game View.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Inicializar()
    {
        if (!Application.isEditor)
        {
            Aplicar();
        }
    }

    // Salva a escolha e aplica na hora.
    public static void DefinirTelaCheia(bool telaCheia)
    {
        PlayerPrefs.SetInt(ChaveTelaCheia, telaCheia ? 1 : 0);
        PlayerPrefs.Save();

        if (!Application.isEditor)
        {
            Aplicar();
        }
    }

    private static void Aplicar()
    {
        if (TelaCheia)
        {
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(JanelaLargura, JanelaAltura, FullScreenMode.Windowed);
        }
    }
}
