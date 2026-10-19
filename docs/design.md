# Mod Wuxia (rework) para Order: The New Dawn

> Fase de análise, 19/10/2026. Baseado na pesquisa do Workshop (ver [pesquisa-modding.md](pesquisa-modding.md)). Nada foi testado nos arquivos do jogo ainda.

## 1. Viabilidade de cada sistema

Legenda: 🟢 os mods atuais já provam que dá | 🟡 dá, mas exige código C# considerável | 🔴 arriscado até ver os arquivos do jogo

| Sistema | Viab. | Como seria feito | O que é difícil |
|---|---|---|---|
| **Itens próprios** (pílulas, armas, talismãs, manuais) | 🟢 | Mods como Mech Expansion e os mercadores já adicionam itens, receitas e bancadas. Itens novos com ícone, stats e receita. | Desenhar os ícones em pixel art. Itens com efeito especial (ex.: pílula que sobe de nível) precisam de código. |
| **Menu próprio na tela de Personagem** (botão que abre a tela de cultivo) | 🟡 | Harmony em C# para injetar um botão na tela do personagem e abrir uma janela própria. O "UI Example" oficial existe justamente para isso, e o mercador Klukai já tem UI própria. | Desenhar a árvore visual (nós, linhas, tooltips) do zero. Cada patch do jogo pode mudar a UI e quebrar o botão. |
| **Árvore de habilidades + passivas por pontos** | 🟡 | Dados em JSON (nó, custo, pré-requisito, efeito). Pontos ganhos ao subir de estágio ou nível. Passivas mexem em stats que o jogo já tem (velocidade de trabalho, dano, saúde). | Habilidades **ativas** (golpes, voo, técnicas de espada) exigem mexer no combate do jogo. Esse é o ponto mais incerto. |
| **12 estágios de cultivo** com subníveis | 🟢/🟡 | Cada personagem guarda estágio, subnível e Qi acumulado (salvo junto com o save, como o Hegemony faz). Cada estágio dá multiplicadores de stats. Meditação vira uma "tarefa" que acumula Qi. | Fazer o personagem **meditar sozinho** como um trabalho da colônia exige integrar com a IA de tarefas do jogo. |
| **Aptidão** (só alguns cultivam) | 🟢 | Um traço/atributo sorteado quando o personagem nasce ou é recrutado: Sem Raiz, Raiz Comum, Raiz Espiritual, Raiz Celestial. Pode depender dos **genes**, que já existem no jogo (o mod Klukai vende DNA). | Pouco. Só balancear. |
| **Alquimia** | 🟢/🟡 | Bancada "Fornalha de Pílulas" com receitas próprias. O básico é uma bancada de produção normal. Para ficar com cara de wuxia: chance de falha, qualidade da pílula (Inferior → Divina) e bônus pelo estágio do alquimista. | Chance de falha e qualidade são código além do sistema padrão de produção. |
| **Forja própria** | 🟢/🟡 | Mesma lógica: "Forja Espiritual" que cria armas com grau (Mortal, Espiritual, Tesouro, Imortal) e permite "nutrir" a arma com Qi. | Armas que evoluem precisam de código. |
| **Tribulação Celestial** (raios) | 🟡 | Evento ao tentar romper para o estágio 8: tempestade de raios sobre o personagem, com chance de morte e bônus se sobreviver. | Efeito visual e dano em área. Dá para começar com algo simples (dano + efeito na tela). |
| **Alma Nascente** (sobrevive se o corpo morrer) | 🔴 | Ao morrer, o personagem vira uma "alma" que pode ser reencarnada num corpo novo (o jogo tem cápsulas de reviver com DNA, dá para aproveitar). | Interceptar a morte com segurança sem quebrar saves. |
| **Seitas como facções** | 🟢 | Usando o sistema de diplomacia escondido do jogo (o mesmo do mod Hegemony). | Integrar com o mod Hegemony, se o jogador usar os dois. |
| **Inimigos novos** (no mundo atual, sem rework) | 🟢/🟡 | Novos tipos de inimigo reaproveitando a IA de combate dos inimigos que já existem (piratas, colmeias), com stats, sprites e drops próprios (núcleos de besta, manuais). Aparecem por eventos, ataques e pontos de interesse do mapa atual. | Sprites em 8 direções. Comportamento diferente do vanilla (ex.: chefe com técnicas) exige código. |

