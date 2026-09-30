using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu do Editor que cria os cards e power-ups padrão (assets) e as listas que os agrupam.
// Nunca sobrescreve: se um asset já existir (o grupo pode ter editado), ele é mantido como está.
public static class CriarDadosPadrao
{
    private const string PastaDados = "Assets/Data";
    private const string PastaCards = "Assets/Data/Cards";
    private const string PastaPowerUps = "Assets/Data/PowerUps";
    private const string CaminhoBanco = "Assets/Data/BancoDeCards.asset";
    private const string CaminhoTabela = "Assets/Data/TabelaDePowerUps.asset";

    // Definição de um card, só para escrever a tabela abaixo de forma compacta.
    private readonly struct DefCard
    {
        public readonly Raridade raridade;
        public readonly string nome;
        public readonly string descricao;
        public readonly StatTipo stat;
        public readonly TipoModificador tipo;
        public readonly float valor;
        public readonly int maxEscolhas;

        public DefCard(Raridade raridade, string nome, string descricao, StatTipo stat,
            TipoModificador tipo, float valor, int maxEscolhas)
        {
            this.raridade = raridade;
            this.nome = nome;
            this.descricao = descricao;
            this.stat = stat;
            this.tipo = tipo;
            this.valor = valor;
            this.maxEscolhas = maxEscolhas;
        }
    }

    // Definição de um power-up (modificador único; escudo usa recarrega = true e sem modificador).
    private readonly struct DefPowerUp
    {
        public readonly string nome;
        public readonly Color cor;
        public readonly float duracao;
        public readonly int peso;
        public readonly bool recarregaEscudo;
        public readonly StatTipo stat;
        public readonly TipoModificador tipo;
        public readonly float valor;

        public DefPowerUp(string nome, Color cor, float duracao, int peso, bool recarregaEscudo,
            StatTipo stat, TipoModificador tipo, float valor)
        {
            this.nome = nome;
            this.cor = cor;
            this.duracao = duracao;
            this.peso = peso;
            this.recarregaEscudo = recarregaEscudo;
            this.stat = stat;
            this.tipo = tipo;
            this.valor = valor;
        }
    }

    private static readonly DefCard[] Cards =
    {
        new DefCard(Raridade.Comum, "Motor Afinado", "+8% de velocidade", StatTipo.Velocidade, TipoModificador.Percentual, 0.08f, 0),
        new DefCard(Raridade.Comum, "Casco Reforçado", "+10 de vida máxima", StatTipo.VidaMax, TipoModificador.Somar, 10f, 0),
        new DefCard(Raridade.Comum, "Gatilho Leve", "+8% de cadência de tiro", StatTipo.TirosPorSegundo, TipoModificador.Percentual, 0.08f, 0),
        new DefCard(Raridade.Comum, "Ímã Fraco", "+0,5 de raio de coleta", StatTipo.RaioColeta, TipoModificador.Somar, 0.5f, 0),

        new DefCard(Raridade.Incomum, "Propulsores", "+15% de velocidade", StatTipo.Velocidade, TipoModificador.Percentual, 0.15f, 0),
        new DefCard(Raridade.Incomum, "Blindagem", "+20 de vida máxima", StatTipo.VidaMax, TipoModificador.Somar, 20f, 0),
        new DefCard(Raridade.Incomum, "Estudante", "+10% de XP ganho", StatTipo.GanhoXp, TipoModificador.Percentual, 0.10f, 0),

        new DefCard(Raridade.Raro, "Cadência", "+20% de cadência de tiro", StatTipo.TirosPorSegundo, TipoModificador.Percentual, 0.20f, 0),
        new DefCard(Raridade.Raro, "Escudo Reserva", "+1 carga no máximo do escudo das caixas", StatTipo.Escudo, TipoModificador.Somar, 1f, 2),
        new DefCard(Raridade.Raro, "Ímã Forte", "+1,5 de raio de coleta", StatTipo.RaioColeta, TipoModificador.Somar, 1.5f, 0),

        new DefCard(Raridade.Epico, "Munição Pesada", "+1 de dano por tiro", StatTipo.Dano, TipoModificador.Somar, 1f, 0),
        new DefCard(Raridade.Epico, "Mestre", "+25% de XP ganho", StatTipo.GanhoXp, TipoModificador.Percentual, 0.25f, 0),

        new DefCard(Raridade.Lendario, "Coração de Titã", "+50 de vida máxima", StatTipo.VidaMax, TipoModificador.Somar, 50f, 0),
        new DefCard(Raridade.Lendario, "Sobrecarga", "+40% de cadência de tiro", StatTipo.TirosPorSegundo, TipoModificador.Percentual, 0.40f, 0),

        new DefCard(Raridade.Mitico, "Canhão Extra", "+1 projétil por disparo", StatTipo.Projeteis, TipoModificador.Somar, 1f, 2),
    };

