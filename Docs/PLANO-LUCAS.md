# Plano do Lucas — Progressão, Power-ups e Bombas

Documento de contexto do trabalho do Lucas Veloso na branch `correcoes-gameplay`.
Serve para continuar o trabalho em outro PC (ou numa nova sessão do Claude) sem perder nada.

> **Para o Claude numa sessão nova:** leia este arquivo inteiro antes de mexer no código.
>
> ⚠️ **Este arquivo e a skill `.claude/skills/subir-para-casa/` NÃO podem ir para a `main`.**
> Antes de abrir o PR ou mesclar na `main`, remova os dois da branch (ver seção 3).

_Última atualização: 2026-09-28 (PC do trabalho)._

O Lucas trabalha em **3 PCs**:

| PC | Uso |
|---|---|
| **Trabalho** | Desenvolvimento |
| **Casa** | Desenvolvimento |
| **Senai** | Onde fica a versão que o **professor avalia**. Deve ter a versão final e limpa, **sem** este `.md` nem a skill. |

---

## 1. Visão do jogo

Não é um Galaga clássico: é um **"Vampire Survivors espacial"**.

- Câmera travada no player; WASD move, o mouse mira e atira.
- Inimigos vêm em direção ao player.
- Inimigo morto solta **gema de XP**; ao subir de level aparecem **3 cards** de buff **permanente**,
  com raridades Comum → Incomum → Raro → Épico → Lendário → Mítico.
- **Baús** espalhados pelo mapa quebram com tiro e soltam **power-ups temporários**.
- **Bombas/minas** no mapa explodem em área e dão dano em quem estiver perto (player e inimigos).

## 2. Divisão do grupo

| Pessoa | Parte |
|---|---|
| Eduardo | Sons (branch `Som`, com `Pontuacao.cs` e `Som.cs` ainda fora da main) |
| Samuel | Background com parallax + câmera seguindo o player |
| Wagner | Código do player (`PlayerMove.cs`) |
| **Lucas** | **Baús de power-up, XP/level/cards, bombas e correção de bugs** |

Branches ainda **não mergeadas** que mexem em `PlayerMove.cs`/`LookAt.cs`: `Player-Move`, `Som`, `Gameplay-Teste`.
Cuidado com conflito nesses arquivos.

## 3. Regras de trabalho (combinadas com o Claude)

- **Nunca fazer commit/push sem perguntar ao Lucas antes**: o projeto é de grupo.
- Sistemas do Lucas ficam em **scripts novos e separados**; o mínimo possível de mudanças em arquivos dos colegas.
- Não editar `.unity`, `.prefab`, `.asset` nem `ProjectSettings` com o Unity aberto: passar como passo a passo no Inspector.
- Nos resumos: destacar muito o que foi **apagado** e o que **mudou muito**, e mostrar **o código original apagado logo abaixo de cada explicação**.
- Usar agents com o modelo certo para a complexidade: Haiku para tarefas simples, Sonnet para implementar e revisar, Opus para arquitetura.
- Mudanças no `PlayerMove.cs` (Wagner) serão discutidas no PR.
- Responder sempre em português do Brasil.
- Quando o Lucas pedir para "subir para mexer em casa", usar a skill `subir-para-casa`: ela atualiza este arquivo e faz commit e push da branch.
- **Este `.md` e a skill são só da branch de trabalho.** Antes do PR para a `main`:
  1. rode `git rm -r --cached Docs/PLANO-LUCAS.md .claude/skills/subir-para-casa`;
  2. faça o commit da remoção;
  3. confira que eles não aparecem em `git diff main --stat`.

## 4. O que já foi feito (branch `correcoes-gameplay`)

### Correções de bugs
- `GameManager`:
  - Virou **singleton** (`GameManager.Instance`), que usa `Destroy(this)` para não apagar a Main Camera.
  - Não chama mais `SetGameState` todo frame.
  - Começa no **Menu** quando existe `menuPanel` e vai direto para `OnPlay` quando não existe.
  - Ganhou `Restart()`.
- `PlayerMove`:
  - Quando `vida <= 0`, chama **GameOver** e desativa o player.
  - Movimento **normalizado**: a diagonal não é mais 41% mais rápida.
  - `GanharXp` usa `while`.
