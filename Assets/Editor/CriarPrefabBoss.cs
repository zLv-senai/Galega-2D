using UnityEditor;
using UnityEngine;

// Menu "Galega > Criar Prefab do Boss": cria (ou sobrescreve) Assets/Resources/Boss.prefab, que o
// ControladorDeBoss usa para instanciar os bosses. O prefab nasce de um GameObject temporário, montado por código
// (não se edita prefab à mão). Pode rodar de novo: só reescreve o mesmo arquivo.
// O "Galega > Montar Cena Final" chama GarantirPrefab() e cria o prefab sozinho se ele ainda não existir.
public static class CriarPrefabBoss
{
    public const string CaminhoPrefab = "Assets/Resources/Boss.prefab";

    private const string CaminhoSprite = "Assets/Images/boss2.png";
    private const string CaminhoAnimator = "Assets/Images/Player/boss2_1.controller";
    private const string CaminhoTiro = "Assets/Prefabs/ShotEnemy.prefab";

    // Mesma tag dos inimigos (Enemy.prefab): o tiro do boss usa essa tag para não acertar inimigos nem o próprio boss.
    private const string TagInimigo = "Inimigo";

    private const float Escala = 3f;
    private const int OrdemDeDesenho = 10;
    private const int VidaDoBoss = 60;
    private const int DanoPorContatoDoBoss = 20;

    // O boss2.anim alterna dois quadros do boss2.png. Se eles tiverem tamanhos muito diferentes (hoje: 66x74 e 6x18),
    // o boss piscaria para um sprite minúsculo, então o Animator só é colocado quando os quadros são parecidos.
    // Mude para true para colocar o Animator mesmo assim.
    private const bool ForcarAnimator = false;
    private const float AreaMinimaRelativaDosQuadros = 0.5f;

    [MenuItem("Galega/Criar Prefab do Boss")]
    public static void CriarPeloMenu()
    {
        GameObject prefab = Criar();
        if (prefab == null)
        {
            return;
        }

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("CriarPrefabBoss: " + CaminhoPrefab + " criado/atualizado. Agora rode Galega > Montar Cena Final.");
    }

    // Devolve o prefab que já existe, ou cria um novo se ainda não houver.
    public static GameObject GarantirPrefab()
    {
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
        return existente != null ? existente : Criar();
    }

    // Cria/sobrescreve o prefab. Devolve o asset salvo, ou null se algo essencial faltar (o erro vai para o Console).
    public static GameObject Criar()
    {
        Sprite sprite = CarregarSpriteDoBoss();
        if (sprite == null)
        {
            Debug.LogError("CriarPrefabBoss: não achei nenhum Sprite em " + CaminhoSprite + ". Confira se o Texture Type é Sprite (2D and UI) e o Sprite Mode é Multiple.");
            return null;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        GameObject temporario = new GameObject("Boss");
        try
        {
            Montar(temporario, sprite);
            return PrefabUtility.SaveAsPrefabAsset(temporario, CaminhoPrefab);
        }
        finally
        {
            Object.DestroyImmediate(temporario);
        }
    }

    private static void Montar(GameObject boss, Sprite sprite)
    {
        boss.transform.localScale = Vector3.one * Escala;
        DefinirTag(boss);

        SpriteRenderer render = boss.AddComponent<SpriteRenderer>();
        render.sprite = sprite;
        render.sortingOrder = OrdemDeDesenho;

        AdicionarAnimator(boss);

        // Mesma configuração do Enemy.prefab: collider em trigger + Rigidbody2D cinemático, para os tiros acertarem.
        CircleCollider2D colisor = boss.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        Vector3 metade = sprite.bounds.extents;
        colisor.radius = Mathf.Max(metade.x, metade.y) * 0.9f;

        Rigidbody2D corpo = boss.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.gravityScale = 0f;

        BossController controle = boss.AddComponent<BossController>();
        controle.padrao = PadraoBoss.Rondar;
        controle.tiroPrefab = CarregarTiro();
        controle.gun = boss.transform;
        controle.vida = VidaDoBoss;
        controle.mirarNoPlayer = true;

        // dano é [SerializeField] privado: preenche como se fosse digitado no Inspector.
        DanoPorContato contato = boss.AddComponent<DanoPorContato>();
        SerializedObject so = new SerializedObject(contato);
        SerializedProperty dano = so.FindProperty("dano");
        if (dano != null)
        {
            dano.intValue = DanoPorContatoDoBoss;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("CriarPrefabBoss: o campo 'dano' não existe no DanoPorContato. O boss ficou com o dano padrão.");
        }
    }

    private static void DefinirTag(GameObject boss)
    {
        try
        {
            boss.tag = TagInimigo;
        }
        catch (UnityException)
        {
            Debug.LogWarning("CriarPrefabBoss: a tag '" + TagInimigo + "' não existe em Project Settings > Tags and Layers. O boss ficou sem tag e o tiro dele pode acertar inimigos.");
        }
    }

    private static GameObject CarregarTiro()
    {
        GameObject tiro = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoTiro);
        if (tiro == null)
        {
            Debug.LogWarning("CriarPrefabBoss: não achei " + CaminhoTiro + ". O boss ficou sem tiroPrefab (arraste um no Inspector do prefab).");
        }

        return tiro;
    }

    // O boss2.png tem 2 sprites (66x74 e 6x18): usa o maior, que é o corpo do boss.
    private static Sprite CarregarSpriteDoBoss()
    {
        Sprite maior = null;
        float maiorArea = 0f;

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(CaminhoSprite))
        {
            if (asset is Sprite sprite)
            {
                float area = sprite.rect.width * sprite.rect.height;
                if (area > maiorArea)
                {
                    maior = sprite;
                    maiorArea = area;
                }
            }
        }

        return maior;
    }

    private static void AdicionarAnimator(GameObject boss)
    {
        RuntimeAnimatorController controle = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CaminhoAnimator);
        if (controle == null)
        {
            Debug.Log("CriarPrefabBoss: " + CaminhoAnimator + " não existe. O boss ficou sem Animator.");
            return;
        }

        if (!ForcarAnimator && !QuadrosSaoParecidos(controle))
        {
            Debug.LogWarning("CriarPrefabBoss: os quadros do " + CaminhoAnimator + " têm tamanhos bem diferentes (o boss piscaria para um sprite minúsculo). O boss ficou SEM Animator. Para colocar mesmo assim, mude ForcarAnimator para true em CriarPrefabBoss.cs.");
            return;
        }

        Animator animator = boss.AddComponent<Animator>();
        animator.runtimeAnimatorController = controle;
    }

    // true se todos os sprites trocados pelas animações do controller têm área parecida (ou se não há troca de sprite).
    private static bool QuadrosSaoParecidos(RuntimeAnimatorController controle)
    {
        float menor = float.MaxValue;
        float maior = 0f;

        foreach (AnimationClip clip in controle.animationClips)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.propertyName != "m_Sprite")
                {
                    continue;
                }

                foreach (ObjectReferenceKeyframe quadro in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                {
                    if (quadro.value is Sprite sprite)
                    {
                        float area = sprite.rect.width * sprite.rect.height;
                        menor = Mathf.Min(menor, area);
                        maior = Mathf.Max(maior, area);
                    }
                }
            }
        }

        return maior <= 0f || menor >= maior * AreaMinimaRelativaDosQuadros;
    }
}
