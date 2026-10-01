using System.Collections.Generic;
using UnityEngine;

// Guarda os valores base do player (vida, velocidade, dano, etc.) e aplica por cima os
// modificadores vindos de upgrades. Outros scripts leem os stats por aqui (Obter/propriedades)
// em vez de guardar seus próprios valores.
public class PlayerStats : MonoBehaviour
{
    // Valores iniciais de cada stat, editáveis no Inspector (os upgrades entram por cima deles).
    [Header("Stats base")]
    [SerializeField] private float vidaMaxBase = 100f;
    [SerializeField] private float velocidadeBase = 5f;
    [SerializeField] private float danoBase = 1f;
    [SerializeField] private float tirosPorSegundoBase = 4f;
    [SerializeField] private float projeteisBase = 1f;
    [SerializeField] private float raioColetaBase = 1.5f;
    [SerializeField] private float ganhoXpBase = 1f;
    // Escudo: número MÁXIMO de cargas que o escudo recarrega (ver RecarregarEscudo).
    [SerializeField] private float escudoBase = 1f;

    // Dispara quando um stat muda de valor: (stat, valor antigo, valor novo).
    public event System.Action<StatTipo, float, float> AoMudarStat;

    // Dispara quando o número de cargas do escudo muda: (cargas atuais).
    public event System.Action<int> AoMudarCargasEscudo;

    // Escudo por cargas: cada carga bloqueia 1 golpe inteiro. Começa com 0 cargas:
    // as cargas só vêm de RecarregarEscudo (power-up de escudo), até o máximo do stat Escudo.
    public int CargasEscudo { get; private set; }

    // Cada entrada guarda o modificador e quem o aplicou (a "fonte"), para poder remover depois.
    private readonly List<(ModificadorDeStat modificador, Object fonte)> modificadores = new List<(ModificadorDeStat, Object)>();

    // Cache do valor final de cada stat, recalculado só quando os modificadores mudam.
    private readonly Dictionary<StatTipo, float> cache = new Dictionary<StatTipo, float>();

    // Mudou um valor base no Inspector (ex.: durante o Play): limpa o cache para recalcular.
    private void OnValidate()
    {
        cache.Clear();
    }

    // Valor final do stat pedido (base + modificadores), usando/atualizando o cache.
    public float Obter(StatTipo stat)
    {
        if (cache.TryGetValue(stat, out float valor))
        {
            return valor;
        }

        return Recalcular(stat);
    }

    // Atalhos para ler cada stat já com os modificadores; Dano, Projeteis e VidaMax são arredondados para inteiro (mínimo 1).
    public float Velocidade => Obter(StatTipo.Velocidade);

    public int Dano => Mathf.Max(1, Mathf.RoundToInt(Obter(StatTipo.Dano)));

    // 1 / TirosPorSegundo, protegido contra divisão por zero.
    public float IntervaloDeTiro
    {
        get
        {
            float tirosPorSegundo = Obter(StatTipo.TirosPorSegundo);
            if (tirosPorSegundo <= 0f)
            {
                tirosPorSegundo = 0.01f;
            }

            return 1f / tirosPorSegundo;
        }
    }

    public int Projeteis => Mathf.Max(1, Mathf.RoundToInt(Obter(StatTipo.Projeteis)));

    public float RaioColeta => Obter(StatTipo.RaioColeta);

    public float GanhoXp => Obter(StatTipo.GanhoXp);

    public int VidaMax => Mathf.Max(1, Mathf.RoundToInt(Obter(StatTipo.VidaMax)));

    // True enquanto sobrar pelo menos 1 carga de escudo.
    public bool TemEscudo => CargasEscudo > 0;

    // Máximo de cargas que o escudo recarrega: é o valor final do stat Escudo (inteiro, mínimo 0).
    public int MaxCargasEscudo => Mathf.Max(0, Mathf.RoundToInt(Obter(StatTipo.Escudo)));

    // Adiciona modificadores vindos de "fonte" (o upgrade/objeto responsável) e avisa
    // quem estiver ouvindo AoMudarStat sobre os stats que realmente mudaram.
    public void AdicionarModificadores(ModificadorDeStat[] mods, Object fonte)
    {
        if (mods == null || mods.Length == 0)
        {
            return;
        }

        HashSet<StatTipo> statsAfetados = new HashSet<StatTipo>();
        foreach (ModificadorDeStat mod in mods)
        {
            statsAfetados.Add(mod.stat);
        }

        Dictionary<StatTipo, float> valoresAntigos = CapturarValores(statsAfetados);

        foreach (ModificadorDeStat mod in mods)
        {
            modificadores.Add((mod, fonte));
        }

        NotificarMudancas(statsAfetados, valoresAntigos);
    }