- `EnemyMove`:
  - `gun` virou `[SerializeField]`, com fallback `Find("Gun")`.
  - Acha o Player pela tag.
  - A coroutine `Shoot` sempre libera `canShoot`.
  - Tem proteção contra morrer duas vezes.
- `Shot`: garante `Rigidbody2D` Kinematic e usa a constante `TagParede`.
- `DestruirForaDaTela`: guarda a câmera numa variável em vez de buscar todo frame.

### Etapa 1 — Stats (`Assets/Scripts/Stats/`)
- `StatTipo.cs`: enums `StatTipo` e `TipoModificador`, struct `ModificadorDeStat` e `CalculoDeStat`, que calcula `(base + Somar) × (1 + Percentual)`.
- `PlayerStats.cs`: guarda os valores base e os modificadores por fonte. Tem `Obter`, `AdicionarModificadores`, `RemoverModificadoresDaFonte`, `FiltrarDanoRecebido` (escudo), o evento `AoMudarStat` e ContextMenus de teste.
- `PadraoDeTiro.cs`: dispara `Projeteis` tiros em leque, com 12° entre eles.
- `SincronizarVida.cs`: quando `VidaMax` muda, ajusta `PlayerMove.vida`, mantendo o valor entre 1 e o máximo.
- `EstadoDoJogo.cs`: `EstadoDoJogo.Rodando` é verdadeiro quando o estado é `OnPlay`.
- **Ganchos no `PlayerMove`**, marcados com `// PlayerStats:`:
  - `[RequireComponent(PlayerStats)]`;
  - tiro **segurando o botão**, com cadência;
  - velocidade, dano e escudo lidos dos stats.

### Etapa 2 — XP (`Assets/Scripts/Progressao/`)
- `EnemyMove.AoMorrer`: evento estático disparado antes do `Destroy`.
- `GeradorDeGemas`: ouve `AoMorrer` e cria a gema.
- `Coletor`: fica no player e procura coletáveis com `OverlapCircleAll`, usando `RaioColeta`.
- `Coletavel`: classe abstrata; o item voa até o player acelerando.
- `GemaDeXp`: é um `Coletavel` que dá XP ao ser coletado.
- `PlayerXp`: guarda level e XP. `XpNecessario = 5 + 10 × (level − 1)`, sobe vários levels de uma vez e tem os eventos `AoMudarXp` e `AoSubirDeLevel`.

### ⚠️ Código apagado

**`GameManager.cs`: o `Update()` inteiro**, que reaplicava o estado todo frame:
```csharp
void Update()
{
    SetGameState(gameState);
}
```

**`GameManager.cs`: o `Start()` antigo:**
```csharp
void Start()
{
    menuPanel.enabled = false;
}
```

**`PlayerMove.cs`: o corpo antigo do `Movimento()`**, trocado por um vetor normalizado:
```csharp
if (Keyboard.current.wKey.isPressed)
{
    transform.position = transform.position + new Vector3 (0, 1, 0) * velocidade * Time.deltaTime;
}
if (Keyboard.current.aKey.isPressed)
{
    transform.position = transform.position + new Vector3 (-1, 0, 0) * velocidade * Time.deltaTime;
}
if (Keyboard.current.sKey.isPressed)
{
    transform.position = transform.position + new Vector3 (0, -1, 0) * velocidade * Time.deltaTime;
}
if (Keyboard.current.dKey.isPressed)
{
    transform.position = transform.position + new Vector3 (1, 0, 0) * velocidade * Time.deltaTime;
}
```

**`PlayerMove.cs`: o tiro por clique**, trocado por tiro segurando o botão com `PadraoDeTiro`:
```csharp
if(Mouse.current.leftButton.wasPressedThisFrame)
...
Instantiate(tiroPrefab, gun.position, gun.rotation);
```

**`PlayerMove.cs`: o `if` do level virou `while`:**
```csharp
if (xp >= 100)
```

**`EnemyMove.cs`: a busca do `Gun` por nome e um import sem uso:**
```csharp
using Unity.VisualScripting;
...
private Transform gun;
...
gun = transform.Find("Gun").gameObject.transform;
```

