# Order: The New Dawn — o que dá para modar

> Jogo: **Order: The New Dawn** (Steam 3080070), da desenvolvedora **Outer Space Workshop**. Por isso a pasta no Windows se chama `Outer Space`.
> Análise feita em 19/10/2026 só com pesquisa na web (página da Steam e do Workshop). Ainda não inspecionei os arquivos do jogo.

## 1. Como os mods funcionam hoje

- **Não existe ferramenta oficial de modding ainda.** A página da Steam diz que as ferramentas de mod são uma das grandes atualizações planejadas para o Early Access (que deve durar cerca de 1 ano).
- **Mesmo assim, já tem Workshop ativo**, com uns 40 mods. Eles usam o caminho "não oficial" clássico de jogo Unity:
  - **Harmony** (Workshop, autor Yibukii): biblioteca que "remenda" métodos .NET/Mono em tempo de execução. Isso indica que o jogo é **Unity com scripts em C#**, e os mods são **DLLs em C#** que alteram o código do jogo enquanto ele roda.
  - **UI Example** é descrito como "um exemplo oficial" de como modificar a interface, e depende do Harmony. Ou seja, o próprio dev já está apoiando esse formato.
  - Mods carregam **config em JSON** (ex.: Hegemony usa `v_common/config.json`), **sprites próprios** (animações em 8 direções, retratos 256px) e o jogo usa **Spine** para animação de personagens.
- Conclusão: hoje um mod = **DLL C# (Harmony) + arquivos de dados (JSON) + imagens/sprites**. Isso combina bem com desenvolver no GitHub.
- Atenção: na data da pesquisa vários itens do Workshop aparecem como "removidos" ou "incompatíveis", provavelmente por causa de atualizações do jogo. Em Early Access, patches do jogo podem quebrar mods com frequência.

## 2. O que a comunidade já fez (prova do que é possível)

| Mod | O que prova |
|---|---|
| Hegemony - Faction Diplomacy AI | Facções com guerra, alianças, conquista de sistemas, tributo, contratos de guerra. O jogo **já tem sistema de diplomacia escondido** (desativado por uma config faltando), e o mod liga ele. |
| Mech Expansion | Novas bancadas, cadeia de produção, **novos tipos de unidade** com sprites e animações próprias. |
| Klukai / Luvan - Travelling Merchant | **NPC novo** com diálogos, loja, arte própria, visitas periódicas, venda de naves. |
| Ship plugin buffs (舰船插件) | Alterar **módulos de nave** existentes (casco, reator, carga). |
| Life & Family, Dressing Table, CharacterCostume | Sistemas novos para personagens e customização visual. |
| Crematory, Robot Forge, Servant Forge | **Construções novas**. |
| Tradução PT-BR, Universal Translator, English Patch | Tradução e textos (seus mods). |
| Unlimited Stacking, BetterSpeedControl, FPS unlock, AI Optimizer | Ajustes de jogabilidade, desempenho e QoL. |

## 3. O grande mod: facção + raça + quests + naves

**Resposta curta: sim, é possível**, mas cada parte tem um nível diferente de dificuldade enquanto não houver ferramenta oficial.

| Parte | Viabilidade | Como seria |
|---|---|---|
| **Facção nova** | Alta | O jogo já tem facções (remanescentes, piratas, fortalezas) e diplomacia interna. Dá para registrar uma facção nova nos dados/estruturas que o jogo já usa (como o Hegemony faz), com nome, cor, bandeira, relações, territórios no mapa estelar e comportamento. |
| **Raça própria** | Média | O Mech Expansion mostra que dá para criar tipos de personagem novos com stats e sprites. Uma raça seria: atributos/genes próprios, sprites de 8 direções e retratos, nomes, talvez traços e necessidades. A parte mais trabalhosa é a **arte**, principalmente se precisar de rig Spine igual aos personagens do jogo. |
| **Quests** | Média | Os mods de mercador mostram diálogo, eventos periódicos e UI própria; o Hegemony mostra contratos postados em estações. Quests podem nascer como "contratos" e eventos com diálogo, objetivos (ir a um sistema, entregar itens, derrotar frota) e recompensa. Se o jogo tiver um sistema de missões em dados (ainda não confirmado), fica bem mais fácil. |
| **Modelos de naves** | Média a difícil | Módulos de nave já são alteráveis. Nave nova completa (casco, decks, sprites, armas) depende de como o jogo monta as naves de vários decks. É 2D pixel art, então "modelo" aqui é sprite/tiles, não 3D. |

**Ordem sugerida para o projeto:**
1. Mod "esqueleto" em C# com Harmony que carrega e aparece no jogo (base do repositório no GitHub).
2. Facção nova usando o sistema de diplomacia (mais garantido).
3. Naves da facção, começando por variações de módulos e cascos existentes com sprites novos.
4. Raça própria (stats primeiro, arte depois).
5. Linha de quests da facção (contratos e diálogos), ligando tudo.

## 4. Outras ideias de mods

- **Conteúdo:** novas armas e módulos de nave; novos planetas/biomas; novas construções e megaestruturas; novos inimigos e colmeias alienígenas; novas pesquisas na árvore de tecnologia; eventos aleatórios (naufrágios, sinais de socorro, chuva de meteoros).
- **Personagens:** NPCs recrutáveis com história; companheiros com diálogos; profissões/traços novos.
- **Sistemas:** reputação por facção com loja exclusiva; mercenários e caçadores de recompensa; frota de comércio automático entre seus sistemas; modo de cerco a estações.
- **QoL e UI:** painel de frota e logística; filtros de inventário; alertas melhores; tradução PT-BR contínua (você já tem).
- **Balanceamento:** dificuldade configurável, economia, velocidade de pesquisa.

## 5. Próximo passo para confirmar o formato

A pesquisa na web não mostra o formato exato dos dados (onde ficam facções, raças, naves e quests). Dá para descobrir inspecionando a pasta do jogo no seu PC (`C:\Program Files (x86)\Steam\steamapps\common\Outer Space`): ver se tem `*_Data/Managed/Assembly-CSharp.dll` (confirma Unity/C#), pastas de dados em JSON/XML, a pasta de mods e como os mods do Workshop baixados estão organizados. Isso responde de vez o que dá para fazer só com dados e o que exige código.

## Fontes

- [Página da Steam](https://store.steampowered.com/app/3080070/Order_The_New_Dawn/)
- [Workshop](https://steamcommunity.com/app/3080070/workshop/)
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=3803065949), [UI Example](https://steamcommunity.com/sharedfiles/filedetails/?id=3818436636), [Hegemony](https://steamcommunity.com/sharedfiles/filedetails/?id=3820324943), [Mech Expansion](https://steamcommunity.com/sharedfiles/filedetails/?id=3821493984), [Klukai Merchant](https://steamcommunity.com/sharedfiles/filedetails/?id=3821395485), [Ship plugins](https://steamcommunity.com/sharedfiles/filedetails/?id=3820508623)
