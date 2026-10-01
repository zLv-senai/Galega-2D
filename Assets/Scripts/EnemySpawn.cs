using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemySpawn : MonoBehaviour
{
    // Lados da tela (o EnemyMove usa para reaparecer do lado oposto ao que ficou).
    public const int LadoCima = 0;
    public const int LadoBaixo = 1;
    public const int LadoEsquerda = 2;
    public const int LadoDireita = 3;

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
    // não apareca na camera, retorna como Vector2 (relativo ao centro da câmera)
    public Vector2 SpawnPosition()
    {
        return DeslocamentoForaDaTela(mainCamera, UnityEngine.Random.Range(0, 4), margem);
    }

    // Deslocamento (relativo ao centro da câmera) de um ponto aleatório logo fora do lado pedido da tela.
    public static Vector2 DeslocamentoForaDaTela(Camera cam, int lado, float margem)
    {
        float altura = cam.orthographicSize;
        float largura = altura * cam.aspect;

        switch (lado)
        {
            case LadoCima:
                return new Vector2(UnityEngine.Random.Range(-largura, largura), altura + margem);
            case LadoBaixo:
                return new Vector2(UnityEngine.Random.Range(-largura, largura), -altura - margem);
            case LadoEsquerda:
                return new Vector2(-largura - margem, UnityEngine.Random.Range(-altura, altura));
            default: // LadoDireita
                return new Vector2(largura + margem, UnityEngine.Random.Range(-altura, altura));
        }
    }

    // Mesmo ponto, já em coordenadas do mundo (a câmera segue o player), com Z = 0.
    public static Vector3 PontoForaDaTela(Camera cam, int lado, float margem)
    {
        Vector2 ponto = (Vector2)cam.transform.position + DeslocamentoForaDaTela(cam, lado, margem);
        return new Vector3(ponto.x, ponto.y, 0f);
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