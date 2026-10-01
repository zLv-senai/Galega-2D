using UnityEngine;

// Toca os sons do jogo ouvindo os eventos que já existem (nenhum script de gameplay
// precisa saber dele). Os clipes (pasta Assets/Sons, do Eduardo) são arrastados no Inspector.
public class GerenciadorDeSom : MonoBehaviour
{
    [Header("Efeitos")]
    [SerializeField] private AudioClip tiroPlayer;
    [SerializeField] private AudioClip tiroInimigo;
    [SerializeField] private AudioClip explosaoInimigo;
    [SerializeField] private AudioClip levelUp;
    [SerializeField] private AudioClip powerUp;
    [SerializeField] private AudioClip gameOver;

    [Header("Música")]
    [SerializeField] private AudioClip musicaDeFundo;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float volumeEfeitos = 0.6f;
    [SerializeField, Range(0f, 1f)] private float volumeTiroInimigo = 0.3f;
    [SerializeField, Range(0f, 1f)] private float volumeMusica = 0.35f;

    // Com muitos inimigos atirando juntos, o som vira ruído: limita a 1 por intervalo.
    [SerializeField] private float intervaloMinimoTiroInimigo = 0.08f;

    private AudioSource fonteEfeitos;
    private AudioSource fonteMusica;
    private float proximoTiroInimigo;

    private PlayerXp playerXp;
    private PlayerPowerUps playerPowerUps;

    private void Awake()
    {
        fonteEfeitos = gameObject.AddComponent<AudioSource>();
        fonteEfeitos.playOnAwake = false;

        fonteMusica = gameObject.AddComponent<AudioSource>();
        fonteMusica.playOnAwake = false;
        fonteMusica.loop = true;
        fonteMusica.volume = volumeMusica;
    }

    private void OnEnable()
    {
        PadraoDeTiro.AoDisparar += TocarTiroPlayer;
        EnemyMove.AoAtirar += TocarTiroInimigo;
        EnemyMove.AoMorrer += TocarExplosaoInimigo;
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
    }

    private void OnDisable()
    {
        PadraoDeTiro.AoDisparar -= TocarTiroPlayer;
        EnemyMove.AoAtirar -= TocarTiroInimigo;
        EnemyMove.AoMorrer -= TocarExplosaoInimigo;
        GameManager.AoMudarEstado -= TratarMudancaDeEstado;

        if (playerXp != null)
        {
            playerXp.AoSubirDeLevel -= TocarLevelUp;
        }

        if (playerPowerUps != null)
        {
            playerPowerUps.AoAtivar -= TocarPowerUp;
        }
    }

    private void Start()
    {
        // Eventos de instância ficam no Player: procura pelo PlayerMove (a tag pode faltar).
        PlayerMove jogador = FindAnyObjectByType<PlayerMove>();
        if (jogador != null)
        {
            playerXp = jogador.GetComponent<PlayerXp>();
            playerPowerUps = jogador.GetComponent<PlayerPowerUps>();
        }

        if (playerXp != null)
        {
            playerXp.AoSubirDeLevel += TocarLevelUp;
        }

        if (playerPowerUps != null)
        {
            playerPowerUps.AoAtivar += TocarPowerUp;
        }

        if (musicaDeFundo != null)
        {
            fonteMusica.clip = musicaDeFundo;
            fonteMusica.Play();
        }
    }

    private void Tocar(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            fonteEfeitos.PlayOneShot(clip, volume);
        }
    }

    private void TocarTiroPlayer()
    {
        Tocar(tiroPlayer, volumeEfeitos);
    }

    private void TocarTiroInimigo(EnemyMove inimigo)
    {
        if (Time.unscaledTime < proximoTiroInimigo)
        {
            return;
        }

        proximoTiroInimigo = Time.unscaledTime + intervaloMinimoTiroInimigo;
        Tocar(tiroInimigo, volumeTiroInimigo);
    }

    private void TocarExplosaoInimigo(EnemyMove inimigo)
    {
        Tocar(explosaoInimigo, volumeEfeitos);
    }

    private void TocarLevelUp(int level)
    {
        Tocar(levelUp, volumeEfeitos);
    }

    private void TocarPowerUp(PowerUpData dados)
    {
        Tocar(powerUp, volumeEfeitos);
    }

    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.GameOver)
        {
            fonteMusica.Stop();
            Tocar(gameOver, volumeEfeitos);
        }
        else if (estado == GameManager.GameState.OnPlay && musicaDeFundo != null && !fonteMusica.isPlaying)
        {
            fonteMusica.Play();
        }
    }
}
