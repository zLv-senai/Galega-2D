using UnityEngine;

// Tiro disparado pela nave do jogador ou pelos inimigos.
// Anda em linha reta na direção em que foi disparado, dá dano no primeiro
// alvo que tocar e some. Se bater numa parede, para ali.
public class Shot : MonoBehaviour
{
    // Tag usada para identificar paredes/obstáculos que param o tiro.
    private const string TagParede = "Parede";

    // Tag dos objetos que o tiro do jogador pode quebrar (minas). A tag precisa
    // existir em Project Settings > Tags and Layers, senão o CompareTag dá erro.
    private const string TagDestrutivel = "Destrutivel";

    // Velocidade em unidades por segundo. Público para ajustar no Inspector.
    public float velocidade = 10f;

    // Quanto de vida o tiro tira do alvo.
    public int dano = 1;

    // Tag de quem DISPAROU este tiro ("Player" ou "Inimigo"). O tiro ignora tudo com essa
    // tag (nunca acerta quem atirou nem os aliados dele) e acerta qualquer outro objeto
    // que possa levar dano (IDamageable). Quem cria o tiro preenche isto na hora do disparo.
    public string tagGameObject;

    // Se este tiro acerta objetos com a tag "Destrutivel" (minas). O tiro do jogador liga;
    // o tiro dos inimigos desliga (inimigo não explode minas). Também é definido no disparo.
    public bool acertaDestrutiveis = true;

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

    void Start()
    {
        // O Start roda depois de quem criou o tiro terminar o Instantiate e preencher os campos.
        // Sem dono, o tiro acertaria qualquer um (inclusive quem atirou): avisa para corrigir.
        if (string.IsNullOrEmpty(tagGameObject))
        {
            Debug.LogWarning("Shot: tiro sem tagGameObject (quem atirou). Ele vai acertar qualquer um. Preencha no disparo.", this);
        }
    }

    // Preenche quem atirou e se o tiro quebra destrutíveis, logo depois do Instantiate.
    public void Configurar(string atirador, bool quebraDestrutiveis)
    {
        tagGameObject = atirador;
        acertaDestrutiveis = quebraDestrutiveis;
    }

    void Update()
    {
        // Move o tiro para a frente a cada frame. A "frente" é o eixo Y local
        // (a seta verde no editor), então o tiro segue a rotação com que nasceu.
        transform.Translate(Vector2.up * velocidade * Time.deltaTime);
    }

    // A Unity chama este método sozinha quando o collider do tiro
    // encosta em outro collider.
    void OnTriggerEnter2D(Collider2D outro)
    {
        // Quem atirou (e os aliados com a mesma tag) nunca é acertado.
        if (!string.IsNullOrEmpty(tagGameObject) && outro.CompareTag(tagGameObject))
        {
            return;
        }

        // Destrutível (mina) com este tiro desligado para isso: o tiro passa direto.
        if (!acertaDestrutiveis && outro.CompareTag(TagDestrutivel))
        {
            return;
        }

        IDamageable alvo = outro.GetComponent<IDamageable>();

        // GetComponent devolve null se o objeto não tiver nenhum script IDamageable.
        if (alvo != null)
        {
            AcertarAlvo(alvo);
        }
        else if (outro.CompareTag(TagParede))
        {
            BaterNaParede();
        }
    }

    // Dá o dano e o tiro some.
    void AcertarAlvo(IDamageable alvo)
    {
        alvo.TakeDamage(dano);
        Destroy(gameObject);
    }

    // Tudo o que acontece quando o tiro encosta numa parede fica aqui.
    // Quando os power-ups chegarem, é só este método que muda.
    void BaterNaParede()
    {
        Destroy(gameObject);
    }
}
