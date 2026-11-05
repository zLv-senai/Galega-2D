using UnityEngine;

// Explosões fora dos inimigos comuns (morte do player e do boss). Usa o mesmo prefab dos inimigos, já em Resources.
public static class EfeitoDeExplosao
{
    private const string PrefabNoResources = "Explosion";
    private const float SegundosAteApagar = 3f;

    private static GameObject prefab;

    // Anda em tempo real: a morte do player e as cartas pós-boss congelam o jogo (timeScale 0), e as partículas ficariam paradas.
    public static void Criar(Vector3 posicao, float fatorDeTamanho)
    {
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>(PrefabNoResources);
        }

        if (prefab == null)
        {
            Debug.LogWarning("EfeitoDeExplosao: prefab '" + PrefabNoResources + "' não encontrado em Resources.");
            return;
        }

        GameObject explosao = Object.Instantiate(prefab, posicao, Quaternion.identity);
        foreach (ParticleSystem particulas in explosao.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule principal = particulas.main;
            principal.useUnscaledTime = true;
            principal.startSizeMultiplier *= fatorDeTamanho;
        }

        Object.Destroy(explosao, SegundosAteApagar);
    }
}