**Resumo:** o mundo do jogo fica como está; o mod só adiciona inimigos novos. Itens, aptidão, estágios, alquimia, forja, seitas e inimigos são bem viáveis. O menu próprio com árvore é viável mas trabalhoso. Habilidades ativas de combate e a Alma Nascente são as partes de maior risco.

## 2. Ideia central: "Cultivo Estelar"

O jogo é ficção científica, então em vez de brigar com isso dá para fundir os dois. Sugestão de lore:

> A Fenda Inicial não é só um rasgo no espaço: ela vaza **Qi Estelar**, a mesma "energia radiante" que alimenta as naves. Alguns sobreviventes de New Terra nascem com **raízes espirituais** que absorvem essa energia. Antigas seitas do Reino Terra sobreviveram escondidas e disputam os sistemas estelares.

Isso aproveita coisas que o jogo já tem: energia radiante nas naves, genes/DNA, facções remanescentes e colmeias alienígenas.

## 3. Seitas e facções

| Seita | Caminho | Estilo | Relação |
|---|---|---|---|
| **Seita da Espada Celeste** | Espada voadora | Justa, orgulhosa, forte em combate | Aliada possível; odeia demônios |
| **Vale das Mil Ervas** | Alquimia e cura | Pacífica, comerciante de pílulas | Neutra, ótima para comércio |
| **Pavilhão da Forja Estelar** | Forja e artefatos | Mistura cultivo com tecnologia das naves | Vende armas e módulos de nave espirituais |
| **Culto do Lótus Sangrento** | Cultivo demoníaco | Rouba Qi dos outros, faz rapto | Inimiga principal; ataca colônias |
| **Clã dos Nove Dragões** | Linhagem de sangue | Família nobre, aptidão herdada | Casamentos e alianças políticas |
| **Templo do Vazio** | Budista / Vazio | Isolados, guardam segredos dos estágios altos | Libera o estágio 6+ via quest |
| **Colmeia Devoradora** | Bestas espirituais alienígenas | As colmeias do jogo viram bestas com núcleos | Fonte de núcleos de besta para alquimia |

## 4. Personagem: aptidão, estágios e pontos

