using UnityEngine;

// No inimigo: quando o collider dele encosta no collider do player, causa dano
// e empurra o player para longe. O escudo é tratado dentro do PlayerMove
// (TakeDamage), então aqui o empurrão acontece mesmo quando o escudo bloqueia o dano.
// RequireComponent impede colocar este script em tiros ou outros objetos que não são inimigos.
[RequireComponent(typeof(EnemyMove))]
public class DanoPorContato : MonoBehaviour
{
    [SerializeField] private int dano = 10;
    [SerializeField] private float margemContato = 0.05f; // folga entre as bordas dos colliders que ainda conta como encostar
    [SerializeField] private float raioContato = 0.7f;    // reserva: só usado se o inimigo ou o player não tiver collider
    [SerializeField] private float intervalo = 0.5f;      // segundos entre um golpe e o próximo
    [SerializeField] private float forcaEmpurrao = 8f;
    [SerializeField] private Transform alvo;              // se vazio, procura o objeto com a tag Player

    private float proximoDano;
    private IDamageable alvoDano;
    private Empurravel alvoEmpurravel;
    private EnemyMove inimigo;
    private Collider2D meuCollider;
    private Collider2D colliderAlvo;

    private void Start()
    {
        inimigo = GetComponent<EnemyMove>();
        meuCollider = GetComponent<Collider2D>();

        if (alvo == null)
        {
            GameObject jogador = Jogador.Encontrar();
            if (jogador != null)
            {
                alvo = jogador.transform;
            }
        }

        // Colocado no próprio Player por engano: ele se acharia pela tag e bateria em si mesmo o tempo todo.
        if (alvo == transform)
        {
            Debug.LogError("DanoPorContato: este componente está no próprio Player. Ele deve ficar só nos inimigos. Remova-o (e o EnemyMove que a Unity adicionou junto) do Player.", this);
            enabled = false;
            return;
        }

        if (alvo != null)
        {
            alvoDano = alvo.GetComponent<IDamageable>();
            alvoEmpurravel = alvo.GetComponent<Empurravel>();
            colliderAlvo = alvo.GetComponent<Collider2D>();

            if (alvoEmpurravel == null)
            {
                Debug.LogWarning("DanoPorContato: o Player não tem o componente Empurravel, então só vai levar dano, sem empurrão.");
            }
        }

        if (meuCollider == null || colliderAlvo == null)
        {
            Debug.LogWarning("DanoPorContato: inimigo ou Player sem Collider2D. Usando a distância entre os centros (raioContato) no lugar do contato real.", this);
        }
    }

    // Contato real: as bordas dos dois colliders estão encostadas (ou sobrepostas).
    // Tiros e outros objetos não contam, porque só o collider deste inimigo é comparado.
    private bool EstaEncostando(Vector2 doInimigoParaOAlvo)
    {
        if (meuCollider != null && colliderAlvo != null)
        {
            ColliderDistance2D distancia = meuCollider.Distance(colliderAlvo);
            return distancia.isValid && distancia.distance <= margemContato;
        }

        return doInimigoParaOAlvo.sqrMagnitude <= raioContato * raioContato;
    }

    private void Update()
    {
        if (!EstadoDoJogo.Rodando || alvo == null || !alvo.gameObject.activeInHierarchy)
        {
            return;
        }

        // Inimigo já morreu neste frame (o Destroy só acontece no fim do frame): não bate mais.
        if (inimigo != null && inimigo.vida < 1)
        {
            return;
        }

        if (Time.time < proximoDano)
        {
            return;
        }

        Vector2 doInimigoParaOAlvo = alvo.position - transform.position;
        if (!EstaEncostando(doInimigoParaOAlvo))
        {
            return;
        }

        proximoDano = Time.time + intervalo;

        // Empurra primeiro; assim, se o golpe matar o player, ele não recebe empurrão depois de desativado.
        if (alvoEmpurravel != null)
        {
            alvoEmpurravel.Empurrar(doInimigoParaOAlvo, forcaEmpurrao);
        }

        if (alvoDano != null)
        {
            alvoDano.TakeDamage(dano);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioContato);
    }
}
