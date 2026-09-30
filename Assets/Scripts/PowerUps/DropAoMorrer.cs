using UnityEngine;

// Fica num GameObject da cena (ex.: "Sistemas"): sempre que um inimigo morre, tem "chancePowerUp"
// de soltar um power-up sorteado da tabela no lugar dele. A gema de XP (100%) continua vindo do GeradorDeGemas.
public class DropAoMorrer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float chancePowerUp = 0.2f;
    [SerializeField] private PowerUpPickup pickupPrefab;
    [SerializeField] private TabelaDePowerUps tabela;

    private void OnEnable()
    {
        EnemyMove.AoMorrer += TentarSoltar;
    }

    private void OnDisable()
    {
        EnemyMove.AoMorrer -= TentarSoltar;
    }

    private void TentarSoltar(EnemyMove inimigo)
    {
        if (inimigo == null || Random.value >= chancePowerUp)
        {
            return;
        }

        if (pickupPrefab == null || tabela == null)
        {
            Debug.LogWarning("DropAoMorrer: faltam o Pickup Prefab ou a Tabela. Nenhum power-up foi solto.", this);
            return;
        }

        PowerUpData sorteado = tabela.Sortear();
        if (sorteado == null)
        {
            return;
        }

        PowerUpPickup pickup = Instantiate(pickupPrefab, inimigo.transform.position, Quaternion.identity);
        pickup.Configurar(sorteado);
    }
}
