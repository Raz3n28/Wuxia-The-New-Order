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

Dentro do jogo: aba **Cultivo** na janela do personagem, mostrando a raiz espiritual e o estágio de cada colono, com o botão **Despertar cultivo** para quem tem raiz. O cultivo é salvo junto com cada save do jogo. Colonos despertos ganham Qi no lazer, e bem mais na **Almofada de Meditação** (taxas em `config.json`: `qiPerHourRecreation` e `qiPerHourMeditation`, por hora de jogo).

### Alquimia (Fase 1, convertida do RimImmortal)

Três construções novas, na mesma aba da Bancada de Remédios do jogo:

| Construção | Tamanho | Faz |
|---|---|---|
| Caldeirão Alquímico Pequeno (Small Alchemy Cauldron) | 1x1 | Pílulas de 3º grau |
| Caldeirão Alquímico Taiyi (Taiyi Alchemy Cauldron) | 3x3 | Pílulas de todos os graus e o Fogo Espiritual |
| Bancada de Elixires (Elixir Workbench) | 3x1 | Base de pílula, areia de ouro, ervas, pedra espiritual e pós |

Enquanto as plantas e animais do RimImmortal não chegam, as ervas e partes de animais são feitas na Bancada de Elixires a partir de itens do jogo (folhas, raízes, estames, cogumelos, frutas, batatas, ouro, glândulas).

As pílulas são remédios: clique com o botão direito no item para usar. As de cura (Pomada de Contusão, Pílula Salva-Sangue, Pó do Retorno da Primavera, Elixir da Medula) a IA também usa sozinha. As outras só pelo botão direito:
- Qi na hora (Pílula do Retorno Espiritual +50, Pílula de Jade Espiritual +150).
- Bônus de cultivo por algumas horas: absorção de Qi e chance de ruptura (aparecem na aba Cultivo).
- Bônus de atributo: velocidade, dano corpo a corpo, temperatura, fome, energia, dano recebido.
- **Elixir do Estabelecimento da Fundação**: no nível 9 do Refinamento de Qi com o Qi cheio, tenta a ruptura para o Estabelecimento da Fundação (60% + bônus).

**Modo de teste:** com `"testMode": true` no `config.json`, a aba Cultivo ganha o botão **Kit de teste**, que põe no chão 5 de cada pílula e 30 de cada ingrediente, desperta o colono e enche o Qi até o auge do Refinamento de Qi. As bancadas e receitas do mod também ficam quase instantâneas. Deixe `false` para jogar normalmente.

Os números estão em `src/WuxiaMod/Game/PillEffects.cs`. As linhas do `def.xml` e os ícones da alquimia são gerados por `tools/gen_phase1.py` a partir dos arquivos do RimImmortal.

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

Tudo o que o mod gerar no seu PC fica na pasta `Wuxia The New Dawn`. O cultivo de cada save fica em `Saves\<nome do save>.json`; o arquivo de save do jogo não é alterado, então o save abre normalmente sem o mod.

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

## Créditos

Imagens, nomes, textos e receitas da alquimia vêm do **RimImmortal** (RimWorld, Workshop 3296476341), do mesmo autor, usados com permissão.
