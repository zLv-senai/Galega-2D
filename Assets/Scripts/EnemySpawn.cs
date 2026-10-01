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
        // Integração: a tag "GameManager" não existe neste projeto (FindGameObjectWithTag
        // daria erro); usa o singleton do GameManager.
        if(gameManager == null)
        {
            gameManager = GameManager.Instance;
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

    // Integração: agora devolve o inimigo criado (null se não deu para criar), para o GerenciadorDeWaves contá-lo.
    // Quem chamava sem usar o retorno (SpawnContinuo) continua funcionando igual.
    public GameObject SpawnEnemy()
    {
        if (enemy == null || mainCamera == null)
        {
            Debug.LogWarning("EnemySpawn: falta o prefab Resources/Enemy ou a Main Camera.");
            return null;
        }

        // Integração: soma a posição da câmera, porque ela segue o player (CameraFollow);
        // sem isso os inimigos nasceriam em volta do centro do mundo.
        spawnPoint = (Vector2)mainCamera.transform.position + SpawnPosition();

        return Instantiate(enemy, spawnPoint, quaternion.identity);
    }
}