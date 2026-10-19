# Wuxia: Cultivo Estelar (Order: The New Dawn)

Mod de cultivo Wuxia/Xianxia para **Order: The New Dawn** (Steam 3080070). No Windows a pasta do jogo se chama `Outer Space`.

Idiomas: **inglês** e **português (Brasil)**.

## O que o mod vai ter

- Raízes espirituais: só personagens com aptidão cultivam.
- 12 estágios de cultivo, do Refinamento de Qi ao Imperador Imortal, com níveis, gargalos e rupturas.
- Tela de cultivo na tela do personagem, com árvore de habilidades e passivas por pontos.
- Alquimia e forja próprias, seitas e inimigos novos, em cima do mundo atual do jogo.

Design completo em [docs/design.md](docs/design.md). Como os mods do jogo funcionam: [docs/estrutura-de-mods.md](docs/estrutura-de-mods.md).

## Estado atual

Base do projeto. Já tem:
- Estrutura de mod do jogo (`Mod/Manifest` e `Mod/v_common`).
- Ponto de entrada (`ModPluginLauncher`) que carrega dados, idiomas e patches Harmony.
- Núcleo do cultivo sem depender do jogo: raízes, estágios, Qi, níveis e ruptura.
- Dados em JSON (`Mod/v_common/Data`) e textos em EN/PT-BR (`Mod/v_common/Languages`).

Ainda não mexe em nada dentro do jogo: o mod carrega, lê os dados e escreve no log.

## Requisitos

- Order: The New Dawn.
- Inscrição no mod [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=3803065949) do Workshop: o build copia o `0Harmony.dll` dele para dentro do nosso mod.
- [.NET SDK](https://dotnet.microsoft.com/download) 8 ou mais novo, para compilar.

## Compilar e instalar

1. Se o jogo não estiver no caminho padrão da Steam, copie `GamePaths.props.example` para `GamePaths.props` e ajuste.
2. Rode `build.cmd`. A DLL sai em `Mod\v_common\Assemblies\WuxiaMod.dll`.
3. Rode `install.cmd`. Ele copia o mod para `...\Outer Space\mods\WuxiaMod`.
4. No jogo, ative o **Wuxia** no gerenciador de mods e reinicie.
5. O log do mod fica em `%USERPROFILE%\AppData\LocalLow\CatStudio\Outer Space\Wuxia The New Dawn\Logs\wuxia.log` (o da sessão anterior em `wuxia-prev.log`). Avisos e erros também aparecem no `Player.log` com a tag `[Wuxia]`.

Tudo o que o mod gerar no seu PC fica na pasta `Wuxia The New Dawn`.

## Idioma

`Mod/v_common/config.json`: `"language": "auto"` usa o idioma do Windows (português se o Windows estiver em português, senão inglês). Use `"en"` ou `"pt-BR"` para forçar.

Para adicionar textos, coloque a mesma chave em `en.json` e `pt-BR.json`. Se faltar no português, o mod mostra o inglês.

## Estrutura

```
Mod/                      o que vai para a pasta mods do jogo
  Manifest/               manifest.xml, icon.png, preview.png
  v_common/
    Assemblies/           WuxiaMod.dll (gerada pelo build)
    Data/                 realms.json, spirit_roots.json
    Languages/            en.json, pt-BR.json
    Resources/Def/        def.xml (definições no formato do jogo)
    config.json
src/WuxiaMod/
  Cultivation/            regras de cultivo (não dependem do jogo)
  Json/, Localization/    leitor de JSON e textos EN/PT-BR
  Game/                   entrada do mod e patches Harmony (dependem do jogo)
docs/                     design e pesquisa
```

As DLLs do jogo nunca entram no repositório.
