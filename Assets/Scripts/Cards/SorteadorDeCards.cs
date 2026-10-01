using System.Collections.Generic;
using UnityEngine;

// Sorteia os cards que aparecem no level up. Estático e sem estado: quem chama
// passa o banco e quantas vezes cada card já foi escolhido.
public static class SorteadorDeCards
{
    // Quantos cards aparecem por escolha; a tela cria um botão para cada vaga.
    public const int CardsPorOferta = 3;

    // Sorteia até "CardsPorOferta" cards DISTINTOS. Se houver menos cards elegíveis
    // que isso no total, devolve só os que houver (pode devolver vazio).
    public static CardData[] Sortear(BancoDeCards banco, IReadOnlyDictionary<CardData, int> escolhas)
    {
        List<CardData> oferta = new List<CardData>(CardsPorOferta);

        if (banco == null || banco.cards == null)
        {
            return oferta.ToArray();
        }

        for (int vaga = 0; vaga < CardsPorOferta; vaga++)
        {
            CardData card = SortearUm(banco, escolhas, oferta);
            if (card == null)
            {
                // Sem elegível para esta vaga = não sobra nenhum para as próximas.
                break;
            }

            oferta.Add(card);
        }

        return oferta.ToArray();
    }

    // Uma vaga: sorteia a raridade pelo peso e tenta achar um card dela; se não houver,
    // desce uma raridade por vez até a Comum; se ainda assim não achar, sobe a partir da sorteada.
    private static CardData SortearUm(BancoDeCards banco, IReadOnlyDictionary<CardData, int> escolhas, List<CardData> oferta)
    {
        int sorteada = (int)SortearRaridade();

        for (int r = sorteada; r >= 0; r--)
        {
            CardData card = EscolherDaRaridade(banco, (Raridade)r, escolhas, oferta);
            if (card != null)
            {
                return card;
            }
        }

        for (int r = sorteada + 1; r < InfoDeRaridade.Quantidade; r++)
        {
            CardData card = EscolherDaRaridade(banco, (Raridade)r, escolhas, oferta);
            if (card != null)
            {
                return card;
            }
        }

        return null;
    }

    // Sorteia uma raridade usando os pesos: as comuns saem muito mais que as míticas.
    private static Raridade SortearRaridade()
    {
        int sorteio = Random.Range(0, InfoDeRaridade.PesoTotal());
        int acumulado = 0;

        for (int r = 0; r < InfoDeRaridade.Quantidade; r++)
        {
            acumulado += InfoDeRaridade.Peso((Raridade)r);
            if (sorteio < acumulado)
            {
                return (Raridade)r;
            }
        }

        return Raridade.Comum;
    }

    // Card aleatório da raridade pedida que ainda não está na oferta e não atingiu maxEscolhas.
    private static CardData EscolherDaRaridade(BancoDeCards banco, Raridade raridade,
        IReadOnlyDictionary<CardData, int> escolhas, List<CardData> oferta)
    {
        List<CardData> elegiveis = new List<CardData>();

        foreach (CardData card in banco.cards)
        {
            if (card == null || card.raridade != raridade || oferta.Contains(card))
            {
                continue;
            }

            int vezes = 0;
            if (escolhas != null)
            {
                escolhas.TryGetValue(card, out vezes);
            }

            if (card.maxEscolhas > 0 && vezes >= card.maxEscolhas)
            {
                continue;
            }

            elegiveis.Add(card);
        }

        if (elegiveis.Count == 0)
        {
            return null;
        }

        return elegiveis[Random.Range(0, elegiveis.Count)];
    }
}