**`EnemyMove.cs`: o fim da coroutine `Shoot`** saiu de dentro do `if` e foi para depois dele (não foi apagado):
```csharp
if(tiroPrefab != null && gun != null)
{
    ...
    yield return new WaitForSeconds(fireHate);
    canShoot = true;
}
```

**`Shot.cs`: o texto `"Parede"` virou a constante `TagParede`:**
```csharp
else if (outro.CompareTag("Parede"))
```

**`DestruirForaDaTela.cs`: a câmera passou a ficar guardada no `Start`:**
```csharp
Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
```

> Os campos `level`, `xp` e `velocidade` do `PlayerMove` **não foram apagados**; ficaram sem uso e ganharam um comentário.

## 5. Como testar as etapas 1 e 2 no Unity

1. Duplique `Scenes/Testes/GameTeste` e salve como `TesteProgressao`.
2. No Player, adicione `PlayerStats`, `SincronizarVida`, `PlayerXp` e `Coletor`.
3. Em Project Settings → Tags and Layers, crie a layer **`Coletavel`**. Em Physics 2D → Layer Collision Matrix, desmarque todas as colisões dela.
4. No `Coletor`, deixe `mascaraColetaveis` só com `Coletavel`.
5. Crie o prefab `GemaXp`:
   - sprite qualquer;
   - `CircleCollider2D` com **Is Trigger** e raio 0.2;
   - layer `Coletavel`;
   - componente `GemaDeXp`.
6. Crie um GameObject `Sistemas` com `GeradorDeGemas` e coloque o prefab `GemaXp` no campo `gemaPrefab`.
7. Aperte Play e confira:
   - segurando o mouse, a nave atira sem parar;
   - o inimigo morto solta uma gema;
   - a gema voa até o player;
   - depois de 5 gemas o Console mostra `Level up!`.
8. Clique com o botão direito no `PlayerStats` para testar os ContextMenus: `+1 Projetil`, `+50% Velocidade`, `Escudo` e `Limpar Testes`.

## 6. Próximos passos (plano)

| Etapa | O que fazer | Modelo |
|---|---|---|
| **3** | Cards de level up + **tela de Game Over** (hoje o Game Over congela sem UI; `Restart()` já existe) | Sonnet |
| **4** | Baús + power-ups temporários | Sonnet |
| **5** | Minas / bombas | Haiku ou Sonnet |
| **6** | `SpawnerAoRedor` (baús, minas e talvez inimigos) + `HudProgressao` (barra de XP e power-ups ativos) | Sonnet |
| **7** | Limpeza: `Assets/_Recovery` no git (ignorar `/Assets/_Recovery/`), script faltando `EnemyHealth` em `TesteShot.unity`, `SampleScene` (única do build) sem gameplay, `FUNDOJOGO.png` com 21 MB | Haiku |

### Etapa 3 — Cards de level up
- **Estado novo `LevelUp` no `GameManager`.** Adicione no **fim** do enum, porque ele é salvo como int nas cenas, e trate no mesmo `case` do `Pause` (`timeScale = 0`). Como Player e Enemy só rodam em `OnPlay`, tudo congela sozinho.
- **`CardData` (ScriptableObject):** `nome`, `descricao`, `icone`, `raridade`, `ModificadorDeStat[] modificadores`, `maxEscolhas` (0 = sem limite).
- **Raridades e pesos:** Comum 50, Incomum 25, Raro 12, Épico 8, Lendário 4, Mítico 1.
- **`SorteadorDeCards` (estático):**
  1. sorteia a raridade pelo peso;
  2. escolhe um card dessa raridade que ainda não esteja na oferta;
  3. se não houver, desce uma raridade.
- **`LevelUpManager`:** conta os levels pendentes. Enquanto houver pendentes e o jogo estiver `Rodando`, muda o estado para `LevelUp` e mostra a UI. `Escolher(card)` aplica os modificadores (fonte = o card) e volta a `OnPlay`. Tem o ContextMenu `ForcarLevelUp`.
- **`LevelUpUI`:**
  - usa `PanelRenderer` próprio no padrão do `MenuManager` (`RegisterUIReloadCallback`);
  - esconde com `style.display`, e não com `enabled`;
  - tem classes USS de `.raridade-comum` até `.raridade-mitico`.
