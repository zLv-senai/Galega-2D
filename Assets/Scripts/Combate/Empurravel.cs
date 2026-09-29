using UnityEngine;

// No Player: recebe empurrões (ex.: contato com inimigo). Não usa Rigidbody:
// move o transform direto e vai freando até parar.
public class Empurravel : MonoBehaviour
{
    [SerializeField] private float forcaMax = 12f;      // velocidade máxima do empurrão
    [SerializeField] private float desaceleracao = 30f; // quanto a velocidade cai por segundo

    private Vector2 velocidade;

    // Soma um impulso na direção dada (a direção é normalizada; "forca" é a velocidade inicial).
    public void Empurrar(Vector2 direcao, float forca)
    {
        if (direcao.sqrMagnitude < 0.0001f)
        {
            return;
        }

        velocidade = Vector2.ClampMagnitude(velocidade + direcao.normalized * forca, forcaMax);
    }

    private void Update()
    {
        if (!EstadoDoJogo.Rodando || velocidade == Vector2.zero)
        {
            return;
        }

        transform.position += (Vector3)(velocidade * Time.deltaTime);
        velocidade = Vector2.MoveTowards(velocidade, Vector2.zero, desaceleracao * Time.deltaTime);
    }

    private void OnDisable()
    {
        // Player desativado (morreu): não guarda empurrão para depois.
        velocidade = Vector2.zero;
    }
}
