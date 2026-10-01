using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Menu "Galega > Montar Cena Final": cria Assets/Scenes/CenaFinal.unity a partir da
// "Scene integrada" e liga tudo que veio das branches do grupo:
// câmera seguindo o player (Samuel), parallax copiado da GameTeste (Samuel),
// spawn contínuo (Wagner), sons (Eduardo), bosses (ControladorDeBoss + prefab Resources/Boss) e a
// tela de vitória (VitoriaUI). Pode rodar de novo: não duplica nada.
public static class MontarCenaFinal
{
    private const string CenaBase = "Assets/Scenes/Scene integrada.unity";
    private const string CenaFinal = "Assets/Scenes/CenaFinal.unity";

    private const string CaminhoFundo = "Assets/Images/FUNDOJOGO.png";
    private const string NomeFundo = "Fundo";
    private const int OrdemFundo = -100;              // desenha atrás de tudo
    private const float FatorParallaxFundo = 0.95f;   // perto de 1 = acompanha a câmera = parece bem longe

    // Bosses e vitória
    private const string NomeControladorBoss = "ControladorDeBoss";
    private const string NomeTelaVitoria = "VitoriaUI";
    private const string CaminhoUxmlVitoria = "Assets/UI/Vitoria.uxml";
    private const string CaminhoPanelSettings = "Assets/UI Toolkit/PanelSettings.asset"; // reserva, se não achar o do GameOverUI
    private const int OrdemTelaVitoria = 20;            // mesma ordem da tela de Game Over (acima do HUD)
    private static readonly string[] CaminhosGema = { "Assets/Prefabs/GemsXp.prefab", "Assets/Resources/GemsXp.prefab" };

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
        LigarBoss();
        LigarTelaDeVitoria();
        MontarFundo(camera);
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

