using UnityEngine;

//Classe responsável por destruir o objeto quando ele estiver fora da tela
public class DestruirForaDaTela : MonoBehaviour
{
    //Variável para verificar se o objeto já apareceu na tela
    private bool jaApareceuNaTela = false;

    void Update()
    {   
        //Verificando se o objeto está dentro da tela. Se estiver, a variável jaApareceuNaTela é definida como true. Se o objeto sair da tela depois de ter aparecido, ele será destruído.-
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);

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