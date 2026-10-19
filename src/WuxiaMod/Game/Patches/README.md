# Patches Harmony

Aqui ficam os patches que mexem em classes do jogo (tela do personagem, personagens, bancadas, inimigos). `ModEntry` aplica todos com `Harmony.PatchAll`.

Tudo em `Game/` só compila quando o jogo e o Harmony do Workshop são encontrados (veja `GamePaths.props.example`).

- `HeroInfoCultivationTab`: aba Cultivo na janela do personagem.
- `SavePatches`: carrega e grava o cultivo junto com os saves do jogo.
