using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CameraStarfield : MonoBehaviour
{
    [Header("Configurações do Espaço")]
    [SerializeField] private int numberOfStars = 300;
    [SerializeField] private float fieldWidth = 40f; 
    [SerializeField] private float fieldHeight = 30f; 
    [SerializeField] private float minSize = 0.05f;
    [SerializeField] private float maxSize = 0.2f;

    private ParticleSystem starParticleSystem;
    private ParticleSystem.Particle[] stars;
    private Vector3 lastCameraPosition;
    
    // Cache para evitar divisões matemáticas repetidas dentro do Update
    private float halfWidth;
    private float halfHeight;

    void Start()
    {
        lastCameraPosition = transform.position; 
        
        halfWidth = fieldWidth * 0.5f;
        halfHeight = fieldHeight * 0.5f;

        starParticleSystem = GetComponent<ParticleSystem>();
        
        var main = starParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = numberOfStars;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        starParticleSystem.Emit(numberOfStars);
        stars = new ParticleSystem.Particle[numberOfStars];
        starParticleSystem.GetParticles(stars);

        for (int i = 0; i < numberOfStars; i++)
        {
            float x = Random.Range(-halfWidth, halfWidth);
            float y = Random.Range(-halfHeight, halfHeight);
            float depth = Random.Range(0.1f, 1f); 
            
            stars[i].position = new Vector3(x, y, depth) + transform.position;
            stars[i].startSize = Mathf.Lerp(minSize, maxSize, depth);
            
            float brightness = Mathf.Lerp(0.15f, 1f, depth);
            stars[i].startColor = new Color(1f, 1f, 1f, brightness);
        }

        starParticleSystem.SetParticles(stars, numberOfStars);
    }

    void LateUpdate()
    {
        Vector3 camPos = transform.position;
        Vector3 delta = camPos - lastCameraPosition; 
        
        // Se a câmera não se moveu, não processa o loop
        if (delta.x == 0f && delta.y == 0f) return;

        starParticleSystem.GetParticles(stars);

        for (int i = 0; i < numberOfStars; i++)
        {
            // Otimização: Manipular uma variável de struct local evita alocações de 'new Vector3' na memória
            Vector3 pos = stars[i].position;
            
            float depth = pos.z; 
            float parallaxFactor = 1f - depth;
            
            // Soma direta nos eixos, sem instanciar novos vetores
            pos.x += delta.x * parallaxFactor;
            pos.y += delta.y * parallaxFactor;

            // Cálculo local otimizado para o Wrap Around
            float localX = pos.x - camPos.x;
            float localY = pos.y - camPos.y;
            
            if (localX > halfWidth) pos.x -= fieldWidth;
            else if (localX < -halfWidth) pos.x += fieldWidth;
            
            if (localY > halfHeight) pos.y -= fieldHeight;
            else if (localY < -halfHeight) pos.y += fieldHeight;

            // Devolve a posição alterada para a partícula
            stars[i].position = pos;
        }

        starParticleSystem.SetParticles(stars, numberOfStars);
        lastCameraPosition = camPos; 
    }
}