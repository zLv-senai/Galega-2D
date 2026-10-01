using UnityEngine;

// No Player: mostra o círculo do escudo (um filho com sprite) enquanto houver cargas
// e esconde quando as cargas acabam.
[RequireComponent(typeof(PlayerStats))]
public class VisualEscudo : MonoBehaviour
{
    // Filho do Player com o sprite do círculo do escudo (ligado ou desligado conforme as cargas).
    [SerializeField] private GameObject visual;

    private PlayerStats stats;

    // Pega o PlayerStats do próprio Player.
    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    // Passa a ouvir a mudança de cargas do escudo e já ajusta o visual com as cargas atuais.
    private void OnEnable()
    {
        if (stats != null)
        {
            stats.AoMudarCargasEscudo += Atualizar;
            Atualizar(stats.CargasEscudo);
        }
    }

    // Reajusta o visual depois que o PlayerStats já definiu as cargas iniciais.
    private void Start()
    {
        // O PlayerStats define as cargas iniciais no Awake dele; aqui já é seguro ler.
        if (stats != null)
        {
            Atualizar(stats.CargasEscudo);
        }
    }

    // Para de ouvir a mudança de cargas do escudo.
    private void OnDisable()
    {
        if (stats != null)
        {
            stats.AoMudarCargasEscudo -= Atualizar;
        }
    }

    // Mostra o círculo do escudo se ainda há cargas e esconde quando acabam.
    private void Atualizar(int cargas)
    {
        if (visual != null)
        {
            visual.SetActive(cargas > 0);
        }
    }
}
