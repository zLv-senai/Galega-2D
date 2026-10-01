using UnityEngine;

// Fica num GameObject da cena (ex.: "Sistemas"): sempre que um inimigo morre,
// cria uma gema de XP na posição dele.
public class GeradorDeGemas : MonoBehaviour
{
    // Prefab da gema e o valor de XP que cada gema criada vai dar.
    [SerializeField] private GameObject gemaPrefab;
    [SerializeField] private int valorPadrao = 1;

    // Passa a ouvir a morte de inimigos (evento estático EnemyMove.AoMorrer).
    private void OnEnable()
    {
        EnemyMove.AoMorrer += CriarGema;
    }

    // Para de ouvir a morte de inimigos.
    private void OnDisable()
    {
        EnemyMove.AoMorrer -= CriarGema;
    }

    // Cria a gema onde o inimigo morreu e define o XP dela.
    private void CriarGema(EnemyMove inimigo)
    {
        if (gemaPrefab == null || inimigo == null)
        {
            return;
        }

        GameObject gema = Instantiate(gemaPrefab, inimigo.transform.position, Quaternion.identity);

        GemaDeXp gemaDeXp = gema.GetComponent<GemaDeXp>();
        if (gemaDeXp != null)
        {
            gemaDeXp.valor = valorPadrao;
        }
    }
}
