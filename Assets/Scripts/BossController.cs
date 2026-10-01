using UnityEngine;

public enum PadraoBoss { Oscilar, Seguir }

public class BossController : MonoBehaviour, IDamageable
// Este script controla o comportamento do boss, incluindo movimento, tiro e vida. Ele implementa a interface IDamageable para receber dano de tiros.
{
    [Header("Referências")]
    public GameManager gameManager;
    public Transform target;
    public GameObject tiroPrefab;
    public Transform gun;

    [Header("Vida")]
    public int vida = 20;

    [Header("Movimento")]
    public PadraoBoss padrao = PadraoBoss.Oscilar;
    public float velocidade = 2f;
    public float amplitudeX = 3f;
    public float amplitudeY = 0f;

    [Header("Tiro")]
    public float intervaloTiro = 1.5f;
    public int quantidadeTiros = 3;
    public float anguloEntreTiros = 15f;
    public float anguloBase = 0f;

    private Vector3 posicaoInicial;
    private float tempo;
    private float timerTiro;

    void Start()
    {
        posicaoInicial = transform.position;
    }

    void Update()
    {
        if (gameManager.gameState == GameManager.GameState.OnPlay)
        {
            Mover();
            Atirar();
            //Isso garante que o boss só se move e atira quando o jogo está em andamento, evitando que ele continue agindo durante pausas ou menus.
        }
    }

    void Mover()
    {
        switch (padrao)
        {
            case PadraoBoss.Oscilar:
                tempo += Time.deltaTime * velocidade;

                float offsetX = Mathf.Sin(tempo) * amplitudeX;
                float offsetY = Mathf.Cos(tempo) * amplitudeY;

                // O Z fica 0 para não mudar a profundidade do objeto na tela.
                transform.position = posicaoInicial + new Vector3(offsetX, offsetY, 0f);
                break;

            case PadraoBoss.Seguir:
                transform.position = Vector2.MoveTowards(transform.position, target.position, velocidade * Time.deltaTime);
                break;
                // Se quiser adicionar mais padrões de movimento, é só colocar mais cases aqui.
        }
    }

    void Atirar()
    {
        timerTiro += Time.deltaTime;

        if (timerTiro >= intervaloTiro)
        {
            timerTiro = 0f;
            Disparar();
            // Se quiser adicionar mais efeitos de tiro, é só colocar mais cases aqui.
        }
    }

    void Disparar()
    {
        // Ângulo do primeiro tiro, para o leque ficar centralizado na mira da arma.
        float anguloInicial = -((quantidadeTiros - 1) * anguloEntreTiros / 2f);

        for (int i = 0; i < quantidadeTiros; i++)
        {
            float angulo = anguloInicial + (i * anguloEntreTiros) + anguloBase;

            // Multiplicar rotações = gira a rotação da arma por mais este ângulo.
            Quaternion rotacao = gun.rotation * Quaternion.Euler(0, 0, angulo);
            Instantiate(tiroPrefab, gun.position, rotacao);
          //Isso faz com que o tiro seja instanciado na posição da arma, com a rotação da arma mais o ângulo calculado para cada tiro do leque.
        }
    }

    public void TakeDamage(int dano)
    {
        vida -= dano;

        if (vida < 1)
        {
            Destroy(gameObject);
        }
    }
    // O método TakeDamage é chamado quando o boss leva dano. Ele diminui a vida do boss e, se a vida chegar a zero, destrói o objeto do boss.
}