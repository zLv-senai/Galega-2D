using UnityEngine;

// Envia a posição e a velocidade em tempo real da câmera para a GPU
public class ShaderStarfieldController : MonoBehaviour
{
    // No 2D Renderer manda a ordem: abaixo de tudo, senao o Quad opaco cobre rastros, aneis e escudo (ordens negativas).
    private const int OrdemDoCeu = -200;

    private Material starfieldMaterial;
    private Transform mainCameraTransform;
    
    private static readonly int CamPosProperty = Shader.PropertyToID("_CamPos");
    private static readonly int CamVelocityProperty = Shader.PropertyToID("_CamVelocity");
    
    private Vector3 lastPosition;

    void Start()
    {
        Renderer ceu = GetComponent<Renderer>();
        ceu.sortingOrder = OrdemDoCeu;
        // Copia de runtime: escrever no asset muda o .mat a cada Play e gera conflito no git.
        starfieldMaterial = ceu.material;

        mainCameraTransform = Camera.main.transform;
        lastPosition = mainCameraTransform.position;
    }

    void LateUpdate()
    {
        if (starfieldMaterial != null)
        {
            Vector3 currentPos = mainCameraTransform.position;
            
            // Calcula a velocidade protegendo contra divisão por zero (caso o jogo seja pausado)
            float dt = Time.deltaTime;
            Vector3 velocity = dt > 0f ? (currentPos - lastPosition) / dt : Vector3.zero;
            
            starfieldMaterial.SetVector(CamPosProperty, currentPos);
            starfieldMaterial.SetVector(CamVelocityProperty, velocity);
            
            lastPosition = currentPos;
        }
    }
}