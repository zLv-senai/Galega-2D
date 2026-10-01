using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu "Galega > Montar Cena Final": cria Assets/Scenes/CenaFinal.unity a partir da
// "Scene integrada" e liga tudo que veio das branches do grupo:
// câmera seguindo o player (Samuel), parallax copiado da GameTeste (Samuel),
// spawn contínuo (Wagner) e sons (Eduardo). Pode rodar de novo: não duplica nada.
public static class MontarCenaFinal
{
    private const string CenaBase = "Assets/Scenes/Scene integrada.unity";
    private const string CenaParallax = "Assets/Scenes/Testes/GameTeste.unity";
    private const string CenaFinal = "Assets/Scenes/CenaFinal.unity";

    [MenuItem("Galega/Montar Cena Final")]
    public static void Montar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CenaFinal) == null
            && !AssetDatabase.CopyAsset(CenaBase, CenaFinal))
        {
            Debug.LogError("MontarCenaFinal: não consegui copiar " + CenaBase);
            return;
        }

        Scene final = EditorSceneManager.OpenScene(CenaFinal, OpenSceneMode.Single);

        PlayerMove player = Object.FindAnyObjectByType<PlayerMove>();
        Camera camera = Camera.main;

        if (player == null || camera == null)
        {
            Debug.LogError("MontarCenaFinal: a cena base precisa ter o Player (PlayerMove) e a Main Camera.");
            return;
        }

        LigarCameraFollow(camera, player.transform);
        LigarSpawn();
        LigarSons();
        CopiarParallax(final, camera.transform);
        ColocarNoBuild();

        EditorSceneManager.MarkSceneDirty(final);
        EditorSceneManager.SaveScene(final);
        Debug.Log("MontarCenaFinal: pronto! " + CenaFinal + " salva e colocada em 1º nas Build Settings.");
    }

    private static void LigarCameraFollow(Camera camera, Transform player)
    {
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<CameraFollow>();
        }

        DefinirCampo(follow, "player", player);
    }

    private static void LigarSpawn()
    {
        EnemySpawn spawn = Object.FindAnyObjectByType<EnemySpawn>();
        if (spawn == null)
        {
            spawn = new GameObject("Spawner").AddComponent<EnemySpawn>();
        }

        if (spawn.GetComponent<SpawnContinuo>() == null)
        {
            spawn.gameObject.AddComponent<SpawnContinuo>();
        }
    }

    private static void LigarSons()
    {
        GerenciadorDeSom som = Object.FindAnyObjectByType<GerenciadorDeSom>();
        if (som == null)
        {
            som = new GameObject("GerenciadorDeSom").AddComponent<GerenciadorDeSom>();
        }

        // Sons da pasta do Eduardo (Assets/Sons). Troque no Inspector se preferir outro.
        DefinirClip(som, "tiroPlayer", "Assets/Sons/SomDeTiro.mp3");
        DefinirClip(som, "tiroInimigo", "Assets/Sons/Tiro_Inimigo.wav");
        DefinirClip(som, "explosaoInimigo", "Assets/Sons/Explosão_Inimigo.wav");
        DefinirClip(som, "levelUp", "Assets/Sons/blipSelect.wav");
        DefinirClip(som, "powerUp", "Assets/Sons/Nova pasta/Som_Power_UP.wav");
        DefinirClip(som, "gameOver", "Assets/Sons/Nova pasta/Som_GameOver_Principal.wav");
        DefinirClip(som, "musicaDeFundo", "Assets/Sons/Som Musica de Fundo/Som_De-Fundo.wav");
    }

    // Abre a GameTeste junto, copia os objetos de fundo que têm ParallaxLayer e fecha sem salvar.
    private static void CopiarParallax(Scene final, Transform cameraFinal)
    {
        if (Object.FindAnyObjectByType<ParallaxLayer>() != null)
        {
            Debug.Log("MontarCenaFinal: a cena já tem parallax, não copiei de novo.");
            return;
        }

        Scene origem = EditorSceneManager.OpenScene(CenaParallax, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(final);

        List<GameObject> copiados = new List<GameObject>();
        foreach (GameObject raiz in origem.GetRootGameObjects())
        {
            if (raiz.GetComponentsInChildren<ParallaxLayer>(true).Length == 0 || TemGameplay(raiz))
            {
                continue;
            }

            GameObject copia = Object.Instantiate(raiz);
            copia.name = raiz.name;
            SceneManager.MoveGameObjectToScene(copia, final);
            copiados.Add(copia);
        }

        EditorSceneManager.CloseScene(origem, true);

        foreach (GameObject copia in copiados)
        {
            foreach (ParallaxLayer camada in copia.GetComponentsInChildren<ParallaxLayer>(true))
            {
                DefinirCampo(camada, "cameraTransform", cameraFinal);
            }
        }

        Debug.Log("MontarCenaFinal: " + copiados.Count + " objeto(s) de parallax copiado(s) da GameTeste.");
    }

    // Não copia objetos que carregam gameplay junto (player, câmera, inimigos, boss, GameManager).
    private static bool TemGameplay(GameObject raiz)
    {
        return raiz.GetComponentInChildren<PlayerMove>(true) != null
            || raiz.GetComponentInChildren<Camera>(true) != null
            || raiz.GetComponentInChildren<EnemyMove>(true) != null
            || raiz.GetComponentInChildren<BossController>(true) != null
            || raiz.GetComponentInChildren<GameManager>(true) != null;
    }

    private static void ColocarNoBuild()
    {
        List<EditorBuildSettingsScene> cenas = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(CenaFinal, true)
        };

        foreach (EditorBuildSettingsScene cena in EditorBuildSettings.scenes)
        {
            if (cena.path != CenaFinal)
            {
                cenas.Add(cena);
            }
        }

        EditorBuildSettings.scenes = cenas.ToArray();
    }

    private static void DefinirClip(Object alvo, string campo, string caminho)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
        if (clip == null)
        {
            Debug.LogWarning("MontarCenaFinal: som não encontrado: " + caminho + " (campo " + campo + ").");
            return;
        }

        DefinirCampo(alvo, campo, clip);
    }

    // Preenche um campo [SerializeField] privado, como se fosse arrastado no Inspector.
    private static void DefinirCampo(Object alvo, string campo, Object valor)
    {
        SerializedObject so = new SerializedObject(alvo);
        SerializedProperty prop = so.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogWarning("MontarCenaFinal: campo '" + campo + "' não existe em " + alvo.GetType().Name);
            return;
        }

        prop.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
