using UnityEngine;

// Toca os sons do jogo ouvindo os eventos que já existem (nenhum script de gameplay
// precisa saber dele). Os clipes (pasta Assets/Sons, do Eduardo) são arrastados no Inspector.
public class GerenciadorDeSom : MonoBehaviour
{
    // Clipes dos efeitos (tiros, explosão, level up, power up, game over); clipe vazio = esse som não toca.
    [Header("Efeitos")]
    [SerializeField] private AudioClip tiroPlayer;
    [SerializeField] private AudioClip tiroInimigo;
    [SerializeField] private AudioClip explosaoInimigo;
    [SerializeField] private AudioClip levelUp;
    // Integração (waves): som de wave limpa. Opcional: vazio = toca o mesmo som do levelUp.
    [SerializeField] private AudioClip waveLimpa;
    [SerializeField] private AudioClip powerUp;
    [SerializeField] private AudioClip gameOver;

    // Integração (boss): alerta quando a ameaça enche, explosão quando um boss morre e música de vitória.
    [Header("Boss")]
    [SerializeField] private AudioClip alertaBoss;
    [SerializeField] private AudioClip explosaoBoss;
    [SerializeField] private AudioClip vitoria;

    // Música de fundo (toca em loop; para no Game Over e na Vitória).
    [Header("Música")]
    [SerializeField] private AudioClip musicaDeFundo;

    // Volumes base do Inspector; o volume final é este valor vezes o slider do Settings (Efeitos ou Música).
    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float volumeEfeitos = 0.6f;
    [SerializeField, Range(0f, 1f)] private float volumeTiroInimigo = 0.3f;
    [SerializeField, Range(0f, 1f)] private float volumeMusica = 0.35f;

    // Com muitos inimigos atirando juntos, o som vira ruído: limita a 1 por intervalo.
    [SerializeField] private float intervaloMinimoTiroInimigo = 0.08f;

    // As duas AudioSources (efeitos e música) e a hora liberada para o próximo som de tiro inimigo.
    private AudioSource fonteEfeitos;
    private AudioSource fonteMusica;
    private float proximoTiroInimigo;

    // Player e power-ups cujos eventos este script ouve (achados no Start).
    private PlayerXp playerXp;
    private PlayerPowerUps playerPowerUps;

    // Cria as duas AudioSources (efeitos e música em loop) e aplica os volumes do Settings.
    private void Awake()
    {
        fonteEfeitos = gameObject.AddComponent<AudioSource>();
        fonteEfeitos.playOnAwake = false;

        fonteMusica = gameObject.AddComponent<AudioSource>();
        fonteMusica.playOnAwake = false;
        fonteMusica.loop = true;

        // Integração (Settings): aplica o volume Geral e o da música escolhidos no menu.
        ConfiguracaoDeAudio.Aplicar();
        AtualizarVolumeMusica();
    }

    // Integração (Settings): volume final da música = o do Inspector x o slider "Música".
    private void AtualizarVolumeMusica()
    {
        if (fonteMusica != null)
        {
            fonteMusica.volume = volumeMusica * ConfiguracaoDeAudio.Musica;
        }
    }

    // Ao ativar, assina os eventos do jogo (tiros, mortes, boss, wave limpa, estado) e o de mudança de volume.
    private void OnEnable()
    {
        ConfiguracaoDeAudio.AoMudar += AtualizarVolumeMusica;
        AtualizarVolumeMusica();

        PadraoDeTiro.AoDisparar += TocarTiroPlayer;
        EnemyMove.AoAtirar += TocarTiroInimigo;
        EnemyMove.AoMorrer += TocarExplosaoInimigo;
        ControladorDeBoss.AoAlerta += TocarAlertaBoss;
        BossController.AoMorrer += TocarExplosaoBoss;
        GerenciadorDeWaves.AoWaveLimpa += TocarWaveLimpa;
        GameManager.AoMudarEstado += TratarMudancaDeEstado;
    }

