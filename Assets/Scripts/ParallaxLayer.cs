using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform; // Tem que ser a câmera, para saber quanto ela se moveu desde o último frame.   A câmera é um Transform, então podemos usar a posição dela.
    
    [SerializeField] private float parallax = 0.5f;           // Fator de parallax, quanto menor o valor, mais distante a camada parece estar.

    private Vector3 backposition;                                 // Função LateUpdate() é chamada depois de Update(), então a posição da câmera já foi atualizada, e podemos calcular o quanto ela se moveu desde o último frame.

    void Start()
    {
         backposition = cameraTransform.position;                  // Guardando a posição da câmera no início do jogo, para calcular o delta no próximo frame.
    }

    void LateUpdate()
    {
        Vector3 delta = cameraTransform.position - backposition;  // Calculando o quanto a câmera se moveu desde o último frame.  delta = posição atual - posição anterior
        
        transform.position += new Vector3(delta.x * parallax, delta.y * parallax, 0);  // (6) Movendo a camada de parallax de acordo com o delta da câmera, multiplicado pelo fator de parallax.  Quanto menor o fator, menos a camada se move, dando a sensação de que está mais distante.

        backposition = cameraTransform.position;                  // Atualizando a posição da câmera para o próximo frame.
    }
}