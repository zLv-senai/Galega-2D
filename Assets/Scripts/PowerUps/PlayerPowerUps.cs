using System.Collections.Generic;
using UnityEngine;

// No Player: controla os power-ups temporários ativos. Ativar aplica os modificadores nos stats
// (com o próprio PowerUpData como fonte) e, quando o tempo acaba, remove só os dele.
[RequireComponent(typeof(PlayerStats))]
public class PlayerPowerUps : MonoBehaviour
{
    // Quando um power-up é pego (inclusive renovação e escudo). Para sons, HUD, etc.
    public event System.Action<PowerUpData> AoAtivar;

    // Quando o tempo de um power-up acaba e os modificadores dele são removidos.
    public event System.Action<PowerUpData> AoExpirar;

    // PlayerStats que recebe e perde os modificadores dos power-ups.
    private PlayerStats stats;

    // PlayerMove do mesmo Player (onde fica a vida atual), para os power-ups de cura. Pode faltar.
    private PlayerMove playerMove;

    // Power-ups ativos e o Time.time em que cada um expira.
    private readonly Dictionary<PowerUpData, float> expiraEm = new Dictionary<PowerUpData, float>();

    // Lista reaproveitada no Update, para não alocar memória todo frame.
    private readonly List<PowerUpData> expirados = new List<PowerUpData>();

    // Somente leitura: power-ups ativos (chave) e o Time.time em que expiram (valor).
    // Para o tempo que falta, use TempoRestante.
    public IReadOnlyDictionary<PowerUpData, float> ExpiraEm => expiraEm;

    // Guarda o PlayerStats e o PlayerMove do Player.
    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        playerMove = GetComponent<PlayerMove>();
    }

    // Segundos que faltam para o power-up acabar (0 se não está ativo). Útil para o HUD.
    public float TempoRestante(PowerUpData dados)
    {
        if (dados != null && expiraEm.TryGetValue(dados, out float fim))
        {
            return Mathf.Max(0f, fim - Time.time);
        }

        return 0f;
    }

    // Ativa um power-up: recarrega o escudo se o dado pedir e soma os modificadores por 'duracao'
    // (se já estava ativo, só renova o tempo). No fim dispara o evento AoAtivar.
    public void Ativar(PowerUpData dados)
    {
        if (dados == null)
        {
            return;
        }

        if (dados.recarregaEscudo)
        {
            stats.RecarregarEscudo();
        }

        if (dados.curaPercentualVidaMax > 0f)
        {
            Curar(dados.curaPercentualVidaMax);
        }

        bool temModificadores = dados.modificadores != null && dados.modificadores.Length > 0;

        if (temModificadores && dados.duracao <= 0f)
        {
            Debug.LogWarning("PlayerPowerUps: '" + dados.name + "' tem modificadores mas duração 0, então o efeito não foi aplicado.");
        }
        else if (temModificadores)
        {
            // Já ativo: só renova o tempo. Somar de novo empilharia o efeito e o Remover tiraria tudo de uma vez.
            bool jaAtivo = expiraEm.ContainsKey(dados);
            expiraEm[dados] = Time.time + dados.duracao;

            if (!jaAtivo)
            {
                stats.AdicionarModificadores(dados.modificadores, dados);
            }
        }

        AoAtivar?.Invoke(dados);
    }

    // Cura uma fração da vida máxima (arredondada para cima, no mínimo 1), sem passar do máximo.
    // Mesmo jeito de curar do BonusPassivoDeLevel.
    private void Curar(float fracaoDaVidaMax)
    {
        if (playerMove == null)
        {
            Debug.LogWarning("PlayerPowerUps: o Player não tem PlayerMove, então o power-up de cura não curou.", this);
            return;
        }

        int cura = Mathf.Max(1, Mathf.CeilToInt(stats.VidaMax * fracaoDaVidaMax));
        playerMove.vida = Mathf.Min(stats.VidaMax, playerMove.vida + cura);
    }

    // Com o jogo rodando, remove os power-ups cujo tempo acabou e dispara o evento AoExpirar.
    private void Update()
    {
        // Time.time não anda com timeScale = 0, mas o estado também precisa estar rodando
        // (ex.: menu/game over sem timeScale 0).
        if (expiraEm.Count == 0 || !EstadoDoJogo.Rodando)
        {
            return;
        }

        expirados.Clear();
        foreach (KeyValuePair<PowerUpData, float> par in expiraEm)
        {
            if (Time.time >= par.Value)
            {
                expirados.Add(par.Key);
            }
        }

        foreach (PowerUpData dados in expirados)
        {
            expiraEm.Remove(dados);
            stats.RemoverModificadoresDaFonte(dados);
            AoExpirar?.Invoke(dados);
        }
    }
}
