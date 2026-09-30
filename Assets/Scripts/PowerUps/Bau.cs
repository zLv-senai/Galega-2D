using System.Collections;
using UnityEngine;

// Baú destrutível: leva tiros do player (o objeto precisa da tag "Destrutivel" e de um collider),
// pisca ao levar dano e, ao quebrar, solta um PowerUpPickup sorteado da tabela.
public class Bau : MonoBehaviour, IDamageable
{
    // Tempo em que o sprite fica vermelho depois de um tiro.
    private const float DuracaoPisca = 0.1f;

    [SerializeField] private int vida = 3;
    [SerializeField] private TabelaDePowerUps tabela;
    [SerializeField] private PowerUpPickup pickupPrefab;
    [SerializeField] private SpriteRenderer sprite; // se vazio, procura no objeto ou nos filhos

    // Impede soltar dois power-ups se vários tiros chegarem no mesmo frame.
    private bool quebrado;

    private Color corOriginal = Color.white;
    private Coroutine piscando;

    private void Awake()
    {
        if (sprite == null)
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (sprite != null)
        {
            corOriginal = sprite.color;
        }
    }

    public void TakeDamage(int dano)
    {
        if (quebrado)
        {
            return;
        }

        vida -= dano;

        if (vida <= 0)
        {
            quebrado = true;
            SoltarPowerUp();
            Destroy(gameObject);
            return;
        }

        if (piscando != null)
        {
            StopCoroutine(piscando);
        }

        piscando = StartCoroutine(Piscar());
    }

    // WaitForSeconds usa o tempo do jogo (não Realtime): o pisca congela junto com a pausa.
    private IEnumerator Piscar()
    {
        if (sprite != null)
        {
            sprite.color = Color.red;
        }

        yield return new WaitForSeconds(DuracaoPisca);

        if (sprite != null)
        {
            sprite.color = corOriginal;
        }

        piscando = null;
    }

    private void SoltarPowerUp()
    {
        if (tabela == null || pickupPrefab == null)
        {
            Debug.LogWarning("Bau: '" + name + "' está sem tabela de power-ups ou sem prefab do pickup. Nada foi solto.");
            return;
        }

        PowerUpData sorteado = tabela.Sortear();
        if (sorteado == null)
        {
            Debug.LogWarning("Bau: a tabela de power-ups está vazia (ou todos com peso 0). Nada foi solto.");
            return;
        }

        PowerUpPickup pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        pickup.Configurar(sorteado);
    }
}
