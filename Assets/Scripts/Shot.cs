using UnityEngine;

// Tiro disparado pela nave do jogador.
// Sobe em linha reta, dá dano no primeiro inimigo que tocar e some.
// Se bater numa parede, para ali.
public class Shot : MonoBehaviour
{
    // Velocidade em unidades por segundo. Público para ajustar no Inspector.
    public float velocidade = 10f;

    // Quanto de vida o tiro tira do inimigo.
    public int dano = 1;

    void Update()
    {
        // Move o tiro para cima a cada frame.
        transform.Translate(Vector2.up * velocidade * Time.deltaTime);
    }

    // A Unity chama este método sozinha quando o collider do tiro
    // encosta em outro collider.
    void OnTriggerEnter2D(Collider2D outro)
    {
        // Procura o script de vida no objeto atingido.
        IDamageable alvo = outro.GetComponent<IDamageable>();

        // Se o objeto atingido não tem EnemyHealth, GetComponent devolve null.
        if (alvo != null)
        {
            alvo.TakeDamage(dano);
            Destroy(gameObject);
        }
        else if (outro.CompareTag("Parede"))
        {
            BaterNaParede();
        }
    }

    // Tudo o que acontece quando o tiro encosta numa parede fica aqui.
    // Quando os power-ups chegarem, é só este método que muda.
    void BaterNaParede()
    {
        Destroy(gameObject);
    }
}
