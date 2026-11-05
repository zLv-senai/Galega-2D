using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

// Animação de abertura das janelas (menu, pausa, level up, game over...). Funciona com Time.timeScale = 0: transições do
// UI Toolkit e schedule rodam em tempo real. As classes (anim-*) estão no Tema.uss.
// Uso: depois de deixar a janela visível, Tocar(elemento, "anim-ini-...") leva o elemento do estado inicial ao normal.
public static class AnimacaoDeJanela
{
    private const string ClasseTransicao = "anim-transicao";
    private const long EsperaParaEstilizarMs = 34;   // o estado inicial precisa ser pintado antes de a classe sair
    private const long DuracaoMaximaMs = 600;        // depois disso a transição acabou: libera hover etc.

    // Uma execução em andamento por elemento: tocar de novo cancela a anterior, para os timers dela não cortarem a nova.
    private static readonly ConditionalWeakTable<VisualElement, Execucao> EmAndamento = new ConditionalWeakTable<VisualElement, Execucao>();

    private sealed class Execucao
    {
        private readonly VisualElement alvo;
        private readonly string classeInicial;
        private readonly string classeExtra;
        private readonly IVisualElementScheduledItem tirarInicial;
        private readonly IVisualElementScheduledItem tirarTransicao;

        public Execucao(VisualElement alvo, string classeInicial, long atrasoMs, string classeExtra)
        {
            this.alvo = alvo;
            this.classeInicial = classeInicial;
            this.classeExtra = classeExtra;

            alvo.AddToClassList(ClasseTransicao);
            alvo.AddToClassList(classeInicial);
            if (classeExtra != null)
            {
                alvo.AddToClassList(classeExtra);
            }

            tirarInicial = alvo.schedule.Execute(() => alvo.RemoveFromClassList(classeInicial));
            tirarInicial.ExecuteLater(atrasoMs + EsperaParaEstilizarMs);
            tirarTransicao = alvo.schedule.Execute(Limpar);
            tirarTransicao.ExecuteLater(atrasoMs + EsperaParaEstilizarMs + DuracaoMaximaMs);
        }

        // Cancela os timers e tira as classes (fim normal ou substituída por outra chamada).
        public void Limpar()
        {
            tirarInicial.Pause();
            tirarTransicao.Pause();
            alvo.RemoveFromClassList(classeInicial);
            alvo.RemoveFromClassList(ClasseTransicao);
            if (classeExtra != null)
            {
                alvo.RemoveFromClassList(classeExtra);
            }
        }
    }

    // classeInicial: estado de partida (anim-ini-*), tirada após atrasoMs para a transição do USS levar ao estado normal.
    // classeExtra (opcional): ajuste de duração ou curva (anim-lenta, anim-back...), mantida durante a transição.
    public static void Tocar(VisualElement alvo, string classeInicial, long atrasoMs = 0, string classeExtra = null)
    {
        if (alvo == null)
        {
            return;
        }

        if (EmAndamento.TryGetValue(alvo, out Execucao anterior))
        {
            anterior.Limpar();
            EmAndamento.Remove(alvo);
        }

        EmAndamento.Add(alvo, new Execucao(alvo, classeInicial, atrasoMs, classeExtra));
    }
}
