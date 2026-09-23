using UnityEngine;

public class LookAt : MonoBehaviour
// "class" é como um MOLDE/PLANTA que representa o nosso jogador dentro do código.
// "public" significa que essa classe pode ser vista e usada por outras partes do projeto.
// "Player" é o NOME da classe - precisa ser IGUAL ao nome do arquivo (Player.cs).
// ": MonoBehaviour" significa que essa classe HERDA características especiais da Unity,
// o que permite ela ser "anexada" (arrastada) em cima de um GameObject (nossa nave)
// e ganhar acesso a Start(), Update(), transform, e outras coisas prontas da engine.
{
    void Start()
    {
        // Start() é uma função ESPECIAL da Unity (já vem "de fábrica" no MonoBehaviour).
        // Ela roda APENAS 1 VEZ, no exato momento em que o objeto entra na cena/jogo.
        // "void" significa que essa função NÃO retorna nenhum valor, só executa ações.
        // Por enquanto está vazia, pois ainda não usamos ela.
    }

    void Update()
    {
        // Update() também é uma função especial da Unity.
        // Diferente do Start(), ela roda TODO FRAME, repetidamente, sem parar,
        // enquanto o jogo estiver rodando (ex: 60 vezes por segundo, se o jogo roda a 60fps).
        // É aqui que colocamos coisas que precisam ser checadas/atualizadas o tempo todo.

        OlharParaMouse(); 
        // Isso é uma CHAMADA de função: estamos "executando" a função OlharParaMouse()
        // que criamos logo abaixo. Como está dentro do Update(), ela vai rodar
        // repetidamente também, todo frame, fazendo a nave girar em tempo real.
    }

    void OlharParaMouse()
    // Essa é uma função CRIADA POR NÓS (não existe pronta na Unity).
    // "void" = não retorna valor. "OlharParaMouse" = nome que escolhemos.
    // "()" = parênteses vazios porque não estamos passando nenhum parâmetro/informação extra pra ela.
    {
        Vector3 posMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // Vector3 = uma "caixinha" que guarda 3 números: X, Y e Z (usado mesmo em jogos 2D
        // por causa de como a câmera enxerga o espaço).
        // Input.mousePosition = pega a posição do mouse NA TELA, em PIXELS (ex: pixel 850, 400).
        // Camera.main = pega a câmera principal da cena.
        // .ScreenToWorldPoint(...) = "traduz" essa posição de pixels da tela para uma posição
        // real dentro do MUNDO do jogo (coordenadas do cenário, não da tela).
        // Guardamos esse resultado na variável "posMouse".

        Vector2 direção = posMouse - transform.position;
        // Vector2 = uma "caixinha" que guarda 2 números: X e Y (usado em jogos 2D).
        // transform.position = a posição ATUAL do nosso próprio objeto (a nave) no mundo.
        // Aqui calculamos a DIREÇÃO: pra "andar/apontar" do ponto onde a nave está (origem)
        // até o ponto onde o mouse está (destino), fazemos: destino - origem.
        // Resultado: um vetor (seta) apontando da nave para o mouse.

        float angulo = Mathf.Atan2(direção.y, direção.x) * Mathf.Rad2Deg;
        // float = tipo de variável que guarda números com casas decimais (ex: 45.5).
        // Mathf = "caixa de ferramentas" matemática da Unity (contém funções trigonométricas, etc).
        // Atan2(y, x) = função matemática (arco tangente) que calcula o ÂNGULO da nossa direção,
        // considerando corretamente todos os "lados" possíveis (cima, baixo, direita, esquerda).
        // IMPORTANTE: a ordem dos parâmetros é sempre Y primeiro, depois X.
        // O resultado do Atan2 vem em RADIANOS (outra unidade de medir ângulo, tipo Km x Milhas).
        // * Mathf.Rad2Deg = multiplica pra CONVERTER de radianos para GRAUS (0° a 360°),
        // que é a unidade que a gente entende e usa no dia a dia.

        Quaternion rotação = Quaternion.Euler(0, 0, angulo);
        // Quaternion = um formato especial que a Unity usa internamente para representar rotações
        // (evita problemas matemáticos que ângulos simples têm, tipo "gimbal lock" - não precisa
        // se preocupar com esse termo agora, só saber que é o "jeito certo" de rotacionar).
        // Quaternion.Euler(x, y, z) = cria uma rotação a partir de 3 ângulos (X, Y, Z).
        // Como estamos em 2D, só giramos no eixo Z (0, 0 nos outros dois eixos).

        transform.rotation = rotação;
        // transform = representa a "posição/rotação/escala" do nosso próprio objeto (a nave).
        // .rotation = a propriedade de ROTAÇÃO do objeto.
        // Aqui aplicamos de fato a rotação calculada, fazendo a nave girar e apontar pro mouse.
    }
}