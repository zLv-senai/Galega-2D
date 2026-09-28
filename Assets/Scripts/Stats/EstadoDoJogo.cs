// Ponto único para checar se o jogo está rodando (não pausado, não no menu, não em game over).
// Toda lógica nova de gameplay deve checar EstadoDoJogo.Rodando antes de agir.
public static class EstadoDoJogo
{
    public static bool Rodando
    {
        get
        {
            if (GameManager.Instance == null)
            {
                // Sem GameManager na cena (ex.: cena de teste isolada): não trava os testes.
                return true;
            }

            return GameManager.Instance.gameState == GameManager.GameState.OnPlay;
        }
    }
}
