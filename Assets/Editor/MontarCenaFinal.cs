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
// tela de vitória (VitoriaUI), o menu de pausa (MenuDePausa) e as waves (GerenciadorDeWaves + BonusPassivoDeLevel
// no Player; o SpawnContinuo fica na cena mas desligado) e a seta do boss fora da tela (SetaDoBoss no Hud).
// Pode rodar de novo: não duplica nada.
public static class MontarCenaFinal
{
    // Cena de origem (só é copiada se a CenaFinal ainda não existir) e a cena final que este menu monta.
    private const string CenaBase = "Assets/Scenes/Scene integrada.unity";
    private const string CenaFinal = "Assets/Scenes/CenaFinal.unity";

    // Imagem, nome do objeto, ordem de desenho e fator de parallax do fundo espacial.
    private const string CaminhoFundo = "Assets/Images/FUNDOJOGO.png";
    private const string NomeFundo = "Fundo";
    private const int OrdemFundo = -100;              // desenha atrás de tudo
    private const float FatorParallaxFundo = 0.95f;   // perto de 1 = acompanha a câmera = parece bem longe

    // Bosses e vitória
    private const string NomeControladorBoss = "ControladorDeBoss";
    private const string NomeTelaVitoria = "VitoriaUI";
    private const string NomeGerenciadorDeWaves = "GerenciadorDeWaves";
    private const string CaminhoUxmlVitoria = "Assets/UI/Vitoria.uxml";
    private const string CaminhoPanelSettings = "Assets/UI Toolkit/PanelSettings.asset"; // reserva, se não achar o do GameOverUI
    private const int OrdemTelaVitoria = 20;            // mesma ordem da tela de Game Over (acima do HUD)

    // Menu de pausa (MenuDePausa + Pausa.uxml, feito a partir do menu principal)
    private const string NomeMenuDePausa = "MenuDePausa";
    private const string CaminhoUxmlPausa = "Assets/UI/Pausa.uxml";
    private const int OrdemMenuDePausa = 30;            // acima do HUD, do LevelUp e das telas de fim
    // Lugares onde procurar o prefab da gema de XP (vale o primeiro que existir).
    private static readonly string[] CaminhosGema = { "Assets/Prefabs/GemsXp.prefab", "Assets/Resources/GemsXp.prefab" };

    // Cria a CenaFinal a partir da cena base (se não existir), liga os sistemas por código, salva e coloca a cena em 1º no Build.
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
        LigarWaves(player);
        LigarSetaDoBoss();
        LigarTela<VitoriaUI>(NomeTelaVitoria, CaminhoUxmlVitoria, OrdemTelaVitoria);
        LigarTela<MenuDePausa>(NomeMenuDePausa, CaminhoUxmlPausa, OrdemMenuDePausa);
        LigarMenu();
        MontarFundo(camera);
        ColocarNoBuild();

