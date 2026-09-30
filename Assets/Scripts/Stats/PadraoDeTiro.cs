using UnityEngine;

// Cria os tiros do player a partir dos stats atuais: um leque de projéteis
// centrado na direção da mira, com o dano definido pelos stats.
// O tiro anda no eixo Y local (up), então a rotação faz o "up" apontar para a mira.
public static class PadraoDeTiro
{
    // Ângulo entre cada projétil do leque, quando há mais de um.
    private const float AnguloEntreTiros = 12f;

    // Tag que marca os tiros do player (o Shot ignora quem tem a mesma tag de quem atirou).
    private const string TagPlayer = "Player";

    // "direcao" = para onde mirar (ex.: da arma até o mouse). Se vier zero, usa a rotação da arma.
    public static void Disparar(GameObject prefab, Transform gun, PlayerStats stats, Vector2 direcao)
    {
        if (prefab == null || gun == null)
        {
            return;
        }

        int quantidade = stats != null ? stats.Projeteis : 1;
        int dano = stats != null ? stats.Dano : 1;

        // Rotação base: "up" apontando para a mira. Não depende de como a Gun está girada no prefab.
        Quaternion baseRotacao = direcao.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(Vector3.forward, direcao)
            : gun.rotation;

        // Centraliza o leque na direção da mira (ex.: 3 tiros = -12°, 0°, +12°).
        float anguloInicial = -(quantidade - 1) * AnguloEntreTiros * 0.5f;

        for (int i = 0; i < quantidade; i++)
        {
            float anguloTiro = anguloInicial + i * AnguloEntreTiros;
            Quaternion rotacao = baseRotacao * Quaternion.Euler(0f, 0f, anguloTiro);

            GameObject tiro = Object.Instantiate(prefab, gun.position, rotacao);

            Shot shot = tiro.GetComponent<Shot>();
            if (shot != null)
            {
                shot.dano = dano;
                // Tiro do player: não acerta o próprio player e quebra minas.
                shot.Configurar(TagPlayer, true);
            }
        }
    }
}
