using System.Collections.Generic;
using UnityEngine;

// Listas de modelos (prefabs) para sortear: os inimigos no EnemySpawn e os bosses no ControladorDeBoss.
// Os modelos vêm do Inspector; se o campo ficar vazio, são carregados de Assets/Resources pelos nomes.
public static class SorteioDeModelos
{
    // Junta os prefabs válidos: os do Inspector (ignorando espaços vazios) ou, se não houver nenhum,
    // os de Assets/Resources com esses nomes. Avisa no Console cada nome que não existe no Resources.
    // Pode devolver uma lista vazia: quem chama decide o que fazer sem modelos.
    public static GameObject[] Carregar(GameObject[] doInspector, string[] nomesNoResources, Object contexto)
    {
        List<GameObject> modelos = new List<GameObject>();

        if (doInspector != null)
        {
            foreach (GameObject modelo in doInspector)
            {
                if (modelo != null)
                {
                    modelos.Add(modelo);
                }
            }
        }

        if (modelos.Count > 0)
        {
            return modelos.ToArray();
        }

        foreach (string nome in nomesNoResources)
        {
            GameObject modelo = Resources.Load<GameObject>(nome);
            if (modelo != null)
            {
                modelos.Add(modelo);
            }
            else
            {
                Debug.LogWarning("SorteioDeModelos: não achei '" + nome + "' em Assets/Resources; ele fica fora do sorteio.", contexto);
            }
        }

        return modelos.ToArray();
    }

    // Índice aleatório de 0 a quantidade - 1, diferente de "anterior" quando há mais de uma opção
    // (assim o mesmo modelo não sai duas vezes seguidas). anterior < 0 = ainda não saiu nenhum: vale qualquer um.
    public static int SortearSemRepetir(int quantidade, int anterior)
    {
        if (quantidade <= 1)
        {
            return 0;
        }

        if (anterior < 0 || anterior >= quantidade)
        {
            return Random.Range(0, quantidade);
        }

        // Sorteia entre as outras opções (uma a menos) e pula o índice anterior.
        int sorteado = Random.Range(0, quantidade - 1);
        return sorteado >= anterior ? sorteado + 1 : sorteado;
    }
}
