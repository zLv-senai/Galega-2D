using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

// Cria inimigos logo fora da tela, num lado aleatório. Cada inimigo é sorteado entre os modelos
// (EnemyUno, EnemyDuo e EnemyThree do Resources, ou os arrastados no Inspector).
// Quem decide quando criar é o SpawnContinuo ou o GerenciadorDeWaves.
public class EnemySpawn : MonoBehaviour
{
    // Lados da tela (o EnemyMove usa para reaparecer do lado oposto ao que ficou).
    public const int LadoCima = 0;
    public const int LadoBaixo = 1;
    public const int LadoEsquerda = 2;
    public const int LadoDireita = 3;

    // Integração (inimigos aleatórios): nomes dentro de Assets/Resources. Os 3 modelos novos são sorteados;
    // o inimigo antigo ("Enemy") só é usado se nenhum deles existir.
    private static readonly string[] InimigosNoResources = { "EnemyUno", "EnemyDuo", "EnemyThree" };
    private static readonly string[] InimigoAntigoNoResources = { "Enemy" };

    // Integração (inimigos aleatórios): modelos sorteados a cada spawn. Vazio = os do Resources acima.
    [SerializeField] private GameObject[] modelosDeInimigo;

    // Câmera principal, GameManager e modelos de inimigo (preenchidos no Awake) e o último ponto de spawn usado.
    private Camera mainCamera;
    private Vector2 spawnPoint;

    private GameManager gameManager;
    private GameObject[] modelos;

    // Distância (em unidades) para fora da borda da tela onde o inimigo nasce.
    [SerializeField] public int margem = 1;

    // Nenhum script lê este campo hoje.
    [SerializeField] public Vector2 direcaoRay = Vector2.up;

    // Acha o GameManager, a câmera principal e carrega os modelos de inimigo (Inspector ou Resources).
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
        if (modelos == null)
        {
            modelos = SorteioDeModelos.Carregar(modelosDeInimigo, InimigosNoResources, this);
            if (modelos.Length == 0)
            {
                modelos = SorteioDeModelos.Carregar(null, InimigoAntigoNoResources, this);
            }
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
    // Integração (inimigos aleatórios): cada chamada sorteia um dos modelos (pode repetir).
    public GameObject SpawnEnemy()
    {
        if (modelos == null || modelos.Length == 0 || mainCamera == null)
        {
            Debug.LogWarning("EnemySpawn: faltam os modelos de inimigo (EnemyUno/EnemyDuo/EnemyThree ou Enemy em Resources) ou a Main Camera.");
            return null;
        }

        GameObject modelo = modelos[UnityEngine.Random.Range(0, modelos.Length)];

        // Integração: soma a posição da câmera, porque ela segue o player (CameraFollow);
        // sem isso os inimigos nasceriam em volta do centro do mundo.
        spawnPoint = (Vector2)mainCamera.transform.position + SpawnPosition();

        return Instantiate(modelo, spawnPoint, quaternion.identity);
    }
}