using UnityEngine;

// Gema de XP: ao ser coletada pelo player, dá "valor" de XP.
public class GemaDeXp : Coletavel
{
    // Quanto de XP a gema dá (o GeradorDeGemas troca este valor ao criar a gema).
    public int valor = 1;

    // Dá o XP da gema ao player (o PlayerXp aplica o bônus de GanhoXp).
    protected override void AoColetar(Coletor c)
    {
        c.Xp.GanharXp(valor);
    }
}
