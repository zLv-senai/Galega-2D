using UnityEngine;

//Classe responsável por destruir o objeto quando ele estiver fora da tela
public class DestruirForaDaTela : MonoBehaviour
{
    //Variável para verificar se o objeto já apareceu na tela
    private bool jaApareceuNaTela = false;

    //Referência da câmera principal cacheada, em vez de buscar Camera.main todo frame
    private Camera cameraPrincipal;

    void Start()
    {
        cameraPrincipal = Camera.main;
    }

    void Update()
    {
        //Se ainda não conseguimos a câmera (ex.: objeto instanciado antes dela existir), tenta de novo.
        if (cameraPrincipal == null)
        {
            cameraPrincipal = Camera.main;

            if (cameraPrincipal == null)
            {
                return;
            }
        }

        //Verificando se o objeto está dentro da tela. Se estiver, a variável jaApareceuNaTela é definida como true. Se o objeto sair da tela depois de ter aparecido, ele será destruído.-
        Vector3 viewportPos = cameraPrincipal.WorldToViewportPoint(transform.position);

        //Se o objeto estiver dentro da tela, a variável jaApareceuNaTela é definida como true.
        if (!(viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1))
        {
            //O objeto está dentro da tela, então definimos a variável jaApareceuNaTela como true.
            jaApareceuNaTela = true;
        }
        //Se o objeto já apareceu na tela e agora está fora dela, ele será destruído.
        else if (jaApareceuNaTela)
        {
            //Destruir o objeto fora da tela
            Destroy(gameObject);
        }
    }
}