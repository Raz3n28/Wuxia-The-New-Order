# Mapa de conversão: RimImmortal → Wuxia (Order: The New Dawn)

Levantado em 19/10/2026 a partir dos 10 mods da coleção RimImmortal (versão 1.7; o Chinese Weapon Pack é 1.6). Foram lidos 486 arquivos XML e 2.687 definições; a contagem completa está em `ri_inventory.json` (pasta de rascunho da sessão).

## Como ler

- 🟢 **Direto:** dá para gerar automaticamente com um script (XML do RimWorld → `def.xml` deste jogo). São nome, descrição, imagem, números e receitas.
- 🟡 **Adapta:** vira uma cópia de algo que já existe neste jogo (com `DependID`), com a sua imagem e os seus números. Precisa escolher o modelo certo e testar o tamanho e o comportamento.
- 🔴 **Refaz:** depende de código do RimWorld (C#). A ideia continua, mas o código é escrito do zero no nosso mod.
- ⚪ **Não se aplica:** não existe nada parecido neste jogo, ou não vale a pena.

As tabelas do jogo que vamos usar já foram encontradas no código dele: `T_MaterialDef` (materiais), `T_FoodDef` (comida), `T_MedicineDef` (remédios e pílulas), `T_WeaponDef` (armas), `T_ClothDef` (roupas e armaduras), `T_BodyPartsDef` (próteses), `F_ManufactureDef` + `F_ProductListDef` (bancadas e receitas), `F_FurnitureDef`, `F_DecroDef` (decoração), `F_Entertainment` (lazer), `F_DefenseDef` (torres), `Map_PlantDef` (plantas), `AnimalDef`, `MonsterDef`/`EnemyNormalDef` (inimigos), `TechnologyDef` (pesquisa), `Role_MoodPartDef` (humor), `Role_DiseaseDef` (estados no corpo), `CharacterAppearanceDef` (aparência), `WeatherDef` (clima) e `LanguageDef` (textos).

## Resumo por mod

| Mod | O que tem | Classificação |
|---|---|---|
| **Core** | Cultivo (11 estágios, raiz espiritual, ruptura), 64 itens (pílulas, livros, ervas), 25 construções, 42 habilidades, 99 estados no corpo, 28 tipos de NPC, 18 eventos, 21 traços | 🟢 itens, pílulas e receitas · 🟡 construções · 🔴 cultivo, habilidades, estados, eventos e IA |
| **Living** | 26 materiais (seda, tijolo, aço padronizado, sândalo, bambu), 38 receitas, 16 construções (bicho-da-seda, forno, mesas de jogo), 19 roupas, 10 ferramentas, 10 plantas, 4 animais | 🟢 materiais e receitas · 🟡 bancadas, mesas de jogo e roupas · 🟡 plantas e animais |
| **Forge** | 56 armas (1ª, 2ª e 3ª categoria), 36 roupas e armaduras, 52 itens (próteses de jade e aço negro, órgãos dos 5 elementos, talismãs), 46 construções (torres, móveis antigos), 18 golens e bonecos mecânicos, 29 habilidades | 🟢 armas, armaduras e próteses · 🟡 torres e móveis · 🔴 habilidades de armas, bonecos mecânicos e ruínas |
| **FiveElements** | 5 caminhos elementais com 33 habilidades, 5 pedras elementais, 5 livros, 12 construções (paredes de terra, plantas atiradoras) | 🟢 pedras e livros · 🔴 habilidades e caminhos (entram na nossa árvore de habilidades) |
| **Faction** | 2 seitas (Justiça e Mal), 6 títulos de seita, 15 tipos de membro, 20 favores de seita, 19 itens (livros de técnica, pílulas, vinho), 10 comerciantes | 🟢 itens e pílulas · 🔴 seitas, títulos e favores (sistema nosso) · ⚪ ideologia e memes |
| **Hairstyle** | 29 cabelos, 10 barbas, 8 tatuagens | 🟡 se `CharacterAppearanceDef` aceitar imagens novas (a testar) |
| **Furniture** | 53 móveis e decorações (mesas Ming, lanternas, tapetes, trono), 48 pisos | 🟡 móveis via `F_FurnitureDef`/`F_DecroDef` · 🟡 pisos a testar |
| **ChineseWeaponPack** | 21 armas brancas chinesas (dao, lança, alabarda) | 🟢 armas via `T_WeaponDef` |
| **Biome** | 10 plantas, 7 itens, 6 animais, 2 biomas, 2 climas, droga Miragold | 🟢 itens · 🟡 plantas, animais e clima · ⚪ biomas (o mapa deste jogo é outro) |
| **Farmcraft** | 92 comidas chinesas, 126 receitas, 25 construções (fogão, varal, barril de vinho), 12 plantas, 21 efeitos de chá e vinho, 26 humores | 🟢 comidas e receitas · 🟡 bancadas e plantas · 🟡 efeitos via humor |

## Detalhe por tipo

### 🟢 Direto (script)

- **Pílulas e elixires:** Core (3ª, 2ª e 1ª categoria: Pílula de Energia, Pílula LingYun, Elixir de Fundação etc.), Faction e Biome. Viram linhas em `T_MedicineDef` com a imagem e o efeito descrito. O efeito em si (Qi, chance de ruptura) é ligado no nosso código.
- **Materiais:** Living (seda, seda de gelo, fio de ouro e de prata, aço padronizado, sândalo, bambu, tijolos), Forge (gangium, bronze solar, relíquias) e as pedras dos 5 elementos. Viram linhas em `T_MaterialDef`.
- **Comidas:** Farmcraft, com 92 pratos, sobremesas e conservas. Viram linhas em `T_FoodDef`.
- **Armas:** as 56 do Forge e as 21 do Weapon Pack. Viram linhas em `T_WeaponDef`, com dano, alcance e velocidade convertidos. As habilidades especiais de algumas armas ficam para o 🔴.
- **Roupas e armaduras:** Core, Living, Forge e Faction. Viram linhas em `T_ClothDef`.
- **Próteses:** braço, olho e mão de jade, membros de aço negro e órgãos dos 5 elementos (Forge). Viram linhas em `T_BodyPartsDef`.
- **Receitas:** cerca de 400 no total. Viram linhas em `F_ProductListDef`, ligadas às bancadas convertidas.
- **Pesquisas:** cerca de 80. Viram linhas em `TechnologyDef`, liberando o que cada pesquisa libera no RimWorld.
- **Textos:** nomes e descrições em inglês saem direto. O chinês vem das traduções que já estão nos seus mods.

### 🟡 Adapta (cópia de algo do jogo)

- **Bancadas:** Caldeirão, Caldeirão TaiYi, Bancada de Elixires, forno, fogão, varais, barris, talismãs e mesa de cópia de livros. Viram cópias de bancadas do jogo (`F_ManufactureDef`), como fizemos com a almofada.
- **Lugares de cultivo:** Ponto de Cultivo, Almofada (feita), Plataforma de Cultivo, Ponto e Plataforma de Ruptura, Plataforma de Ascensão e Rocha Espiritual. Viram lazer (`F_Entertainment`) ou móveis com efeito no nosso código.
- **Móveis e decoração:** Furniture e Forge. Usam `F_FurnitureDef` e `F_DecroDef`.
- **Mesas de jogo:** xadrez chinês, WeiQi, mahjong, pitch-pot e cartas (Living). Viram lazer.
- **Torres:** "Grou que Grita", "Baleia que Engole", "Dragão da Enchente", Torre de Pedra e plantas atiradoras. Viram `F_DefenseDef`.
- **Plantas e árvores:** ervas medicinais, chá, frutas e árvores especiais. Usam `Map_PlantDef` (a testar).
- **Animais:** JueRu, JunGou, rato de fogo, cão Tian, búfalo, tartaruga Xuan e gato Lei. Usam `AnimalDef` (a testar).
- **Humores:** comeu algo bom, ganhou nível etc. Usam `Role_MoodPartDef`, como o mod HopeMeat faz.
- **Aparência:** cabelos, barbas e tatuagens, a testar.

### 🔴 Refaz (código nosso)

- **Sistema de cultivo:** os 11 estágios do Core, raiz espiritual, chance de ruptura, Desvio de Qi e Ascensão. Já começamos: raiz, estágios, Qi, despertar e meditação. Falta adotar os números do Core.
- **Caminhos e árvore de habilidades:** os caminhos do Core (Corpo, Medicina, Núcleo Interno), o Forja (Forge) e os 5 elementos (FiveElements). Viram a nossa árvore de habilidades por pontos.
- **Habilidades:** 42 no Core, 29 no Forge, 33 no FiveElements e 7 no Faction. Precisam de código próprio, uma por uma ou por família. É a parte mais trabalhosa.
- **Estados no corpo (hediffs):** cerca de 200, entre efeitos de pílulas, chá, vinho, dano e técnicas. Os simples viram bônus de atributo e os complexos são código.
- **Traços de nascença:** meridianos fortes, Shennong, Iluminação etc. Viram traços nossos ligados à raiz espiritual.
- **Seitas e títulos:** discípulo externo, interno e direto, ancião, mestre, e os favores de seita. É um sistema nosso, em cima das facções do jogo.
- **Inimigos:** cultivadores do mal, assassinos, louco da espada, golens e bonecos mecânicos. Usam `EnemyNormalDef`/`MonsterDef` mais o nosso código para dar estágio de cultivo a eles.
- **Eventos:** maré de Qi alta e baixa, cultivador cai do céu, ervas brotam, ataques de cultivadores. Usam `DisasterDef`/`EventInvadeDef` mais código.
- **Missões:** ruínas antigas, túmulo abandonado, mendigos. O jogo tem um sistema de missões (`TK_*`) a investigar.

### ⚪ Não se aplica

Ideologia e memes, estilos de ideologia, narradores (storytellers), cenários, biomas, efeitos visuais do RimWorld (motes e flecks), sons e atalhos de teclado.

## Ordem sugerida

1. **Pílulas, materiais e receitas do Core**, com o Caldeirão e a Bancada de Elixires. Isso já dá alquimia funcionando e serve de teste para o script de conversão.
2. **Números do cultivo do Core** no nosso sistema (Qi por estágio, chance de ruptura, Desvio de Qi), mais a Plataforma de Ruptura.
3. **Armas, armaduras e próteses** (Forge e Weapon Pack): muito conteúdo por pouco código.
4. **Living e Farmcraft:** materiais, comidas, bancadas e plantas.
5. **Furniture:** móveis e pisos.
6. **Árvore de habilidades** com os caminhos (Core, Forge, 5 elementos), começando pelas passivas e depois as habilidades ativas.
7. **Inimigos, eventos, seitas e missões.**

## O que ainda precisa ser testado no jogo

- Se `T_MedicineDef`, `T_WeaponDef`, `T_ClothDef` e `Map_PlantDef` aceitam cópia com `DependID` como as bancadas aceitam.
- O tamanho das imagens. No mapa a imagem aparece no tamanho real; a almofada de 128 px ocupou cerca de 3 quadrados.
- Se cabelos e barbas novos funcionam pela tabela de aparência.
