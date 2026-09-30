using UnityEngine;

// Tiro disparado pela nave do jogador ou pelos inimigos.
// Anda em linha reta na direção em que foi disparado, dá dano no primeiro
// alvo que tocar e some. Se bater numa parede, para ali.
public class Shot : MonoBehaviour
{
    // Tag usada para identificar paredes/obstáculos que param o tiro.
    private const string TagParede = "Parede";

    // Tag dos objetos que o tiro do jogador pode quebrar (baús, minas). A tag precisa
    // existir em Project Settings > Tags and Layers, senão o CompareTag dá erro.
    private const string TagDestrutivel = "Destrutivel";

    // Velocidade em unidades por segundo. Público para ajustar no Inspector.
    public float velocidade = 10f;

    // Quanto de vida o tiro tira do alvo.
    public int dano = 1;

    // Tag de quem este tiro pode acertar: "Inimigo" no tiro do jogador,
    // "Player" no tiro dos inimigos. Qualquer objeto com outra tag é ignorado,
    // então o tiro nunca acerta quem disparou.
    public string tagAlvo = "Inimigo";

    // Se este tiro acerta objetos com a tag "Destrutivel". Ligado no tiro do jogador;
    // o tiro dos inimigos (ShotEnemy) deve desligar isto para não quebrar baús.
    [SerializeField] private bool acertaDestrutiveis = true;

    void Awake()
    {
        // Colisão via trigger 2D exige Rigidbody2D em pelo menos um dos lados.
        // Se o prefab já não tiver um, adiciona um cinemático (sem física),
        // sem mexer em nenhum Rigidbody2D que já exista.
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        // Move o tiro para a frente a cada frame. A "frente" é o eixo X local
        // (a seta vermelha no editor), então o tiro segue a rotação com que nasceu.
        transform.Translate(Vector2.right * velocidade * Time.deltaTime);
    }

    // A Unity chama este método sozinha quando o collider do tiro
    // encosta em outro collider.
    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag(tagAlvo))
        {
            AcertarAlvo(outro);
        }
        else if (acertaDestrutiveis && outro.CompareTag(TagDestrutivel))
        {
            // Mesma regra do alvo: dá dano se tiver IDamageable e o tiro some.
            AcertarAlvo(outro);
        }
        else if (outro.CompareTag(TagParede))
        {
            BaterNaParede();
        }
    }

    // Só chega aqui quem tem a tag do alvo. Dá dano se o objeto puder levar dano.
    void AcertarAlvo(Collider2D outro)
    {
        IDamageable alvo = outro.GetComponent<IDamageable>();

        // GetComponent devolve null se o objeto não tiver nenhum script IDamageable.
        if (alvo != null)
        {
            alvo.TakeDamage(dano);
            Destroy(gameObject);
        }
    }

    // Tudo o que acontece quando o tiro encosta numa parede fica aqui.
    // Quando os power-ups chegarem, é só este método que muda.
    void BaterNaParede()
    {
        Destroy(gameObject);
    }
}
