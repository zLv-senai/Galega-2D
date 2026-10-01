// Contrato de tudo que pode levar dano (player, inimigos, boss, baú): o Shot e o DanoPorContato
// chamam o TakeDamage.
public interface IDamageable
{
    // Tira 'dano' da vida de quem implementa; cada classe decide o que acontece ao zerar.
    void TakeDamage(int dano);
}