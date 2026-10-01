using UnityEngine;

// Base para qualquer coisa que voa até o player quando entra no raio de coleta
// (gemas de XP, itens, etc.). Cada tipo concreto só precisa dizer o que acontece
// ao ser coletado (AoColetar).
public abstract class Coletavel : MonoBehaviour
{
    // Velocidade com que o item começa a voar até o player e quanto ela aumenta por segundo.
    [SerializeField] private float velocidadeInicial = 6f;
    [SerializeField] private float aceleracao = 4f;

    // Distância do alvo a partir da qual o item conta como coletado.
    private const float DistanciaParaColetar = 0.3f;

    // Quem está sendo perseguido (null = parado) e a velocidade atual do voo.
    private Coletor alvo;
    private float velocidadeAtual;

    // Chamado pelo Coletor quando o player entra no raio de coleta: define o alvo a perseguir.
    public void Atrair(Coletor c)
    {
        // O Coletor chama isso todo frame; sem essa guarda a velocidade
        // voltaria ao inicial e a gema nunca aceleraria.
        if (alvo == c)
        {
            return;
        }

        alvo = c;
        velocidadeAtual = velocidadeInicial;
    }

    // Com o jogo rodando e com alvo: voa até o player acelerando; ao chegar perto chama
    // AoColetar e some.
    private void Update()
    {
        if (alvo == null || !EstadoDoJogo.Rodando)
        {
            return;
        }

        // O alvo (player) sumiu ou foi desativado (ex.: morreu): para de perseguir.
        if (!alvo.gameObject.activeInHierarchy)
        {
            alvo = null;
            return;
        }

        velocidadeAtual += aceleracao * Time.deltaTime;

        Vector3 posAlvo = alvo.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, posAlvo, velocidadeAtual * Time.deltaTime);

        if (Vector3.Distance(transform.position, posAlvo) < DistanciaParaColetar)
        {
            AoColetar(alvo);
            Destroy(gameObject);
        }
    }

    // Efeito de cada tipo de item ao ser coletado; as classes filhas implementam.
    protected abstract void AoColetar(Coletor c);
}
