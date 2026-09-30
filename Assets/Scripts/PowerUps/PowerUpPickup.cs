using UnityEngine;

// Item de power-up solto no chão. É um Coletavel (mesmo contrato da GemaDeXp): o Coletor do
// player atrai o item quando ele entra no raio, e ao chegar o AoColetar ativa o power-up.
// O prefab precisa estar na layer Coletavel e ter um collider trigger.
public class PowerUpPickup : Coletavel
{
    [SerializeField] private PowerUpData dados;

    private void Start()
    {
        // Pickup colocado à mão na cena/prefab com "dados" já preenchido: aplica a cor também.
        AplicarCor();
    }

    // Usado pelo baú logo depois do Instantiate para dizer qual power-up este item dá.
    public void Configurar(PowerUpData novosDados)
    {
        dados = novosDados;
        AplicarCor();
    }

    private void AplicarCor()
    {
        if (dados == null)
        {
            return;
        }

        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = dados.cor;
        }
    }

    protected override void AoColetar(Coletor c)
    {
        if (dados == null)
        {
            Debug.LogWarning("PowerUpPickup: '" + name + "' não tem PowerUpData configurado.");
            return;
        }

        PlayerPowerUps powerUps = c.GetComponent<PlayerPowerUps>();
        if (powerUps == null)
        {
            Debug.LogWarning("PowerUpPickup: o Player não tem o componente PlayerPowerUps.");
            return;
        }

        powerUps.Ativar(dados);
    }
}
