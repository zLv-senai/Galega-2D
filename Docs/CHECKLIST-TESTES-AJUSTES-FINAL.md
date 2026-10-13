# Checklist de testes — ajustes do jogo final

Branch: `feature/ajustes-final`. Cena: `CenaFinal`. Marque cada item após testar em Play Mode e em build.

## 1. Tela de Game Over
- [ ] Morrer: aparecem **Jogar novamente**, **Voltar ao menu principal** e **Sair**.
- [ ] Clicar nos primeiros 0,5s da tela não faz nada (anti-clique acidental).
- [ ] **Jogar novamente**: reinicia direto na partida (sem menu), `Time.timeScale` = 1, level/wave/HUD zerados.
- [ ] **Voltar ao menu principal**: abre o menu inicial; iniciar nova partida funciona (Campanha e Infinito).
- [ ] **Sair**: fecha o build (no Editor, para o Play Mode); volumes continuam salvos ao reabrir.
- [ ] Repetir morrer → jogar novamente 5 vezes seguidas (sem acumular eventos nem travar).
- [ ] Modo Infinito: "Recorde" e "Você entrou no ranking" continuam corretos.
- [ ] Pausa (Esc) → **Menu** e **Exit** continuam funcionando (agora usam `GameManager.VoltarAoMenu/Sair`).

## 2. Tela de cards — texto e layout
- [ ] Título "WAVE N CONCLUÍDA" e subtítulo "Escolha um card" totalmente visíveis e centralizados.
- [ ] Testar resoluções: 1280x720, 1366x768, 1600x900, 1920x1080, 16:10 e 4:3.
- [ ] Os 3 cards cabem na tela sem cortar; textos longos quebram linha dentro do card.
- [ ] Se algo ainda sair da tela: conferir `PanelSettings` (Scale Mode = Scale With Screen Size, 1920x1080).

## 3. Tela de cards — selecionar e confirmar
- [ ] Ao abrir: nenhum card selecionado e botão mostra "SELECIONE UM CARD" desabilitado.
- [ ] Clicar num card: ele destaca (maior, borda grossa) e o botão vira "CONFIRMAR" habilitado.
- [ ] Trocar a seleção entre os 3 cards funciona; só um fica destacado.
- [ ] Confirmar aplica o card certo (conferir o status do player) e o jogo volta a rodar.
- [ ] Clique duplo rápido em Confirmar não aplica o card duas vezes.
- [ ] Confirmar dentro de 0,3s da abertura é ignorado, e a seleção continua (clicar de novo funciona).
- [ ] Mais de uma escolha pendente: após confirmar, a próxima oferta abre **sem seleção** e mostra "Escolhas restantes".
- [ ] Menos de 3 cards elegíveis: só os cards existentes aparecem e podem ser selecionados.
- [ ] Morrer com a tela de cards aberta: vai para Game Over sem erros no Console.

## 4. Frequência de cards e ritmo
- [ ] Waves normais oferecem cards com chance de 40% (`chanceDeOferta`); nas demais a wave seguinte começa direto.
- [ ] Waves de boss (5, 10) SEMPRE oferecem cards.
- [ ] Jogar ~10 waves e anotar quantas ofereceram: tem de ficar perto de 40%; ver se sequências longas sem card incomodam.
- [ ] Wave 15 (final): vitória, sem oferta de cards.
- [ ] Infinito: mesma regra (chance nas normais + boss sempre).
- [ ] Ritmo (CenaFinal / GerenciadorDeWaves): anúncio 1,5s, +4 inimigos por wave, spawn a cada 0,4s, máx. 55 vivos.
- [ ] Desempenho: FPS estável com 55 inimigos + tiros dos bosses.
- [ ] Dificuldade: conseguir passar da wave 6 e da wave do primeiro boss; ajustar `intervaloEntreSpawns`/`maxVivos` se estiver impossível ou fácil demais.

## 5. Regressão geral
- [ ] Menu principal → Campanha → boss → vitória → **Jogar de novo** (tela de Vitória).
- [ ] Pausa → Continuar → Settings (volumes) → voltar.
- [ ] Power-ups (inclusive Reparo) e gemas de XP coletam normalmente.
- [ ] Console limpo: sem NullReference/Warnings novos em Play Mode.
