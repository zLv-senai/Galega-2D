// Raridade de um card, da mais comum para a mais rara.
// A ordem do enum importa: o sorteador "desce" e "sobe" de raridade somando/subtraindo 1.
public enum Raridade
{
    Comum,
    Incomum,
    Raro,
    Epico,
    Lendario,
    Mitico
}

// Peso de sorteio e nome de exibição de cada raridade.
public static class InfoDeRaridade
{
    // Comum 50, Incomum 25, Raro 12, Épico 8, Lendário 4, Mítico 1 (total 100).
    private static readonly int[] pesos = { 50, 25, 12, 8, 4, 1 };

    // Nome de exibição de cada raridade, na mesma ordem do enum.
    private static readonly string[] nomes = { "Comum", "Incomum", "Raro", "Épico", "Lendário", "Mítico" };

    // Quantas raridades existem; o sorteador percorre de 0 até aqui.
    public static int Quantidade => pesos.Length;

    // Peso de sorteio da raridade (quanto maior, mais fácil de sair).
    public static int Peso(Raridade raridade)
    {
        return pesos[(int)raridade];
    }

    // Soma dos pesos; o sorteio escolhe um número de 0 até este total.
    public static int PesoTotal()
    {
        int total = 0;
        foreach (int peso in pesos)
        {
            total += peso;
        }

        return total;
    }

    // Nome com acento, para mostrar na tela.
    public static string Nome(Raridade raridade)
    {
        return nomes[(int)raridade];
    }
}
