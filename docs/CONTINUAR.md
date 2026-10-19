# Como continuar o projeto (passagem de bastão, 19/10/2026)

Este arquivo existe para quem pegar o projeto agora (outra conta, outro Claude, Claude Code no PC).
Leia este arquivo, depois `README.md`, `docs/estrutura-de-mods.md`, `docs/conversao-rimimmortal.md` e `docs/design.md`.

## Como o Paulo trabalha

- Fala português. Responder sempre em português.
- Não usa GitHub. Entrega = só o link de download do zip da branch. Nada de anexar ou zipar arquivos.
  - Branch atual: https://github.com/Raz3n28/Wuxia-The-New-Order/archive/refs/heads/claude/stoic-dijkstra-yqr6we.zip
- Ele compila e testa no PC dele (`build.cmd`, `install.cmd`). As DLLs do jogo nunca vão para o GitHub.
- Joga com o jogo em chinês + um mod próprio de tradução. Os textos do nosso mod aparecem em inglês ou português, escolhidos pelo botão "Idioma" na aba Cultivo.
- Logs do mod no PC dele: `%USERPROFILE%\AppData\LocalLow\CatStudio\Outer Space\Wuxia The New Dawn\Logs\wuxia.log`.

## Estado do código

- `main`: base do mod (cultivo, Qi, almofada de meditação) + Fase 1 aprovada no jogo em 19/10/2026 (PRs #1 e #2 juntados): pílulas, materiais, receitas, Caldeirão Pequeno (96901), Caldeirão Taiyi (96902), Bancada de Elixires (96903). Gerado por `tools/gen_phase1.py`.
- `claude/stoic-dijkstra-yqr6we`: Fase 2, em cima da `main`.
- `testMode` está LIGADO no `config.json` (kit de teste e construção instantânea). Desligar antes de publicar no Workshop.

## Compilar sem o PC do Paulo

Precisa das DLLs da pasta `Outer Space_Data\Managed` do jogo e do `0Harmony.dll` do Workshop (mod 3803065949).
`dotnet build src/WuxiaMod/WuxiaMod.csproj -c Release -p:GameManagedDir=<pasta Managed> -p:HarmonyDll=<0Harmony.dll>`
Para ler o código do jogo: `ilspycmd` (dotnet tool) no `Assembly-CSharp.dll`.

## Problemas da Fase 1 (todos resolvidos e confirmados)

| # | Problema | Correção |
|---|----------|----------|
| 1 | Textos dos itens novos em chinês | `DefTexts.cs` + botão de idioma; português em `Mod/v_common/Languages/defs.pt-BR.json` (confirmado) |
| 2 | Modo de teste sem materiais de construção e sem construção na hora | `TestMode.cs` (kit), `InstantBuildPatch.cs` (confirmado) |
| 3 | Imagens dos caldeirões trocadas, prévia de cabeça para baixo, construídos viravam a mesa do jogo | `BuildVisualsPatch.cs` (textura própria, prévia de pé, desenha o RawIcon) e SortId único em `DefAliases.cs` |
| 4 | Bancada de Elixires com imagem errada no menu e na prévia | sem RawIcon, usa o visual da bancada do jogo |
| 5 | Fonte grande na aba Cultivo, botões por cima do texto | `HeroInfoCultivationTab.cs` |
| 6 | Ícone do Pequeno e do Taiyi invertidos no menu | imagem do Pequeno com borda transparente 80x80 (confirmado) |
| 7 | Cadeira (lugar de trabalho) longe dos caldeirões | `Mark` no def.xml: 96901=1, 96902=7, 96903=4. A cadeira mostrada ao construir é só um holograma de onde o jogador põe uma cadeira; está certo assim, não mexer. |
| 8 | Ícone da Bancada de Elixires diferente da bancada no mapa | `BuildVisualsPatch.PlayerInfo_Postfix`: ícone = modelo do mapa |
| 9 | Esse ícone novo ficou espelhado | `BuildVisualsPatch.IconNode_Prefix` desenha com escala X = -1. (confirmado) |

## Próximos passos

1. Fase 2 conforme `docs/conversao-rimimmortal.md`: números do cultivo do Core, Desvio de Qi e Plataforma de Ruptura.

## Texto para colar no novo lugar

> Estou continuando o mod Wuxia para o jogo Order: The New Dawn. O código está em github.com/Raz3n28/Wuxia-The-New-Order, na branch claude/stoic-dijkstra-yqr6we. Leia primeiro o arquivo docs/CONTINUAR.md dessa branch, que explica como eu trabalho e o que falta. Fale comigo em português.
