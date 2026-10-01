using System.Collections;
using UnityEngine;

// Baú destrutível: leva tiros do player (o objeto precisa da tag "Destrutivel" e de um collider),
// pisca ao levar dano e, ao quebrar, solta um PowerUpPickup sorteado da tabela.
public class Bau : MonoBehaviour, IDamageable
{
    // Tempo em que o sprite fica vermelho depois de um tiro.
    private const float DuracaoPisca = 0.1f;

    // Vida do baú, a tabela de onde sai o power-up e o prefab do item que cai.
    [SerializeField] private int vida = 3;
    [SerializeField] private TabelaDePowerUps tabela;
    [SerializeField] private PowerUpPickup pickupPrefab;
    [SerializeField] private SpriteRenderer sprite; // se vazio, procura no objeto ou nos filhos

    // Impede soltar dois power-ups se vários tiros chegarem no mesmo frame.
    private bool quebrado;

    // Cor normal do sprite e o pisca em andamento (reiniciado se vier outro tiro).
    private Color corOriginal = Color.white;
    private Coroutine piscando;

    // Acha o SpriteRenderer (se não foi arrastado) e guarda a cor original.
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

    // Leva o dano de um tiro: ao zerar a vida solta o power-up e some; senão pisca de vermelho.
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

    // Sorteia um power-up da tabela e cria o item no lugar do baú (só avisa no console se faltar algo).
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
