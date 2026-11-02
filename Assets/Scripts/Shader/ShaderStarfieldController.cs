using UnityEngine;

// Envia a posição e a velocidade em tempo real da câmera para a GPU
public class ShaderStarfieldController : MonoBehaviour
{
    [SerializeField] private Material starfieldMaterial;
    private Transform mainCameraTransform;
    
    private static readonly int CamPosProperty = Shader.PropertyToID("_CamPos");
    private static readonly int CamVelocityProperty = Shader.PropertyToID("_CamVelocity");
    
    private Vector3 lastPosition;

    void Start()
    {
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