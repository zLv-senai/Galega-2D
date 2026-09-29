using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;   // Quem a câmera vai seguir?  Função SerializeField permite que a variável seja privada, mas ainda assim visível no Inspector do Unity.

    void LateUpdate()
    {
        Vector3 novaPosicao = player.position;   // Qual a posição do player? (1) A câmera vai seguir o player, então a posição da câmera será a mesma do player.

        novaPosicao.z = -10;                  // A posição da câmera no eixo Z precisa ser -10, para que a câmera fique atrás do player e não oculte a visão do jogador.

        transform.position = novaPosicao; // A posição da câmera será a mesma do player, mas com o eixo Z fixo em -10.
    }
}
