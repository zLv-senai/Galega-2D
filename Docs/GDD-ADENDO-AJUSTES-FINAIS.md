# Adendo ao GDD — Ajustes finais (Game Over, Cards e Ritmo)

Projeto: Galega (Senai Fundação Zerrener — Programação de Jogos Digitais, Sete Lagoas).
Versão do jogo: build final, branch `main` (commit `169827c`).

## 1. Resumo

Esta atualização refina a experiência de fim de partida e da escolha de cards, e deixa o ritmo do jogo mais acelerado.
Três mudanças principais: (1) novas opções na tela de Game Over, (2) tela de cards mais legível, com confirmação explícita da escolha, e (3) cards por chance, com ritmo de combate mais intenso.

## 2. Features adicionadas

### 2.1 Tela de Game Over
- Três opções ao fim da partida: **Jogar novamente**, **Voltar ao menu principal** e **Sair**.
- Proteção contra clique acidental: os botões ignoram cliques nos primeiros 0,5 s depois da tela aparecer (o jogador costuma estar atirando quando morre).
- As mesmas ações de voltar ao menu e sair são compartilhadas com o menu de pausa, garantindo comportamento idêntico (inclusive o salvamento dos volumes).

### 2.2 Tela de escolha de cards
- **Texto mais curto e legível**: título "WAVE N CONCLUÍDA" e subtítulo "Escolha um card", centralizados e com quebra de linha automática.
- **Layout adaptável**: os três cards dividem a largura disponível e encolhem em telas menores, evitando que o texto ou os cards saiam da tela.
- **Seleção + confirmação**: clicar em um card apenas o seleciona (destaque visual: maior e com borda mais grossa). A escolha só vale ao clicar no botão **CONFIRMAR**, que fica desabilitado ("SELECIONE UM CARD") enquanto nenhum card estiver selecionado. Isso evita escolhas por engano e dá tempo de ler o card.

### 2.3 Frequência de cards e ritmo de jogo
- **Cards por chance**: ao concluir uma wave normal, há **40%** de chance de o jogador receber uma escolha de cards (valor ajustável).
- **Cards garantidos nos bosses**: as waves de boss intermediárias (5 e 10) sempre oferecem cards.
- **Ritmo mais frenético**: aviso de wave mais curto (1,5 s), mais inimigos a cada wave (+4), nascimento mais rápido (a cada 0,4 s) e limite de inimigos simultâneos maior (55).

## 3. O que saiu / foi alterado

| Antes | Agora |
|---|---|
| Cards oferecidos ao fim de **toda** wave | Cards por **chance** (40%) nas waves normais; garantidos nos bosses intermediários |
| Escolha do card com **um clique** (sem confirmação) | **Selecionar e confirmar** (botão Confirmar) |
| Título longo "WAVE N CONCLUÍDA — escolha um card", fonte grande, podia sair da tela | Título curto + subtítulo, fonte menor, centralizado, com quebra de linha |
| Cards com largura fixa (300 px cada) | Cards com largura flexível (até 300 px), adaptados à tela |
| Game Over com um único botão ("Jogar de novo") | Três botões: Jogar novamente, Voltar ao menu principal e Sair |
| Anúncio de wave de 2 s; +3 inimigos por wave; spawn a cada 0,6 s; até 40 inimigos | 1,5 s; +4 inimigos por wave; spawn a cada 0,4 s; até 55 inimigos |

Observações:
- O **boss final** (wave 15 da Campanha) não oferece cards: a partida termina direto na tela de Vitória.
- Subir de level continua concedendo apenas bônus passivos; os cards vêm somente do fim de waves.

## 4. Parâmetros de balanceamento (ajustáveis)

Componente `GerenciadorDeWaves`, na cena `CenaFinal`:

| Parâmetro | Valor | Efeito |
|---|---|---|
| Chance De Oferta | 0,4 (40%) | Chance de cards em waves normais (0 a 1). Boss sempre oferece |
| Duração Anúncio | 1,5 s | Tempo do aviso "WAVE N" |
| Inimigos A Mais Por Wave | 4 | Crescimento do número de inimigos por wave |
| Intervalo Entre Spawns | 0,4 s | Tempo entre o nascimento de inimigos |
| Max Vivos | 55 | Limite de inimigos ao mesmo tempo |

## 5. Pontos de atenção para testes

- Com chance de 40%, podem ocorrer sequências de waves sem cards; avaliar se é aceitável ou se vale uma "garantia" de card após N waves sem oferta.
- Com menos cards e mais inimigos, a dificuldade sobe: validar o equilíbrio principalmente nas waves 6 a 10.
- Conferir os textos e os cards nas resoluções 1280×720, 1366×768, 1920×1080, 16:10 e 4:3.
- Checklist completo: `Docs/CHECKLIST-TESTES-AJUSTES-FINAL.md`.
