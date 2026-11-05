using UnityEngine.UIElements;

// Som de navegação nas telas em UI Toolkit: passar o mouse sobre um botão ou card toca o som de "mudar de opção".
// O som de confirmar é tocado pelo clique de cada tela (GerenciadorDeSom.TocarClique).
public static class SomDeInterface
{
    private const string ClasseLigado = "som-ligado";
    private const string ClasseDaRoda = "roda-item";   // os itens da roda tocam o som pela própria seleção
    private const string ClasseCardModo = "card-modo";

    // Chamar a cada RegisterUIReloadCallback: a árvore é nova, então os botões ainda não têm o evento.
    public static void Ligar(VisualElement root)
    {
        if (root == null)
        {
            return;
        }

        root.Query<VisualElement>().Build().ForEach(elemento =>
        {
            bool temSom = elemento is Button || elemento.ClassListContains(ClasseCardModo);
            if (!temSom || elemento.ClassListContains(ClasseDaRoda) || elemento.ClassListContains(ClasseLigado))
            {
                return;
            }

            elemento.AddToClassList(ClasseLigado);
            elemento.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (elemento.enabledInHierarchy)
                {
                    GerenciadorDeSom.TocarNavegacao();
                }
            });
        });
    }
}