        EditorSceneManager.MarkSceneDirty(final);
        EditorSceneManager.SaveScene(final);
        Debug.Log("MontarCenaFinal: pronto! " + CenaFinal + " salva e colocada em 1º nas Build Settings.");
    }

    // Garante o CameraFollow na câmera principal e aponta o campo "player" dele para o Player.
    private static void LigarCameraFollow(Camera camera, Transform player)
    {
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<CameraFollow>();
        }

        DefinirCampo(follow, "player", player);
    }

    // Garante o EnemySpawn na cena e um SpawnContinuo junto dele, deixando o SpawnContinuo desligado.
    private static void LigarSpawn()
    {
        EnemySpawn spawn = Object.FindAnyObjectByType<EnemySpawn>();
        if (spawn == null)
        {
            spawn = new GameObject("Spawner").AddComponent<EnemySpawn>();
        }

        SpawnContinuo continuo = spawn.GetComponent<SpawnContinuo>();
        if (continuo == null)
        {
            continuo = spawn.gameObject.AddComponent<SpawnContinuo>();
        }

        // Waves: o spawn contínuo fica na cena, mas desligado (o GerenciadorDeWaves assume o spawn).
        continuo.enabled = false;
        EditorUtility.SetDirty(continuo);
    }

    // Objeto "GerenciadorDeWaves" (separado do Spawner) ligado ao EnemySpawn, ao ControladorDeBoss e ao LevelUpManager,
    // e o BonusPassivoDeLevel no Player (o level virou bônus passivo; os cards vêm por wave). Precisa rodar depois de
    // LigarSpawn e LigarBoss.
    private static void LigarWaves(PlayerMove player)
    {
        GerenciadorDeWaves waves = Object.FindAnyObjectByType<GerenciadorDeWaves>();
        if (waves == null)
        {
            waves = new GameObject(NomeGerenciadorDeWaves).AddComponent<GerenciadorDeWaves>();
        }

        DefinirReferenciaSeAchou(waves, "spawn", Object.FindAnyObjectByType<EnemySpawn>());
        DefinirReferenciaSeAchou(waves, "controladorBoss", Object.FindAnyObjectByType<ControladorDeBoss>());

        LevelUpManager cartas = Object.FindAnyObjectByType<LevelUpManager>();
        if (cartas == null)
        {
            Debug.LogWarning("MontarCenaFinal: não há LevelUpManager na cena base; as waves vão seguir sem oferecer cards.");
        }

        DefinirReferenciaSeAchou(waves, "levelUpManager", cartas);

        if (player.GetComponent<BonusPassivoDeLevel>() == null)
        {
            player.gameObject.AddComponent<BonusPassivoDeLevel>();
        }
    }

    // Só preenche o campo se achou o objeto (não apaga uma referência que já estava certa).
    private static void DefinirReferenciaSeAchou(Object alvo, string campo, Object valor)
    {
        if (valor != null)
        {
            DefinirCampo(alvo, campo, valor);
        }
    }

    // Seta do boss fora da tela: SetaDoBoss no mesmo objeto do HudProgressao (usa o PanelRenderer dele).
    // Precisa rodar depois de LigarBoss. Pode rodar de novo: não duplica.
    private static void LigarSetaDoBoss()
    {
        HudProgressao hud = Object.FindAnyObjectByType<HudProgressao>();
        if (hud == null)
        {
            Debug.LogWarning("MontarCenaFinal: não achei o HudProgressao na cena; a seta do boss não foi ligada.");
            return;
        }

        SetaDoBoss seta = hud.GetComponent<SetaDoBoss>();
        if (seta == null)
        {
            seta = hud.gameObject.AddComponent<SetaDoBoss>();
        }

        DefinirReferenciaSeAchou(seta, "painel", hud.GetComponent<PanelRenderer>());
        DefinirReferenciaSeAchou(seta, "controladorBoss", Object.FindAnyObjectByType<ControladorDeBoss>());
    }

    // Garante o GerenciadorDeSom e preenche os sons (tiros, explosões, level up, power-up, game over, música, boss e vitória) com arquivos de Assets/Sons.
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

    // Devolve o primeiro prefab de gema achado em CaminhosGema, ou null se nenhum existir.
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

    // Objeto com o componente T + PanelRenderer com o UXML da tela (VitoriaUI, MenuDePausa...), usando o mesmo
    // PanelSettings do GameOverUI e a ordem de desenho pedida.
    private static void LigarTela<T>(string nomeObjeto, string caminhoUxml, int ordem) where T : MonoBehaviour
    {
        T tela = Object.FindAnyObjectByType<T>();
        if (tela == null)
        {
            GameObject objeto = new GameObject(nomeObjeto);
            objeto.AddComponent<PanelRenderer>();
            tela = objeto.AddComponent<T>();
        }

        PanelRenderer painel = tela.GetComponent<PanelRenderer>();
        if (painel == null)
        {
            painel = tela.gameObject.AddComponent<PanelRenderer>();
        }

        PanelSettings settings = AcharPanelSettings();
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(caminhoUxml);
        if (settings == null || uxml == null)
        {
            Debug.LogWarning("MontarCenaFinal: não consegui ligar '" + nomeObjeto + "' (PanelSettings ou " + caminhoUxml + " não encontrado). Confira o PanelRenderer do objeto.");
        }

        // Nomes dos campos serializados do PanelRenderer (iguais aos que aparecem na cena salva).
        DefinirCampo(painel, "m_PanelSettings", settings);
        DefinirCampo(painel, "sourceAsset", uxml);
        painel.sortingOrder = ordem;
    }

    // O GameManager só abre o menu se o campo menuPanel estiver preenchido (vazio = vai direto ao jogo).
    // Liga ao PanelRenderer do objeto que tem o MenuManager; se já estiver preenchido, não mexe.
    private static void LigarMenu()
    {
        GameManager gerenciador = Object.FindAnyObjectByType<GameManager>();
        MenuManager menu = Object.FindAnyObjectByType<MenuManager>();
        PanelRenderer painelDoMenu = menu != null ? menu.GetComponent<PanelRenderer>() : null;

        if (gerenciador == null || painelDoMenu == null)
        {
            Debug.LogWarning("MontarCenaFinal: não achei o GameManager ou o objeto com MenuManager + PanelRenderer. O campo menuPanel do GameManager ficou como está.");
            return;
        }

        SerializedProperty campo = new SerializedObject(gerenciador).FindProperty("menuPanel");
        if (campo != null && campo.objectReferenceValue == null)
        {
            DefinirCampo(gerenciador, "menuPanel", painelDoMenu);
        }
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

    // Devolve o primeiro Sprite dentro do arquivo de imagem, ou null se não houver.
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

    // Preenche um campo float [SerializeField] privado, como se fosse digitado no Inspector.
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

    // Põe a CenaFinal em 1º lugar nas Build Settings, mantendo as outras cenas depois dela.
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

    // Carrega o som do caminho e o põe no campo do alvo; avisa no Console se o arquivo não existir.
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
