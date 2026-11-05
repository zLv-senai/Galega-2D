using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Entrada da nave ao iniciar a partida (só com o menu em roda): a nave real sai de trás do planeta da UI, à esquerda,
// e vai até a posição dela no mundo (o destino) enquanto o planeta sai pela esquerda. Só depois disso o jogo começa.
// Usa o tempo real, porque o menu roda com Time.timeScale = 0. O MenuManager cria este objeto e ele se destrói ao terminar.
public sealed class EntradaDaNave : MonoBehaviour
{
    private const float DuracaoDaNave = 1.2f;
    private const float InicioDaSaidaDoPlaneta = 0.8f;
    private const float DuracaoTotal = 1.4f;
    private const float EscalaInicial = 0.75f;
    private const float FracaoDaTelaNoInicio = 0.75f;   // a nave começa a 75% do meia-largura da tela à esquerda do destino
    private const string ClasseDeSaidaDoPlaneta = "roda-bola--saindo";

    // Chame com o planeta (BolaMenu) e o que fazer quando a nave chegar. Sem player, câmera ou CameraFollow, avisa e chama direto.
    public static void Iniciar(VisualElement planeta, Action aoTerminar)
    {
        EntradaDaNave entrada = new GameObject("EntradaDaNave").AddComponent<EntradaDaNave>();
        entrada.StartCoroutine(entrada.Rodar(planeta, aoTerminar));
    }

    private IEnumerator Rodar(VisualElement planeta, Action aoTerminar)
    {
        GameObject jogador = Jogador.Encontrar();
        Camera camera2D = Camera.main;
        CameraFollow seguidor = camera2D != null ? camera2D.GetComponent<CameraFollow>() : null;
        if (jogador == null || camera2D == null || seguidor == null)
        {
            Debug.LogWarning("EntradaDaNave: player, câmera ou CameraFollow não encontrados. A partida começa sem a animação.");
            Terminar(aoTerminar);
            yield break;
        }

        Transform nave = jogador.transform;
        Vector3 destino = nave.position;
        Vector3 escalaOriginal = nave.localScale;
        Vector3 inicio = destino + Vector3.left * (camera2D.orthographicSize * camera2D.aspect * FracaoDaTelaNoInicio);
        TrailRenderer[] rastros = jogador.GetComponentsInChildren<TrailRenderer>(true);

        // A câmera fica parada no destino (o CameraFollow a prenderia na nave) e a nave viaja com o nariz para a direita.
        seguidor.enabled = false;
        nave.rotation = Quaternion.identity;
        Posicionar(nave, inicio, escalaOriginal, EscalaInicial);

        bool planetaSaiu = false;
        float tempo = 0f;
        while (tempo < DuracaoTotal)
        {
            if (!planetaSaiu && tempo >= InicioDaSaidaDoPlaneta)
            {
                planetaSaiu = true;
                planeta?.AddToClassList(ClasseDeSaidaDoPlaneta);
            }

            float t = Mathf.Clamp01(tempo / DuracaoDaNave);
            float suave = 1f - Mathf.Pow(1f - t, 3f);   // ease-out cúbico
            Posicionar(nave, Vector3.Lerp(inicio, destino, suave), escalaOriginal, Mathf.Lerp(EscalaInicial, 1f, suave));

            yield return null;
            tempo += Time.unscaledDeltaTime;
        }

        nave.position = destino;
        nave.localScale = escalaOriginal;
        foreach (TrailRenderer rastro in rastros)
        {
            rastro.Clear();
        }

        seguidor.enabled = true;
        Terminar(aoTerminar);
    }

    private static void Posicionar(Transform nave, Vector3 posicao, Vector3 escalaOriginal, float fator)
    {
        nave.position = posicao;
        nave.localScale = escalaOriginal * fator;
    }

    private void Terminar(Action aoTerminar)
    {
        aoTerminar?.Invoke();
        Destroy(gameObject);
    }
}