    // Remove todos os modificadores aplicados por "fonte" (ex.: quando um upgrade temporário acaba).
    public void RemoverModificadoresDaFonte(Object fonte)
    {
        HashSet<StatTipo> statsAfetados = new HashSet<StatTipo>();
        foreach (var entrada in modificadores)
        {
            if (entrada.fonte == fonte)
            {
                statsAfetados.Add(entrada.modificador.stat);
            }
        }

        if (statsAfetados.Count == 0)
        {
            return;
        }

        Dictionary<StatTipo, float> valoresAntigos = CapturarValores(statsAfetados);

        modificadores.RemoveAll(entrada => entrada.fonte == fonte);

        NotificarMudancas(statsAfetados, valoresAntigos);
    }

    // Enche o escudo até o máximo (MaxCargasEscudo). Usado pelo power-up de escudo.
    // Subir o stat Escudo (card) NÃO dá carga na hora: só aumenta o máximo da próxima recarga.
    public void RecarregarEscudo()
    {
        CargasEscudo = MaxCargasEscudo;
        AoMudarCargasEscudo?.Invoke(CargasEscudo);
    }

    // Soma cargas ao escudo. Valores <= 0 são ignorados. Nada novo usa isto: o caminho
    // normal é RecarregarEscudo; fica disponível para usos avulsos.
    public void AdicionarCargasEscudo(int n)
    {
        if (n <= 0)
        {
            return;
        }

        CargasEscudo += n;
        AoMudarCargasEscudo?.Invoke(CargasEscudo);
    }

    // Aplica o escudo: se tiver carga, gasta 1 e zera o dano recebido. Quem chamar deve checar
    // o retorno (0 = não tomou dano) antes de aplicar em vida/morte.
    public int FiltrarDanoRecebido(int dano)
    {
        if (dano <= 0 || CargasEscudo <= 0)
        {
            return dano;
        }

        CargasEscudo--;
        AoMudarCargasEscudo?.Invoke(CargasEscudo);
        return 0;
    }

    // Devolve o valor base (do Inspector) do stat pedido, antes dos modificadores.
    private float ValorBase(StatTipo stat)
    {
        switch (stat)
        {
            case StatTipo.VidaMax: return vidaMaxBase;
            case StatTipo.Velocidade: return velocidadeBase;
            case StatTipo.Dano: return danoBase;
            case StatTipo.TirosPorSegundo: return tirosPorSegundoBase;
            case StatTipo.Projeteis: return projeteisBase;
            case StatTipo.RaioColeta: return raioColetaBase;
            case StatTipo.GanhoXp: return ganhoXpBase;
            case StatTipo.Escudo: return escudoBase;
            default: return 0f;
        }
    }

    // Recalcula um stat a partir do valor base + modificadores atuais e guarda no cache.
    private float Recalcular(StatTipo stat)
    {
        List<ModificadorDeStat> todosMods = new List<ModificadorDeStat>(modificadores.Count);
        foreach (var entrada in modificadores)
        {
            todosMods.Add(entrada.modificador);
        }

        float valor = CalculoDeStat.Calcular(ValorBase(stat), todosMods, stat);
        cache[stat] = valor;
        return valor;
    }

    // Guarda o valor atual dos stats pedidos, para comparar antes e depois de mexer nos modificadores.
    private Dictionary<StatTipo, float> CapturarValores(IEnumerable<StatTipo> stats)
    {
        Dictionary<StatTipo, float> valores = new Dictionary<StatTipo, float>();
        foreach (StatTipo stat in stats)
        {
            valores[stat] = Obter(stat);
        }

        return valores;
    }

    // Recalcula cada stat afetado e dispara AoMudarStat só para os que realmente mudaram de valor.
    private void NotificarMudancas(IEnumerable<StatTipo> stats, Dictionary<StatTipo, float> valoresAntigos)
    {
        foreach (StatTipo stat in stats)
        {
            float antigo = valoresAntigos[stat];
            float novo = Recalcular(stat);

            if (!Mathf.Approximately(antigo, novo))
            {
                AoMudarStat?.Invoke(stat, antigo, novo);
            }
        }
    }

    // Atalhos de teste (menu do componente no Inspector): +1 projétil, +50% de velocidade, encher o escudo e limpar o que os testes aplicaram.
    [ContextMenu("Teste +1 Projetil")]
    private void TesteMaisUmProjetil()
    {
        AdicionarModificadores(new[]
        {
            new ModificadorDeStat { stat = StatTipo.Projeteis, tipo = TipoModificador.Somar, valor = 1f }
        }, this);
    }

    [ContextMenu("Teste +50% Velocidade")]
    private void TesteMaisVelocidade()
    {
        AdicionarModificadores(new[]
        {
            new ModificadorDeStat { stat = StatTipo.Velocidade, tipo = TipoModificador.Percentual, valor = 0.5f }
        }, this);
    }

    [ContextMenu("Teste Escudo")]
    private void TesteEscudo()
    {
        // Enche o escudo até o máximo, igual ao power-up de escudo.
        RecarregarEscudo();
    }

    [ContextMenu("Teste Limpar Testes")]
    private void TesteLimparTestes()
    {
        RemoverModificadoresDaFonte(this);
    }
}