        // Boss: alerta (blip de menu), explosão do boss e música de vitória.
        DefinirClip(som, "alertaBoss", "Assets/Sons/blipSelect.wav");
        DefinirClip(som, "explosaoBoss", "Assets/Sons/explosao_boss.wav");
        DefinirClip(som, "vitoria", "Assets/Sons/Nova pasta/Som_Vitoria_Principal.mp3");
    }

    // Objeto "ControladorDeBoss" com o prefab do boss e a gema que ele solta. Cria o prefab do boss se ainda não existir.
    private static void LigarBoss()
    {
        ControladorDeBoss controlador = Object.FindAnyObjectByType<ControladorDeBoss>();
        if (controlador == null)
        {
            controlador = new GameObject(NomeControladorBoss).AddComponent<ControladorDeBoss>();
        }

        GameObject bossPrefab = CriarPrefabBoss.GarantirPrefab();
        if (bossPrefab != null)
        {
            DefinirCampo(controlador, "bossPrefab", bossPrefab);
        }
        else
        {
            Debug.LogWarning("MontarCenaFinal: sem prefab do boss. Rode Galega > Criar Prefab do Boss e monte a cena de novo.");
        }

        GameObject gema = CarregarGema();
        if (gema != null)
        {
            DefinirCampo(controlador, "gemaPrefab", gema);
        }
        else
        {
            Debug.LogWarning("MontarCenaFinal: não achei o prefab GemsXp. O boss vai tentar Resources/GemsXp ao morrer.");
        }
    }

    private static GameObject CarregarGema()
    {
        foreach (string caminho in CaminhosGema)
        {
            GameObject gema = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (gema != null)
            {
                return gema;
            }
        }

        return null;
    }

    // Objeto "VitoriaUI" (VitoriaUI + PanelRenderer com o Vitoria.uxml), usando o mesmo PanelSettings do GameOverUI.
    private static void LigarTelaDeVitoria()
    {
        VitoriaUI tela = Object.FindAnyObjectByType<VitoriaUI>();
        if (tela == null)
        {
            GameObject objeto = new GameObject(NomeTelaVitoria);
            objeto.AddComponent<PanelRenderer>();
            tela = objeto.AddComponent<VitoriaUI>();
        }

        PanelRenderer painel = tela.GetComponent<PanelRenderer>();
        if (painel == null)
        {
            painel = tela.gameObject.AddComponent<PanelRenderer>();
        }

        PanelSettings settings = AcharPanelSettings();
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CaminhoUxmlVitoria);
        if (settings == null || uxml == null)
        {
            Debug.LogWarning("MontarCenaFinal: não consegui ligar a tela de vitória (PanelSettings ou " + CaminhoUxmlVitoria + " não encontrado). Confira o PanelRenderer do objeto '" + NomeTelaVitoria + "'.");
        }

        // Nomes dos campos serializados do PanelRenderer (iguais aos que aparecem na cena salva).
        DefinirCampo(painel, "m_PanelSettings", settings);
        DefinirCampo(painel, "sourceAsset", uxml);
        painel.sortingOrder = OrdemTelaVitoria;
    }

    // O PanelSettings que o GameOverUI da cena já usa; se não houver, o padrão do projeto.
    private static PanelSettings AcharPanelSettings()
    {
        GameOverUI gameOver = Object.FindAnyObjectByType<GameOverUI>();
        if (gameOver != null)
        {
            PanelRenderer painelGameOver = gameOver.GetComponent<PanelRenderer>();
            if (painelGameOver != null)
            {
                SerializedProperty prop = new SerializedObject(painelGameOver).FindProperty("m_PanelSettings");
                if (prop != null && prop.objectReferenceValue is PanelSettings doGameOver)
                {
                    return doGameOver;
                }
            }
        }

        return AssetDatabase.LoadAssetAtPath<PanelSettings>(CaminhoPanelSettings);
    }

    // Fundo espacial com o ParallaxLayer do Samuel, usando o FUNDOJOGO.png do projeto.
    // Antes apaga as camadas provisórias com o sprite tiro-boss2 (copiadas da GameTeste
    // numa versão anterior deste menu), que pareciam inimigos andando junto com o player.
    private static void MontarFundo(Camera camera)
    {
        foreach (ParallaxLayer camada in Object.FindObjectsByType<ParallaxLayer>())
        {
            SpriteRenderer sr = camada.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null && sr.sprite.name.StartsWith("tiro-boss2"))
            {
                Object.DestroyImmediate(camada.gameObject);
            }
        }

        if (GameObject.Find(NomeFundo) != null)
        {
            Debug.Log("MontarCenaFinal: o objeto '" + NomeFundo + "' já existe, não criei de novo.");
            return;
        }

        Sprite sprite = CarregarSprite(CaminhoFundo);
        if (sprite == null)
        {
            Debug.LogWarning("MontarCenaFinal: não achei um Sprite em " + CaminhoFundo + ". Confira se o Texture Type é Sprite (2D and UI).");
            return;
        }

        GameObject fundo = new GameObject(NomeFundo);
        Vector3 posCamera = camera.transform.position;
        fundo.transform.position = new Vector3(posCamera.x, posCamera.y, 0f);

        SpriteRenderer render = fundo.AddComponent<SpriteRenderer>();
        render.sprite = sprite;
        render.sortingOrder = OrdemFundo;

        // Escala para cobrir ~3x a área da câmera (sobra para o player andar).
        float alturaVisao = camera.orthographicSize * 2f;
        float larguraVisao = alturaVisao * camera.aspect;
        Vector2 tamanhoSprite = sprite.bounds.size;
        float escala = Mathf.Max(larguraVisao * 3f / tamanhoSprite.x, alturaVisao * 3f / tamanhoSprite.y);
        fundo.transform.localScale = new Vector3(escala, escala, 1f);

        ParallaxLayer parallax = fundo.AddComponent<ParallaxLayer>();
        DefinirCampo(parallax, "cameraTransform", camera.transform);
        DefinirFloat(parallax, "parallax", FatorParallaxFundo);
    }

    private static Sprite CarregarSprite(string caminho)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(caminho))
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void DefinirFloat(Object alvo, string campo, float valor)
    {
        SerializedObject so = new SerializedObject(alvo);
        SerializedProperty prop = so.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogWarning("MontarCenaFinal: campo '" + campo + "' não existe em " + alvo.GetType().Name);
            return;
        }

        prop.floatValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
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