    private static readonly DefPowerUp[] PowerUps =
    {
        new DefPowerUp("Ímã de XP", Color.green, 3f, 30, false, StatTipo.RaioColeta, TipoModificador.Somar, 30f),
        new DefPowerUp("Turbo", Color.yellow, 8f, 25, false, StatTipo.Velocidade, TipoModificador.Percentual, 0.5f),
        new DefPowerUp("Escudo", Color.cyan, 0f, 20, true, StatTipo.Escudo, TipoModificador.Somar, 0f),
        new DefPowerUp("Tiro Rápido", new Color(1f, 0.55f, 0f), 8f, 15, false, StatTipo.TirosPorSegundo, TipoModificador.Percentual, 1f),
        new DefPowerUp("Tiro Triplo", Color.magenta, 10f, 10, false, StatTipo.Projeteis, TipoModificador.Somar, 2f),
    };

    [MenuItem("Galega/Criar cards e power-ups padrão")]
    public static void Criar()
    {
        GarantirPasta(PastaDados);
        GarantirPasta(PastaCards);
        GarantirPasta(PastaPowerUps);

        int criados = 0;

        List<CardData> cardsDoBanco = new List<CardData>();
        foreach (DefCard def in Cards)
        {
            cardsDoBanco.Add(ObterOuCriarCard(def, ref criados));
        }

        List<PowerUpData> powerUpsDaTabela = new List<PowerUpData>();
        foreach (DefPowerUp def in PowerUps)
        {
            powerUpsDaTabela.Add(ObterOuCriarPowerUp(def, ref criados));
        }

        CriarBancoSeNaoExistir(cardsDoBanco, ref criados);
        CriarTabelaSeNaoExistir(powerUpsDaTabela, ref criados);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Galega: " + criados + " asset(s) criado(s). Os que já existiam foram mantidos.");
    }

    private static CardData ObterOuCriarCard(DefCard def, ref int criados)
    {
        string caminho = PastaCards + "/" + def.raridade + "_" + NomeDeArquivo(def.nome) + ".asset";

        CardData existente = AssetDatabase.LoadAssetAtPath<CardData>(caminho);
        if (existente != null)
        {
            return existente;
        }

        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.nome = def.nome;
        card.descricao = def.descricao;
        card.raridade = def.raridade;
        card.maxEscolhas = def.maxEscolhas;
        card.modificadores = new[]
        {
            new ModificadorDeStat { stat = def.stat, tipo = def.tipo, valor = def.valor }
        };

        AssetDatabase.CreateAsset(card, caminho);
        criados++;
        return card;
    }

    private static PowerUpData ObterOuCriarPowerUp(DefPowerUp def, ref int criados)
    {
        string caminho = PastaPowerUps + "/" + NomeDeArquivo(def.nome) + ".asset";

        PowerUpData existente = AssetDatabase.LoadAssetAtPath<PowerUpData>(caminho);
        if (existente != null)
        {
            return existente;
        }

        PowerUpData powerUp = ScriptableObject.CreateInstance<PowerUpData>();
        powerUp.nome = def.nome;
        powerUp.cor = def.cor;
        powerUp.duracao = def.duracao;
        powerUp.peso = def.peso;
        powerUp.recarregaEscudo = def.recarregaEscudo;

        // O power-up de escudo só recarrega o escudo: não tem modificadores.
        powerUp.modificadores = def.recarregaEscudo
            ? new ModificadorDeStat[0]
            : new[] { new ModificadorDeStat { stat = def.stat, tipo = def.tipo, valor = def.valor } };

        AssetDatabase.CreateAsset(powerUp, caminho);
        criados++;
        return powerUp;
    }

    private static void CriarBancoSeNaoExistir(List<CardData> cards, ref int criados)
    {
        if (AssetDatabase.LoadAssetAtPath<BancoDeCards>(CaminhoBanco) != null)
        {
            return;
        }

        BancoDeCards banco = ScriptableObject.CreateInstance<BancoDeCards>();
        banco.cards = cards;
        AssetDatabase.CreateAsset(banco, CaminhoBanco);
        criados++;
    }

    private static void CriarTabelaSeNaoExistir(List<PowerUpData> powerUps, ref int criados)
    {
        if (AssetDatabase.LoadAssetAtPath<TabelaDePowerUps>(CaminhoTabela) != null)
        {
            return;
        }

        TabelaDePowerUps tabela = ScriptableObject.CreateInstance<TabelaDePowerUps>();
        tabela.powerUps = powerUps;
        AssetDatabase.CreateAsset(tabela, CaminhoTabela);
        criados++;
    }

    // Cria a pasta se ainda não existir (o pai já precisa existir; criamos de cima para baixo).
    private static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho))
        {
            return;
        }

        int barra = caminho.LastIndexOf('/');
        AssetDatabase.CreateFolder(caminho.Substring(0, barra), caminho.Substring(barra + 1));
    }

    // "Ímã de XP" vira "ImadeXP": sem acento nem espaço, para o nome do arquivo ser seguro.
    private static string NomeDeArquivo(string nome)
    {
        string decomposto = nome.Normalize(NormalizationForm.FormD);
        StringBuilder resultado = new StringBuilder();

        foreach (char c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c != ' ')
            {
                resultado.Append(c);
            }
        }

        return resultado.ToString();
    }
}
