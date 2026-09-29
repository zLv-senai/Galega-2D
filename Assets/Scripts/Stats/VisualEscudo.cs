using UnityEngine;

// No Player: mostra o círculo do escudo (um filho com sprite) enquanto houver cargas
// e esconde quando as cargas acabam.
[RequireComponent(typeof(PlayerStats))]
public class VisualEscudo : MonoBehaviour
{
    [SerializeField] private GameObject visual;

    private PlayerStats stats;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (stats != null)
        {
            stats.AoMudarCargasEscudo += Atualizar;
            Atualizar(stats.CargasEscudo);
        }
    }

    private void Start()
    {
        // O PlayerStats define as cargas iniciais no Awake dele; aqui já é seguro ler.
        if (stats != null)
        {
            Atualizar(stats.CargasEscudo);
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.AoMudarCargasEscudo -= Atualizar;
        }
    }

    private void Atualizar(int cargas)
    {
        if (visual != null)
        {
            visual.SetActive(cargas > 0);
        }
    }
}
