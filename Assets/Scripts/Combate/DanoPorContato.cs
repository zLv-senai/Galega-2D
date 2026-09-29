using UnityEngine;

// No inimigo: quando encosta no player, causa dano e empurra ele para longe.
// O escudo é tratado dentro do PlayerMove (TakeDamage), então aqui o empurrão
// acontece mesmo quando o escudo bloqueia o dano.
public class DanoPorContato : MonoBehaviour
{
    [SerializeField] private int dano = 10;
    [SerializeField] private float raioContato = 0.7f;
    [SerializeField] private float intervalo = 0.5f;    // segundos entre um golpe e o próximo
    [SerializeField] private float forcaEmpurrao = 8f;
    [SerializeField] private Transform alvo;            // se vazio, procura o objeto com a tag Player

    private float proximoDano;
    private IDamageable alvoDano;
    private Empurravel alvoEmpurravel;
    private EnemyMove inimigo;

    private void Start()
    {
        inimigo = GetComponent<EnemyMove>();

        if (alvo == null)
        {
            GameObject jogador = GameObject.FindWithTag("Player");
            if (jogador != null)
            {
                alvo = jogador.transform;
            }
        }

        if (alvo != null)
        {
            alvoDano = alvo.GetComponent<IDamageable>();
            alvoEmpurravel = alvo.GetComponent<Empurravel>();

            if (alvoEmpurravel == null)
            {
                Debug.LogWarning("DanoPorContato: o Player não tem o componente Empurravel, então só vai levar dano, sem empurrão.");
            }
        }
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
        if (doInimigoParaOAlvo.sqrMagnitude > raioContato * raioContato)
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
