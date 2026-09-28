using UnityEngine;

// Cria os tiros do player a partir dos stats atuais: um leque de projéteis
// centrado na rotação da arma, com o dano definido pelos stats.
public static class PadraoDeTiro
{
    // Ângulo entre cada projétil do leque, quando há mais de um.
    private const float AnguloEntreTiros = 12f;

    public static void Disparar(GameObject prefab, Transform gun, PlayerStats stats)
    {
        if (prefab == null || gun == null)
        {
            return;
        }

        int quantidade = stats != null ? stats.Projeteis : 1;
        int dano = stats != null ? stats.Dano : 1;

        // Centraliza o leque na rotação da arma (ex.: 3 tiros = -12°, 0°, +12°).
        float anguloInicial = -(quantidade - 1) * AnguloEntreTiros * 0.5f;

        for (int i = 0; i < quantidade; i++)
        {
            float anguloTiro = anguloInicial + i * AnguloEntreTiros;
            Quaternion rotacao = gun.rotation * Quaternion.Euler(0f, 0f, anguloTiro);

            GameObject tiro = Object.Instantiate(prefab, gun.position, rotacao);

            Shot shot = tiro.GetComponent<Shot>();
            if (shot != null)
            {
                shot.dano = dano;
            }
        }
    }
}
