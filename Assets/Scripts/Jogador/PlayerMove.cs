using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Controle do player: anda com WASD, atira com o botão esquerdo do mouse, recebe dano e chama o Game Over ao morrer.
[RequireComponent(typeof(PlayerStats))] // PlayerStats: player sempre precisa dos stats
public class PlayerMove : MonoBehaviour, IDamageable
{
    // Vida atual (começa cheia pelo VidaMax dos stats, no Awake).
    public int vida = 100;
    public int level = 1; // PlayerStats: level/XP agora vivem em PlayerXp; campo mantido para não quebrar o Inspector.
    private int xp = 0; // PlayerStats: idem, não é mais incrementado por aqui.
    public float velocidade = 5.0f; // PlayerStats: substituído por stats.Velocidade; campo mantido sem uso para não quebrar o Inspector.
    // Prefab do projétil, GameManager (se faltar, usa o singleton) e a ponta da arma de onde o tiro sai.
    public GameObject tiroPrefab;
    public GameManager gameManager;
    [SerializeField] private Transform gun;
    // Evita chamar o GameOver/desativar o player mais de uma vez.
    private bool morto = false;

    // PlayerStats: cache do componente de stats e controle de cadência de tiro.
    private PlayerStats stats;
    private float proximoTiro;

    void Awake()
    {
        // PlayerStats: pega o componente e começa com a vida cheia conforme o VidaMax atual.
        stats = GetComponent<PlayerStats>();
        if (stats == null)
        {
            // PlayerStats: rede de segurança para cenas salvas sem o componente (ex.: GameTeste).
            stats = gameObject.AddComponent<PlayerStats>();
        }

        vida = stats.VidaMax;
    }

    // A cada frame, com o jogo rodando, o player atira e se move.
    // Update is called once per frame
    void Update()
    {
        // Mesmo critério do resto do jogo: OnPlay com GameManager, e sempre ligado em cenas de teste sem ele.
        if (EstadoDoJogo.Rodando)
        {
            Shoot();
            Movimento();
        }
    }

    // Recebe dano (depois do filtro do escudo); se a vida zera, chama o Game Over e desativa o player.
    public void TakeDamage(int dano)
    {
        if (morto)
        {
            return;
        }

        // PlayerStats: escudo pode filtrar (zerar) o dano recebido.
        dano = stats.FiltrarDanoRecebido(dano);
        if (dano <= 0)
        {
            return;
        }

        vida -= dano;

        if (vida <= 0)
        {
            morto = true;

            GameManager gm = ObterGameManager();
            if (gm != null)
            {
                gm.SetGameState(GameManager.GameState.GameOver);
            }

            gameObject.SetActive(false);
        }
    }

    // gameManager pode não estar arrastado no Inspector; usa o singleton como fallback.
    private GameManager ObterGameManager()
    {
        return gameManager != null ? gameManager : GameManager.Instance;
    }

    private void Shoot()
    {
        // PlayerStats: atira enquanto o botão estiver pressionado, respeitando o intervalo
        // de tiro dos stats, e dispara o leque de projéteis via PadraoDeTiro.
        if(tiroPrefab != null && gun != null)
        {
            if(Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= proximoTiro)
            {
                PadraoDeTiro.Disparar(tiroPrefab, gun, stats, DirecaoDaMira());
                proximoTiro = Time.time + stats.IntervaloDeTiro;
            }
        }
    }

    // Da arma até o mouse, no mundo. O tiro sai nessa direção, qualquer que seja a rotação da Gun
    // no prefab (a nave antiga e o modelo 3D novo têm a arma girada de jeitos diferentes).
    private Vector2 DirecaoDaMira()
    {
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null)
        {
            return Vector2.zero;
        }

        Vector2 mouse = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return mouse - (Vector2)gun.position;
    }

    // PlayerStats: level/XP agora são responsabilidade do PlayerXp. Método mantido por compatibilidade.
    public void GanharXp (int xpGanho)
    {
        xp = xp + xpGanho;

        while (xp >= 100)
        {
            level = level + 1;
            xp = xp - 100;
        }
    }

    void Movimento()
    {
        // Junta o input dos dois eixos num vetor só e normaliza, assim andar
        // na diagonal não fica mais rápido do que andar reto.
        Vector2 direcao = Vector2.zero;

        // Sem teclado conectado o Keyboard.current é nulo.
        Keyboard teclado = Keyboard.current;
        if (teclado == null)
        {
            return;
        }

        if (teclado.wKey.isPressed)
        {
            direcao.y += 1;
        }
        if (teclado.sKey.isPressed)
        {
            direcao.y -= 1;
        }
        if (teclado.aKey.isPressed)
        {
            direcao.x -= 1;
        }
        if (teclado.dKey.isPressed)
        {
            direcao.x += 1;
        }

        // PlayerStats: velocidade agora vem dos stats (substitui o campo "velocidade").
        transform.position += (Vector3)(direcao.normalized * stats.Velocidade * Time.deltaTime);
    }
}