- **Raízes espirituais:** Sem Raiz (não cultiva, 60%), Raiz Mista (lenta, 25%), Raiz Única de elemento (Fogo, Água, Metal, Madeira, Terra) (12%), Raiz Celestial/Trovão/Vazio (rara, 3%). Elemento define bônus na árvore.
- **Qi:** acumulado meditando, tomando pílulas e em locais especiais (veios de Qi em planetas, perto da Fenda).
- **Gargalos:** no fim de cada estágio, precisa de uma pílula de ruptura, item raro ou quest. Falhar dá "Desvio de Qi" (ferimento ou perda de nível).
- **Pontos de Cultivo:** ganhos por subnível. Gastos na árvore de habilidades e em passivas.
- **Expectativa de vida:** cada estágio aumenta a vida, o que dá peso aos estágios imortais.
- **Cultivo duplo:** um casal de cultivadores medita junto e os dois ganham Qi extra (mais se as raízes combinarem, ex.: Fogo + Madeira). Pode usar os relacionamentos do mod [Life & Family](https://steamcommunity.com/sharedfiles/filedetails/?id=3821030513) quando ele estiver ativo, ou os do próprio jogo.

**Árvore (3 ramos + passivas):**
- **Corpo** (Refinamento corporal): mais saúde, resistência, força de carga, imunidade a doenças.
- **Espírito** (Sentido divino): pesquisa mais rápida, detectar inimigos, comandar mais naves.
- **Caminho Marcial** (técnicas): espada voadora, palma de fogo, passos leves (velocidade).
- **Passivas** (só com pontos, poucos slots): "Corpo de Jade", "Mente Serena" (sem surtos mentais), "Coração da Forja", "Mão Alquímica", "Afinidade Estelar" (bônus em naves).

## 5. Itens

- **Pílulas:** Pílula de Condensação de Qi, Pílula de Fundação, Pílula do Núcleo Dourado, Pílula de Cura de Meridianos, Pílula de Longevidade.
- **Materiais:** ervas espirituais (cultiváveis em estufas), núcleos de besta (das colmeias), pedras espirituais (moeda das seitas e combustível), Cristal da Fenda.
- **Armas e artefatos:** espada voadora, leque de vento, sino de proteção, talismãs de papel (descartáveis), anéis de armazenamento (aumentam inventário).
- **Manuais de técnica:** item que desbloqueia um ramo ou nó da árvore.
- **Nave:** módulo "Formação de Matriz" para naves (escudo ou dano com pedras espirituais). Mais à frente, uma "Arca Espiritual" como nave da seita.

## 6. Quests (exemplos)

1. **O Despertar:** um colono sente o Qi da Fenda. Introduz meditação e o primeiro estágio.
2. **A Erva do Deserto:** buscar uma erva espiritual num ponto de New Terra. Libera a Fornalha de Pílulas.
3. **Exame de Entrada da Seita:** levar um colono com raiz espiritual até a Seita da Espada Celeste. Relação e manuais.
4. **Ruína do Reino Terra:** explorar uma estação abandonada com uma herança de um cultivador antigo.
5. **Ataque do Lótus Sangrento:** defender a colônia de cultivadores demoníacos que querem raptar colonos com aptidão.
6. **A Tribulação:** preparar para-raios, pílulas e formação de proteção antes do estágio 8.
7. **Ascensão:** final de campanha, abrir um portal para o "Reino Imortal" (um sistema estelar escondido).

## 7. Inimigos novos

Todos entram no mundo atual, por ataques à colônia, eventos ou locais que já existem no mapa.

| Inimigo | Tipo | Onde aparece | Drop |
|---|---|---|---|
| **Discípulo do Lótus Sangrento** | Cultivador demoníaco humano (Refinamento de Qi) | Ataques à colônia | Pedras espirituais, manuais demoníacos |
| **Ancião do Lótus Sangrento** | Chefe (Núcleo Dourado), drena Qi | Fim da quest de ataque | Núcleo demoníaco, arma de grau Tesouro |
| **Lobo da Fenda** | Besta espiritual em matilha | Desertos e tundras de New Terra | Núcleo de besta comum, pele espiritual |
| **Serpente de Areia Ancestral** | Besta grande, chefe de região | Desertos | Núcleo de besta raro (alquimia alta) |
| **Colmeia Devoradora Desperta** | Variante das colmeias alienígenas do jogo, absorveu Qi | Ninhos de colmeia existentes | Núcleos de colmeia, Cristal da Fenda |
| **Cadáver Refinado (Jiangshi)** | Morto-vivo controlado por cultivador | Ruínas e estações abandonadas | Talismãs, ervas raras |
| **Fantasma de Cultivador Antigo** | Espírito, ignora armadura | Ruínas do Reino Terra | Herança (manual raro) |
| **Fera da Tribulação** | Criatura de raio | Só durante a Tribulação Celestial | Essência de trovão |
| **Nave do Lótus Sangrento** | Inimigo espacial (casco existente com módulos novos) | Rotas entre sistemas | Módulos de Formação de Matriz |

## 8. Por onde começar (ordem sugerida)

1. **Fase 1, base:** aptidão (raízes), Qi e os 3 primeiros estágios só com bônus de stats. Uma pílula e uma Fornalha simples.
2. **Fase 2, UI:** botão na tela do personagem e a janela de cultivo (estágio, barra de Qi, árvore).
3. **Fase 3, conteúdo:** alquimia com qualidade, forja, ervas, núcleos de besta, as primeiras seitas.
4. **Fase 4, quests, eventos e inimigos novos** (Lótus Sangrento, bestas espirituais, Tribulação).
5. **Fase 5, estágios altos** e sistemas arriscados (Alma Nascente, habilidades ativas, Reino Imortal).

Antes da fase 1 vale inspecionar a pasta do jogo para confirmar como personagens, itens e receitas são definidos.
