using UnityEngine;

// Gema de XP: ao ser coletada pelo player, dá "valor" de XP.
public class GemaDeXp : Coletavel
{
    public int valor = 1;

    protected override void AoColetar(Coletor c)
    {
        c.Xp.GanharXp(valor);
    }
}
