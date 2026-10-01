using System.Collections.Generic;
using UnityEngine;

// Lista de todos os cards que podem aparecer no level up. O LevelUpManager aponta para um destes.
[CreateAssetMenu(fileName = "BancoDeCards", menuName = "Galega/Banco de cards")]
public class BancoDeCards : ScriptableObject
{
    // Arraste aqui os assets de card; o sorteador ignora entradas vazias.
    public List<CardData> cards = new List<CardData>();
}
