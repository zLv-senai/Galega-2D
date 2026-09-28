using UnityEngine;

// Fica num GameObject da cena (ex.: "Sistemas"): sempre que um inimigo morre,
// cria uma gema de XP na posição dele.
public class GeradorDeGemas : MonoBehaviour
{
    [SerializeField] private GameObject gemaPrefab;
    [SerializeField] private int valorPadrao = 1;

    private void OnEnable()
    {
        EnemyMove.AoMorrer += CriarGema;
    }

    private void OnDisable()
    {
        EnemyMove.AoMorrer -= CriarGema;
    }

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