    // Cancela as assinaturas (inclusive as do Player feitas no Start): os eventos estáticos sobrevivem a este objeto.
    private void OnDisable()
    {
        ConfiguracaoDeAudio.AoMudar -= AtualizarVolumeMusica;

        PadraoDeTiro.AoDisparar -= TocarTiroPlayer;
        EnemyMove.AoAtirar -= TocarTiroInimigo;
        EnemyMove.AoMorrer -= TocarExplosaoInimigo;
        ControladorDeBoss.AoAlerta -= TocarAlertaBoss;
        BossController.AoMorrer -= TocarExplosaoBoss;
        GerenciadorDeWaves.AoWaveLimpa -= TocarWaveLimpa;
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

    // Acha o Player para ouvir level up e power up, e começa a tocar a música de fundo.
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

    // Toca um efeito uma vez com o volume dado; ignora se o clipe estiver vazio.
    private void Tocar(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            // Integração (Settings): o slider "Efeitos" multiplica o volume de todo efeito.
            fonteEfeitos.PlayOneShot(clip, volume * ConfiguracaoDeAudio.Efeitos);
        }
    }

    // Som do tiro do player.
    private void TocarTiroPlayer()
    {
        Tocar(tiroPlayer, volumeEfeitos);
    }

    // Som do tiro de inimigo, limitado a um por intervalo para não virar ruído.
    private void TocarTiroInimigo(EnemyMove inimigo)
    {
        if (Time.unscaledTime < proximoTiroInimigo)
        {
            return;
        }

        proximoTiroInimigo = Time.unscaledTime + intervaloMinimoTiroInimigo;
        Tocar(tiroInimigo, volumeTiroInimigo);
    }

    // Som de explosão quando um inimigo morre.
    private void TocarExplosaoInimigo(EnemyMove inimigo)
    {
        Tocar(explosaoInimigo, volumeEfeitos);
    }

    // Som de alerta quando a ameaça do boss enche.
    private void TocarAlertaBoss(int indiceBoss, bool ehFinal)
    {
        Tocar(alertaBoss, volumeEfeitos);
    }

    // Som de explosão quando um boss morre.
    private void TocarExplosaoBoss(BossController boss)
    {
        Tocar(explosaoBoss, volumeEfeitos);
    }

    // Som de level up do player.
    private void TocarLevelUp(int level)
    {
        Tocar(levelUp, volumeEfeitos);
    }

    // Som de wave limpa (usa o do level up se não houver clipe próprio).
    private void TocarWaveLimpa(int wave)
    {
        Tocar(waveLimpa != null ? waveLimpa : levelUp, volumeEfeitos);
    }

    // Som ao ativar um power-up.
    private void TocarPowerUp(PowerUpData dados)
    {
        Tocar(powerUp, volumeEfeitos);
    }

    // GameOver e Vitória cortam música e efeitos e tocam o som de fim; voltar a OnPlay retoma a música se ela parou.
    private void TratarMudancaDeEstado(GameManager.GameState estado)
    {
        if (estado == GameManager.GameState.GameOver)
        {
            fonteMusica.Stop();
            // Corta efeitos em andamento (explosão do boss, wave limpa) para o som de fim tocar limpo.
            fonteEfeitos.Stop();
            Tocar(gameOver, volumeEfeitos);
        }
        else if (estado == GameManager.GameState.Vitoria)
        {
            // Integração (boss): vitória para a música de fundo e toca o som de vitória.
            fonteMusica.Stop();
            // Corta efeitos em andamento (explosão do boss, wave limpa) para o som de vitória tocar limpo.
            fonteEfeitos.Stop();
            Tocar(vitoria, volumeEfeitos);
        }
        else if (estado == GameManager.GameState.OnPlay && musicaDeFundo != null && !fonteMusica.isPlaying)
        {
            fonteMusica.Play();
        }
    }
}
