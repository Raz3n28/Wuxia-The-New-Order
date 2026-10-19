# Patches Harmony

Aqui ficam os patches que mexem em classes do jogo (tela do personagem, personagens, bancadas, inimigos). `ModEntry` aplica todos com `Harmony.PatchAll`.

Tudo em `Game/` só compila quando o jogo e o Harmony do Workshop são encontrados (veja `GamePaths.props.example`).

- `HeroInfoCultivationTab`: aba Cultivo na janela do personagem.
- `SavePatches`: carrega e grava o cultivo junto com os saves do jogo.
- `MeditationPatch`: ganho de Qi no lazer e na almofada de meditação.
- `InstantBuildPatch`: modo de teste, construções do mod prontas assim que são colocadas.
- `BuildVisualsPatch`: imagem dos caldeirões no mapa (prévia de pé e construção pronta com o RawIcon). Ícone das nossas construções sem RawIcon (Bancada de Elixires) = o próprio modelo do mapa.
