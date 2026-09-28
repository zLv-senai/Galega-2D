---
name: subir-para-casa
description: Sobe o trabalho da sessão para o GitHub para o Lucas continuar em outro PC (casa/Senai). Atualiza Docs/PLANO-LUCAS.md com o que foi feito, o código apagado, o passo a passo no Unity e o plano, depois faz commit e push da branch de trabalho. Use quando o Lucas pedir "sobe para eu mexer em casa", "subir para o git para continuar em casa", "salvar para o outro PC" ou algo parecido. NÃO use para subir para a main nem para abrir PR para a main.
---

# Subir para casa

Objetivo: o Claude do outro PC precisa conseguir continuar o trabalho **só lendo `Docs/PLANO-LUCAS.md`** depois de um `git pull`.
Responda sempre em português do Brasil.

## ⚠️ Regra de ouro: nada disto vai para a `main`

`Docs/PLANO-LUCAS.md` e `.claude/skills/subir-para-casa/` são **pessoais do Lucas** e existem só na branch de trabalho.
- **Nunca** faça commit ou push na `main`. Se a branch atual for `main`, pare e pergunte ao Lucas qual branch usar.
- Quando o Lucas for **abrir PR ou mesclar na `main`**, esses dois arquivos **precisam sair antes**:
  - avise o Lucas;
  - numa branch limpa para o PR, ou num commit final de remoção, rode `git rm -r --cached Docs/PLANO-LUCAS.md .claude/skills/subir-para-casa`;
  - confira com `git diff main --stat` que eles não aparecem no PR.
  - Guarde uma cópia do `.md` fora do repositório antes, se ainda for útil.

## O que entra no commit

- **Pode entrar:**
  - scripts (`Assets/Scripts/**`) **com os `.meta` deles**;
  - `Docs/PLANO-LUCAS.md`;
  - `.claude/skills/subir-para-casa/`;
  - arquivos que o Lucas pediu explicitamente.
- **Não inclua sem perguntar:**
  - `ProjectSettings/**`;
  - `.meta` de imagens que o Unity gerou sozinho;
  - `.unity` e `.prefab` que você não criou;
  - `.claude/settings.local.json`;
  - `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- **Não edite `.unity`, `.prefab` nem `.asset`** com o Unity aberto.
- O pedido do Lucas para subir já autoriza commit e push **da branch de trabalho**. Mesmo assim, mostre a lista de arquivos antes e espere o "ok" dele se houver algo fora do padrão acima.

## Passo a passo

1. **Levantar o estado**
   - `git branch --show-current`, `git status --short -uall` e `git log --oneline -5`.
   - `git diff main --stat` e `git diff main -U0 -- Assets/Scripts`, para achar o que foi **apagado**: são as linhas com `-`.

2. **Atualizar `Docs/PLANO-LUCAS.md`.** Crie o arquivo se não existir, seguindo as seções abaixo. **Edite as seções; não duplique conteúdo.**
   - Atualize a linha `_Última atualização: <data> (<PC>)_`. Pergunte ao Lucas o PC se não souber.
   - **4. O que já foi feito:** adicione o que a sessão fez, com os arquivos criados e alterados.
   - **⚠️ Código apagado:** para cada remoção, escreva uma explicação curta e, **logo abaixo, o bloco ```csharp com o código original**, tirado de `git diff`/`git show main:<arquivo>`.
   - **5. Como testar no Unity:** o passo a passo concreto para o Inspector (componentes, prefabs, tags, layers) do que foi feito.
   - **6. Próximos passos:** marque as etapas concluídas e ajuste o plano com as decisões tomadas na sessão.
   - **Riscos e decisões:** registre qualquer decisão nova ("o Lucas escolheu X").
   - **8. Histórico de sessões:** acrescente uma linha com data, PC e resumo.
   - Seções fixas que devem continuar existindo: visão do jogo, divisão do grupo e regras de trabalho.

3. **Mostrar ao Lucas antes de subir**
   - a branch;
   - a lista de arquivos que vão entrar no commit;
   - um resumo curto das mudanças no `PLANO-LUCAS.md`.

4. **Commit** (no Windows PowerShell as aspas quebram `-m`, então **sempre use arquivo**):
   - Escreva a mensagem num arquivo temporário **fora do repositório e com caminho curto**, como `$env:TEMP\galega-commit-msg.txt`. A pasta de scratch da sessão tem um caminho longo demais e o git falha com "Filename too long". Use o formato conventional commits em português (`feat:`, `fix:`, `docs:`…), com um corpo em tópicos. Apague o arquivo depois do commit.
   - `git add -- <arquivos listados>`
   - `git commit -F <arquivo-da-mensagem>`
   - Confira com `git log --oneline -1` que o commit foi criado.

5. **Push**
   - `git push -u origin <branch>`
   - Confira que `git status -sb` mostra a branch sincronizada, sem `ahead`.

6. **Resposta final ao Lucas**
   - a branch, o hash do commit e o link `https://github.com/zLv-senai/Galega-2D/tree/<branch>`;
   - o que ficou **de fora** do commit, e por quê;
   - os comandos para o outro PC:
     ```bash
     git fetch
     git switch <branch>
     git pull
     ```
   - a frase para usar na sessão nova: "leia Docs/PLANO-LUCAS.md e vamos continuar da etapa X";
   - um lembrete: "antes do PR para a main, tirar o `.md` e a skill".
