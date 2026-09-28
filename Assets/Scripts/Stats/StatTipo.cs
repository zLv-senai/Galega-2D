using System.Collections.Generic;

// Tipos de stat que o player pode ter. Usado pelos modificadores (upgrades)
// e pelo PlayerStats para calcular o valor final de cada um.
public enum StatTipo
{
    VidaMax,
    Velocidade,
    Dano,
    TirosPorSegundo,
    Projeteis,
    RaioColeta,
    GanhoXp,
    Escudo
}

// Como o modificador altera o stat: soma um valor fixo ou aplica um percentual.
public enum TipoModificador
{
    Somar,
    Percentual
}

// Um modificador de stat: qual stat, que tipo de alteração e o valor aplicado.
[System.Serializable]
public struct ModificadorDeStat
{
    public StatTipo stat;
    public TipoModificador tipo;
    public float valor;
}

// Calcula o valor final de um stat a partir do valor base e da lista de modificadores.
public static class CalculoDeStat
{
    // Fórmula: (base + soma dos "Somar") * (1 + soma dos "Percentual"), considerando
    // só os modificadores do stat pedido.
    public static float Calcular(float baseValor, IEnumerable<ModificadorDeStat> mods, StatTipo stat)
    {
        float soma = 0f;
        float percentual = 0f;

        foreach (ModificadorDeStat mod in mods)
        {
            if (mod.stat != stat)
            {
                continue;
            }

            if (mod.tipo == TipoModificador.Somar)
            {
                soma += mod.valor;
            }
            else
            {
                percentual += mod.valor;
            }
        }

        return (baseValor + soma) * (1f + percentual);
    }
}
