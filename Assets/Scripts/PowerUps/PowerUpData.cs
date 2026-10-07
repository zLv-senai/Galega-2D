using UnityEngine;

// Um power-up TEMPORÁRIO (solto pelos baús). Cada power-up é um asset (Create > Galega > Power-up).
// Diferente do card, o efeito dura "duracao" segundos e depois é removido.
[CreateAssetMenu(fileName = "PowerUp", menuName = "Galega/Power-up")]
public class PowerUpData : ScriptableObject
{
    // Nome mostrado no HUD ao pegar o power-up. O ícone ainda não é lido por nenhum script.
    public string nome;

    public Sprite icone;

    // Cor aplicada no sprite do pickup para diferenciar um power-up do outro.
    public Color cor = Color.white;

    // Quanto tempo o efeito dura, em segundos de jogo (não conta com o jogo pausado).
    public float duracao = 8f;

    // O que o power-up muda nos stats enquanto está ativo (fonte do modificador = este asset).
    public ModificadorDeStat[] modificadores;

    // Se ao pegar ele enche o escudo (PlayerStats.RecarregarEscudo). Não depende da duração.
    public bool recarregaEscudo;

    // Cura ao pegar: fração da vida máxima (0.1 = 10%), sem passar do máximo. 0 = não cura. Não depende da duração.
    [Range(0f, 1f)] public float curaPercentualVidaMax;

    // Se true, só sai no sorteio do modo Infinito (TabelaDePowerUps.Sortear). Ex.: Reparo.
    public bool somenteNoInfinito;

    // Peso no sorteio da TabelaDePowerUps: quanto maior, mais comum. Peso <= 0 nunca sai.
    public int peso = 10;
}
