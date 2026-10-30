# Como continuar o projeto (passagem de bastão, 19/10/2026)

Este arquivo existe para quem pegar o projeto agora (outra conta, outro Claude, Claude Code no PC).
Leia este arquivo, depois `README.md`, `docs/estrutura-de-mods.md`, `docs/conversao-rimimmortal.md` e `docs/design.md`.

## Como o Paulo trabalha

- Fala português. Responder sempre em português.
- Não usa GitHub. Entrega = só o link de download do zip da branch. Nada de anexar ou zipar arquivos.
  - Branch atual: https://github.com/Raz3n28/Wuxia-The-New-Order/archive/refs/heads/claude/project-thread-lv3pty.zip
- Ele compila e testa no PC dele (`build.cmd`, `install.cmd`). As DLLs do jogo nunca vão para o GitHub.
- Joga com o jogo em chinês + um mod próprio de tradução. Os textos do nosso mod aparecem em inglês ou português, escolhidos pelo botão "Idioma" na aba Cultivo.
- Logs do mod no PC dele: `%USERPROFILE%\AppData\LocalLow\CatStudio\Outer Space\Wuxia The New Dawn\Logs\wuxia.log`.

## Estado do código

- `main`: base do mod (cultivo, Qi, almofada de meditação).
- `claude/project-thread-sn2nn3` (PR #1, rascunho): Fase 1 = pílulas, materiais, receitas, Caldeirão Pequeno (96901), Caldeirão Taiyi (96902), Bancada de Elixires (96903). Gerado por `tools/gen_phase1.py`.
- `claude/project-thread-lv3pty` (PR #2, rascunho, em cima do PR #1): correções dos testes da Fase 1. É a branch que o Paulo está testando.
- `claude/project-thread-e4xo4h` (em cima do PR #5, branch `uy58dj`): Fase 2 = cultivo do RimImmortal Core (11 estágios, ruptura, Desvio de Qi, Plataforma de Cultivo 9602, Plataforma de Ruptura 9603, Rocha Espiritual 9604, itens 96018/96019/96115/96116). Instalada no PC do Paulo em 30/10/2026, falta testar.
- `testMode` está LIGADO no `config.json` (kit de teste e construção instantânea). Desligar antes de publicar no Workshop.

## Ciclo automático (desde 29/10/2026)

O Paulo só abre o jogo e testa. O resto o Claude faz sozinho pela conexão com o PC dele
(pastas liberadas: jogo, Workshop 3080070 e `LocalLow\CatStudio\Outer Space`):

1. Stage das DLLs do jogo (`Outer Space_Data\Managed\*.dll` usadas no `.csproj`, incluindo
   `com.rlabrecque.steamworks.net.dll`) e do `0Harmony.dll` (Workshop 3803065949). Refazer quando o jogo atualizar.
2. `tools/claude_build.sh --list` compila e monta a pasta do mod em `/mnt/project-files/build/WuxiaMod`,
   imprimindo cada arquivo e o destino em `...\Outer Space\mods\WuxiaMod`.
3. `device_commit_files` (force) grava esses arquivos direto em `mods\WuxiaMod` (gravar em Program Files funciona).
   O jogo precisa estar fechado para trocar a DLL.
4. Depois que o Paulo jogar: ler `Wuxia The New Dawn\Logs\wuxia.log` e `Player.log` (erros do Unity/Harmony)
   em `LocalLow\CatStudio\Outer Space` e corrigir sem esperar ele mandar print.

Plano B no PC dele: `Testar.bat` (compila, instala e abre o jogo de uma vez).

## Compilar sem o PC do Paulo

Precisa das DLLs da pasta `Outer Space_Data\Managed` do jogo e do `0Harmony.dll` do Workshop (mod 3803065949).
`dotnet build src/WuxiaMod/WuxiaMod.csproj -c Release -p:GameManagedDir=<pasta Managed> -p:HarmonyDll=<0Harmony.dll>`
Para ler o código do jogo: `ilspycmd` (dotnet tool) no `Assembly-CSharp.dll`.

## Falta testar (lista de problemas da Fase 1)

Todos os itens abaixo já foram corrigidos no PR #2, mas o Paulo ainda não confirmou todos no jogo.

| # | Problema | Correção |
|---|----------|----------|
| 1 | Textos dos itens novos em chinês | `DefTexts.cs` + botão de idioma; português em `Mod/v_common/Languages/defs.pt-BR.json` (confirmado) |
| 2 | Modo de teste sem materiais de construção e sem construção na hora | `TestMode.cs` (kit), `InstantBuildPatch.cs` (confirmado) |
| 3 | Imagens dos caldeirões trocadas, prévia de cabeça para baixo, construídos viravam a mesa do jogo | `BuildVisualsPatch.cs` (textura própria, prévia de pé, desenha o RawIcon) e SortId único em `DefAliases.cs` |
| 4 | Bancada de Elixires com imagem errada no menu e na prévia | sem RawIcon, usa o visual da bancada do jogo |
| 5 | Fonte grande na aba Cultivo, botões por cima do texto | `HeroInfoCultivationTab.cs` |
| 6 | Ícone do Pequeno e do Taiyi invertidos no menu | imagem do Pequeno com borda transparente 80x80 (confirmado) |
| 7 | Cadeira (lugar de trabalho) longe dos caldeirões | `Mark` no def.xml: 96901=1, 96902=7, 96903=4. **Falta confirmar: o colono alcança a cadeira do Taiyi (Mark=7, dentro do caldeirão)?** Se não, voltar para fora (Mark=10) e ajustar a imagem. |
| 8 | Ícone da Bancada de Elixires diferente da bancada no mapa | `BuildVisualsPatch.PlayerInfo_Postfix`: ícone = modelo do mapa |
| 9 | Esse ícone novo ficou espelhado | `BuildVisualsPatch.IconNode_Prefix` desenha com escala X = -1. **Falta testar, inclusive na janela de informações.** |

## Próximos passos

1. Paulo testa os itens 7 e 9.
2. Com a Fase 1 aprovada: juntar PR #2 no PR #1 e o PR #1 na `main`.
3. Fase 2 feita (branch `e4xo4h`): Paulo testa o kit de teste, a ruptura na Plataforma de Ruptura e o Desvio de Qi.
4. Fase 3 conforme `docs/conversao-rimimmortal.md`, mais os itens do mod Faction.

## Texto para colar no novo lugar

> Estou continuando o mod Wuxia para o jogo Order: The New Dawn. O código está em github.com/Raz3n28/Wuxia-The-New-Order, na branch claude/project-thread-lv3pty. Leia primeiro o arquivo docs/CONTINUAR.md dessa branch, que explica como eu trabalho e o que falta. Fale comigo em português.
