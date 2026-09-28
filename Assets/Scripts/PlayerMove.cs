using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))] // PlayerStats: player sempre precisa dos stats
public class PlayerMove : MonoBehaviour, IDamageable
{
    public int vida = 100;
    public int level = 1; // PlayerStats: level/XP agora vivem em PlayerXp; campo mantido para não quebrar o Inspector.
    private int xp = 0; // PlayerStats: idem, não é mais incrementado por aqui.
    public float velocidade = 5.0f; // PlayerStats: substituído por stats.Velocidade; campo mantido sem uso para não quebrar o Inspector.
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
        vida = stats.VidaMax;
    }

    // Update is called once per frame
    void Update()
    {
        GameManager gm = ObterGameManager();
        if(gm != null && gm.gameState == GameManager.GameState.OnPlay)
        {
            Shoot();
            Movimento();
        }
    }

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
        Debug.Log(vida);

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
            if(Mouse.current.leftButton.isPressed && Time.time >= proximoTiro)
            {
                PadraoDeTiro.Disparar(tiroPrefab, gun, stats);
                proximoTiro = Time.time + stats.IntervaloDeTiro;
            }
        }
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

        if (Keyboard.current.wKey.isPressed)
        {
            direcao.y += 1;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            direcao.y -= 1;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            direcao.x -= 1;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            direcao.x += 1;
        }

        // PlayerStats: velocidade agora vem dos stats (substitui o campo "velocidade").
        transform.position += (Vector3)(direcao.normalized * stats.Velocidade * Time.deltaTime);
    }
}