- **Exemplos de cards:**
  - Comum: +10% velocidade.
  - Raro: +20% cadência.
  - Épico: +1 dano.
  - Lendário: +25 vida máxima.
  - Mítico: +1 projétil (`maxEscolhas 2`).
- **Tela de Game Over:** mesma técnica da UI de cards, com um botão que chama `GameManager.Instance.Restart()`.

### Etapa 4 — Baús e power-ups
- **Tag nova `Destrutivel`.** O `Shot` ganha `bool acertaDestrutiveis`: `true` no prefab `Shot` e `false` como **override** no `ShotEnemy`, que é variante do `Shot`.
- **`PowerUpData` (ScriptableObject):** `nome`, `icone`, `cor`, `duracao`, `ModificadorDeStat[] modificadores`.
- **`Bau : IDamageable`:** tem `vida = 3`, pisca ao levar dano e solta um `PowerUpPickup` (que é um `Coletavel`). A flag `quebrado` impede soltar duas vezes.
- **`PlayerPowerUps`:** guarda um `Dictionary<PowerUpData, float expiraEm>`. Pegar um power-up que já está ativo só renova o tempo. Tem os eventos `AoAtivar` e `AoExpirar`, que servem para os sons do Eduardo.

| Power-up | Modificador | Duração |
|---|---|---|
| Tiro triplo | Projeteis Somar +2 | 10s |
| Escudo | Escudo Somar +1 | 6s |
| Velocidade | Velocidade Percentual +0.5 | 10s |
| Tiro rápido | TirosPorSegundo Percentual +1.0 | 8s |
| Ímã de XP | RaioColeta Somar +30 | 2s |

### Etapa 5 — Minas
- **`Mina : IDamageable`**, com tag `Destrutivel`. Ativa quando o player chega perto (`raioAtivacao 1.5`) ou quando leva tiro.
- **Explosão:** o filho `Aviso` pisca durante `atraso 1.0`s. Depois, `OverlapCircleAll(raioExplosao 2.5)` dá `dano 20` em cada `IDamageable` (usando `HashSet`).
- Minas vizinhas explodem em cadeia. O efeito usa `explosao.png`.

### Etapa 6 — Spawner e HUD
- **`SpawnerAoRedor`:**
  - campos `prefab`, `intervalo`, `maxVivos`, `raioMin 11` / `raioMax 16` (fora da câmera) e `distanciaDespawn 40`;
  - antes de criar, testa `OverlapCircle` para não nascer dentro de parede.
- **`HudProgressao`:** barra de XP e ícones dos power-ups ativos.

### Riscos lembrados
- **Pausa:** com `timeScale = 0` o `Update` continua rodando. Toda lógica nova precisa checar `EstadoDoJogo.Rodando`. No gameplay, use `Time.time` e `WaitForSeconds`, nunca `WaitForSecondsRealtime`.
- **Evento estático `AoMorrer`:** sempre cancelar a assinatura no `OnDisable`.
- **Dano inteiro com base 1:** cards de dano devem usar `Somar`, não `Percentual`.
- **Conflitos de merge:** testar numa cena própria (`TesteProgressao`). Criar tags e layers num commit só, avisando o grupo. Sugerir que o `Pontuacao.cs` do Eduardo assine `EnemyMove.AoMorrer`.
- **Commit pelo PowerShell:** aspas dentro de `git commit -m` quebram a mensagem. Use sempre `git commit -F arquivo.txt`.

## 7. Como continuar em outro PC

```bash
git fetch
git switch correcoes-gameplay
git pull
```

Abra o projeto no Unity e faça o passo a passo da seção 5.
Numa sessão nova do Claude Code, abra a pasta do projeto e peça:
"leia Docs/PLANO-LUCAS.md e vamos continuar da etapa 3".

## 8. Histórico de sessões

| Data | PC | O que foi feito |
|---|---|---|
| 2026-09-28 | Trabalho | Análise do repositório, correções de bugs, etapas 1 (Stats) e 2 (XP/gemas), criação deste documento e da skill `subir-para-casa`, registro dos 3 PCs (trabalho, casa, Senai). **Etapas 1 e 2 ainda não foram testadas no Unity.** Próximo passo: testar (seção 5) e depois fazer a etapa 3. |
