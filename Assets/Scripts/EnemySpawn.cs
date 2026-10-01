using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemySpawn : MonoBehaviour
{
    private Camera mainCamera;
    private Vector2 spawnPoint;

    private GameManager gameManager;
    private GameObject enemy;

    [SerializeField] public int margem = 1;

    [SerializeField] public Vector2 direcaoRay = Vector2.up;

    void Awake()
    {
        if(gameManager == null)
        {
            gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (enemy == null)
        {
            enemy = Resources.Load<GameObject>("Enemy");
        }
    
    }

    // Gera uma posiçao de spawn aleatoria em um dos quatro lados da tela com margem para que o inimigo
    // não apareca na camera, retorna como Vector2
    public Vector2 SpawnPosition()
    {

        float altura = mainCamera.orthographicSize;
        float largura = altura * mainCamera.aspect;

        int lado = UnityEngine.Random.Range(0, 4);

        switch (lado)
        {
            case 0: // cima
                return new Vector2(
                UnityEngine.Random.Range(-largura, largura),
                altura + margem
                );

            case 1: // baixo
                return new Vector2(
                UnityEngine.Random.Range(-largura, largura),
                -altura - margem
                );

            case 2: // esquerda
                return new Vector2(
                -largura - margem,
                UnityEngine.Random.Range(-altura, altura)
                );

            default: // direita
                return new Vector2(
                largura + margem,
                UnityEngine.Random.Range(-altura, altura)
                );
        }
    }

    public void SpawnEnemy()
    {
        spawnPoint = SpawnPosition();
        
            Instantiate(enemy, spawnPoint, quaternion.identity);
    }
}