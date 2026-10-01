using UnityEngine;

// Um card de level up: buff PERMANENTE que o jogador escolhe ao subir de level.
// Cada card é um asset (Create > Galega > Card). Os cards acumulam: escolher o mesmo
// card de novo aplica os modificadores de novo.
[CreateAssetMenu(fileName = "Card", menuName = "Galega/Card")]
public class CardData : ScriptableObject
{
    // Informações mostradas na tela de escolha (nome, texto e ícone); a raridade também define a chance de sair no sorteio.
    public string nome;

    [TextArea] public string descricao;

    public Sprite icone;

    public Raridade raridade;

    // O que o card muda nos stats do player (fonte do modificador = este card).
    public ModificadorDeStat[] modificadores;

    // Quantas vezes o jogador pode escolher este card (0 = sem limite).
    public int maxEscolhas = 0;
}
