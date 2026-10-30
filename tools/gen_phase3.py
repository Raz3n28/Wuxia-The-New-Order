# Gera as linhas da Fase 3 (forjas, armas e armaduras do RimImmortal) do def.xml, os ícones
# e o português em defs.pt-BR.json.
# Uso: python3 -I gen_phase3.py <pasta 294100 do Workshop> <repo>
#   (a pasta tem 3308873789 = RimImmortal Forge e 3420195156 = RimImmortal Weapon Pack)
import sys, os, re, json
from xml.sax.saxutils import escape
from PIL import Image, ImageDraw

WS, REPO = sys.argv[1], sys.argv[2]
sys.path.insert(0, os.path.join(REPO, "tools"))
from phase3_ri import RI

TEX_OUT = os.path.join(REPO, "Mod/v_common/Resources/Textures")
DEF = os.path.join(REPO, "Mod/v_common/Resources/Def/def.xml")
PT_FILE = os.path.join(REPO, "Mod/v_common/Languages/defs.pt-BR.json")
PKG = "pauloraz3n.wuxia"
FORGE_TEX = os.path.join(WS, "3308873789/Textures/Things/Item/Resource")

lang = {}  # cn -> en, na ordem de criação
pt = {}    # en -> pt
def tr(cn, en, pt_text=None):
    if cn not in lang: lang[cn] = en
    if pt_text: pt[en] = pt_text
    return cn

# ---------------------------------------------------------------- forjas
# key, ID, modelo (B_Manufacture), CN, EN, PT, descCN, descEN, descPT, materiais
BENCHES = [
    ("energy", 96904, 130, "灵器锻炉", "Spirit Tool Forge", "Forja de Armas Espirituais",
     "以灵石温养炉火的锻炉，可以锻造凡兵、三品与二品灵器，以及庚钢、燧铜等炼器材料。",
     "A forge whose fire is fed with spirit stones. It makes mortal weapons, third- and second-rate spirit weapons and armor, and forging materials such as Gangium and Sol-bronze.",
     "Uma forja cujo fogo é alimentado com pedras espirituais. Faz armas mortais, armas e armaduras espirituais de 3º e 2º grau e materiais de forja como Aço Geng e Bronze Solar.",
     "12_6_20|12_16_20|12_96111_5"),
    ("spirit", 96905, 132, "神魂宝器锻炉", "Divine Soul Forge", "Forja da Alma Divina",
     "一座刻满阵纹的大型锻炉，能把神魂核心构件与古修残片炼入兵器，锻造一品宝器与古修遗物。",
     "A large forge covered in array runes. It binds spirit cores and ancient relics into weapons, forging first-rate treasures and ancient cultivator gear.",
     "Uma grande forja coberta de runas de formação. Funde núcleos espirituais e relíquias antigas nas armas, forjando tesouros de 1º grau e equipamentos dos cultivadores antigos.",
     "12_24_40|12_96117_20|12_96119_2|12_96111_20"),
]
BENCH_ID = {b[0]: b[1] for b in BENCHES}
BENCH_CN = {b[0]: b[3] for b in BENCHES}

# ---------------------------------------------------------------- materiais
# key, ID, CN, EN, PT, descCN, descEN, descPT, preço, textura (None = desenhada aqui)
MATS = [
    ("gangium", 96117, "庚钢", "Gangium", "Aço Geng",
     "以灵石反复锤炼钢材得到的金属，坚硬而能承载灵气，是锻造灵器的基础材料。",
     "Steel hammered again and again with spirit stones. Hard and able to hold Qi, it is the base material of spirit weapons.",
     "Aço martelado várias vezes com pedras espirituais. Duro e capaz de guardar Qi, é a base das armas espirituais.",
     120, "RI_Resource_BlackSteel/a.png"),
    ("sol_bronze", 96118, "燧铜", "Sol-bronze", "Bronze Solar",
     "掺入灵石粉末冶炼的青铜，温热不散，用来制作宝器构件。",
     "Bronze smelted with spirit stone dust. It stays warm and is used to make treasure components.",
     "Bronze fundido com pó de pedra espiritual. Fica sempre morno e serve para fazer componentes de tesouros.",
     90, "RI_Resource_YangBronze/a.png"),
    ("device_core", 96119, "宝器核心构件", "Treasure Core Component", "Componente de Núcleo de Tesouro",
     "刻有法阵的精密构件，二品灵器与精良护甲的核心。",
     "A precise component engraved with an array. It is the heart of second-rate weapons and fine armor.",
     "Um componente preciso gravado com uma formação. É o coração das armas de 2º grau e das armaduras finas.",
     600, "RI_Resource_DeviceCoreComponent/a.png"),
    ("spirit_core", 96120, "神魂核心构件", "Divine Soul Core", "Núcleo da Alma Divina",
     "以柔玉封存灵识的核心构件，只有一品宝器才配得上它。",
     "A core that seals a spark of spirit in liquid jade. Only first-rate treasures are worthy of it.",
     "Um núcleo que sela uma centelha de espírito em jade líquido. Só tesouros de 1º grau são dignos dele.",
     3000, "RI_Resource_SpiritCoreComponent/a.png"),
    ("relic", 96121, "古修残片", "Ancient Relic", "Relíquia Antiga",
     "仿照古代修炼者遗物重铸的残片，蕴含失传的炼器法门。",
     "A fragment recast after the relics of ancient cultivators. It holds lost forging techniques.",
     "Um fragmento refundido a partir das relíquias dos cultivadores antigos. Guarda técnicas de forja perdidas.",
     1500, "RI_Resource_AncientArmorRelic/a.png"),
    ("bamboo", 96122, "苍血竹", "Jade Blood Bamboo", "Bambu de Sangue de Jade",
     "一种能很好承载灵气的竹子，可以制作轻巧的灵器。",
     "A bamboo that carries Qi very well, used for light spirit weapons.",
     "Um bambu que conduz muito bem o Qi, usado em armas espirituais leves.",
     30, None),
]
MAT_ID = {m[0]: m[1] for m in MATS}

# Receitas dos materiais: ID, forja, material, quantidade, trabalho, ingredientes (tipo_ID_qtd)
MAT_RECIPES = [
    (96561, "energy", "gangium", 5, 15, "12_24_10|12_96111_2"),
    (96562, "energy", "sol_bronze", 5, 15, "12_15_10|12_96111_1"),
    (96563, "energy", "device_core", 1, 20, "12_96118_5|12_96111_5|12_25_2"),
    (96564, "energy", "bamboo", 10, 10, "12_10_10|12_96111_1"),
    (96565, "spirit", "spirit_core", 1, 30, "12_96119_2|12_96115_3|12_96111_10"),
    (96566, "spirit", "relic", 1, 30, "12_96117_10|12_96111_10|12_23_2|12_96112_1"),
]

# ---------------------------------------------------------------- itens
# Modelos do jogo: a linha copiada traz o visual na mão/no corpo, o som e o tipo de ataque.
WEAPON_TMPL = {
    "dagger": 10810, "sword": 90910, "spear": 11010, "polearm": 21010, "mace": 11110,
    "hammer": 91110, "axe": 91130, "musket": 10610, "rifle": 10310, "minigun": 40410,
    "grenade": 10710, "thrown": 10710, "cannon": 90720,
}
CLOTH_TMPL = {  # (ID, tem 4 qualidades)
    "head1": (12510, True), "head2": (22510, True), "head3": (42510, True), "hat": (20110, False),
    "body1": (12520, True), "body2": (22520, True), "body3": (42520, True), "coat": (20140, False),
}
# índice em RI -> key, modelo, EN, PT, descEN, descPT
ITEMS = {
    0: ("fire_dragon_helmet", "head3", "Candle Dragon Fire Helmet", "Elmo de Fogo do Dragão-Vela",
        "A wondrous helmet inspired by ancient cultivators and old myths. Very sturdy, it can breathe the Candle Dragon's fire from the treasure set on its brow.",
        "Um elmo maravilhoso inspirado nos cultivadores antigos e nos mitos. Muito resistente, solta o fogo do Dragão-Vela pelo tesouro na testa."),
    1: ("viper_helmet", "head3", "Xiangliu Venom Helmet", "Elmo Venenoso de Xiangliu",
        "A wondrous helmet inspired by ancient cultivators and old myths. It protects against poison.",
        "Um elmo maravilhoso inspirado nos cultivadores antigos e nos mitos. Protege contra veneno."),
    2: ("fire_dragon_armor", "body3", "Candle Dragon Scale Armor", "Armadura de Escamas do Dragão-Vela",
        "Mighty armor forged after ancient myths. A treasure built into it absorbs part of every blow.",
        "Uma armadura poderosa forjada a partir dos mitos antigos. Um tesouro embutido absorve parte de cada golpe."),
    3: ("viper_armor", "body3", "Xiangliu White Armor", "Armadura Branca de Xiangliu",
        "Armor forged after ancient myths. Its protection is modest, but it is so light it never slows the wearer.",
        "Armadura forjada a partir dos mitos antigos. Protege menos, mas é tão leve que nunca atrasa quem a veste."),
    4: ("ancient_soldier_helmet", "head2", "Ancient Cultivator Soldier Helmet", "Elmo de Soldado Cultivador Antigo",
        "A helmet of steel and spirit stones made by ancient cultivators. A paper-thin talisman inside guards the mind.",
        "Um elmo de aço e pedras espirituais feito pelos cultivadores antigos. Um talismã finíssimo por dentro protege a mente."),
    5: ("ancient_officer_helmet", "head2", "Ancient Cultivator Officer Helmet", "Elmo de Oficial Cultivador Antigo",
        "An officer's helmet of steel and spirit stones made by ancient cultivators.",
        "Um elmo de oficial de aço e pedras espirituais feito pelos cultivadores antigos."),
    6: ("ancient_sorcerer_mask", "head1", "Ancient Sorcerer Mask", "Máscara de Feiticeiro Antigo",
        "A mask worn by the sorcerers of the ancient cultivators.",
        "Uma máscara usada pelos feiticeiros dos cultivadores antigos."),
    7: ("jade_headband", "hat", "Liquid Jade Headband", "Faixa de Jade Líquido",
        "A headband with liquid jade set in the middle. It calms the spirit.",
        "Uma faixa com jade líquido no centro. Acalma o espírito."),
    8: ("forge_goggles", "hat", "Forging Goggles", "Óculos de Forja",
        "Goggles used by tool refiners. They help with fine crafting work.",
        "Óculos usados pelos refinadores de armas. Ajudam no trabalho fino de fabricação."),
    9: ("wuqu_hat", "hat", "Wuqu Pure Yang Hat", "Gorro Puro Yang de Wuqu",
        "A turban embroidered with gold wire and blessed by a charm. It keeps the mind clear.",
        "Um turbante bordado com fio de ouro e abençoado por um amuleto. Mantém a mente clara."),
    10: ("gilded_helmet", "head2", "Gilded Heavenly King Helmet", "Elmo Dourado do Rei Celestial",
         "A magnificent gilded helmet refined in the forge. It gives off a faint spiritual might.",
         "Um elmo dourado magnífico refinado na forja. Emana um leve poder espiritual."),
    11: ("gilded_helmet_mask", "head2", "Gilded Heavenly King Helmet (visor)", "Elmo Dourado do Rei Celestial (viseira)",
         "A magnificent gilded helmet refined in the forge, with a face guard.",
         "Um elmo dourado magnífico refinado na forja, com proteção para o rosto."),
    12: ("tiger_helmet", "head2", "Tiger Guard Helmet", "Elmo da Guarda do Tigre",
         "A helmet of refined iron engraved with a tiger head. It gives an imposing presence.",
         "Um elmo de ferro refinado gravado com uma cabeça de tigre. Dá uma presença imponente."),
    13: ("iron_cloak", "coat", "Hundred-Forged Iron Cloak", "Manto de Ferro Cem Vezes Forjado",
         "A cloak with a shoulder plate and thin plates sewn inside.",
         "Um manto com ombreira e placas finas costuradas por dentro."),
    14: ("bagua_robe", "coat", "Bagua Robe", "Manto Bagua",
         "A Taoist robe embroidered with the eight trigrams. It sharpens the mind for study.",
         "Um manto taoista bordado com os oito trigramas. Afia a mente para o estudo."),
    15: ("ancient_armor", "body3", "Ancient Cultivator Black Iron Armor", "Armadura de Ferro Negro do Cultivador Antigo",
         "Armor of steel and spirit stones made by ancient cultivators. Incredibly hard, yet light and easy to move in.",
         "Armadura de aço e pedras espirituais dos cultivadores antigos. Incrivelmente dura, mas leve e fácil de mover."),
    16: ("gilded_armor", "body2", "Gilded Heavenly King Armor", "Armadura Dourada do Rei Celestial",
         "Magnificent gilded armor refined in the forge. It gives off a faint spiritual might.",
         "Uma armadura dourada magnífica refinada na forja. Emana um leve poder espiritual."),
    17: ("tiger_armor", "body2", "Tiger Guard Armor", "Armadura da Guarda do Tigre",
         "Armor of refined iron engraved with a tiger head. It gives an imposing presence.",
         "Armadura de ferro refinado gravada com uma cabeça de tigre. Dá uma presença imponente."),
    18: ("ancient_fist", "dagger", "Ancient Power Fist", "Punho de Força Antigo",
         "Ancient cultivator knuckles that hit fast and hard.",
         "Soqueiras dos cultivadores antigos que batem rápido e forte."),
    19: ("ancient_dagger", "dagger", "Ancient Black Iron Dagger", "Adaga de Ferro Negro Antiga",
         "A black iron dagger of the ancient cultivators. Its energy light still flickers.",
         "Uma adaga de ferro negro dos cultivadores antigos. Sua luz de energia ainda tremula."),
    20: ("ancient_blade", "sword", "Ancient Black Iron Saber", "Sabre de Ferro Negro Antigo",
         "A black iron saber of the ancient cultivators, still razor sharp after untold years.",
         "Um sabre de ferro negro dos cultivadores antigos, ainda afiadíssimo depois de anos sem conta."),
    21: ("ancient_gun", "musket", "Black Iron Spirit Gun", "Arma Espiritual de Ferro Negro",
         "A dragon-headed fire gun that shoots burning pellets the size of cannonballs.",
         "Uma arma de fogo com cabeça de dragão que atira projéteis em brasa do tamanho de balas de canhão."),
    22: ("chiyou_halberd", "polearm", "Chiyou Wrath Flame Halberd", "Alabarda da Chama Furiosa de Chiyou",
         "A violent halberd that keeps spewing fire, inspired by Chiyou the war god.",
         "Uma alabarda violenta que não para de cuspir fogo, inspirada em Chiyou, o deus da guerra."),
    23: ("pojun_sword", "sword", "Pojun Sword", "Espada Pojun",
         "A replica of a famous sword of ancient times.",
         "Uma réplica de uma espada famosa dos tempos antigos."),
    24: ("immortal_slayer", "sword", "Immortal Slaying Sword", "Espada Matadora de Imortais",
         "A mysterious sword said to be the first weapon the celestials used against their own kind.",
         "Uma espada misteriosa, dita a primeira arma que os celestiais usaram contra os seus."),
    25: ("dragon_staff", "spear", "Coiling Dragon Heaven Staff", "Bastão do Dragão Enrolado",
         "A divine staff. Each blow shakes the earth and each sweep tears the air.",
         "Um bastão divino. Cada golpe faz a terra tremer e cada varrida rasga o ar."),
    26: ("pearl_umbrella", "sword", "Glazed Pearl Umbrella", "Sombrinha de Pérolas Vitrificadas",
         "Not a weapon of great destruction, but a treasure with healing power.",
         "Não é uma arma muito destrutiva, mas um tesouro com poder de cura."),
    27: ("thunder_minigun", "minigun", "Thunder Bead Gun", "Metralhadora do Trovão",
         "A bead gun that draws in thunder and fires charged bullets able to tear heavy armor.",
         "Uma metralhadora de contas que atrai o trovão e dispara balas carregadas capazes de rasgar armaduras pesadas."),
    28: ("frost_bow", "musket", "Frost Bloom Bow", "Arco da Flor de Geada",
         "A longbow made from the essence of heaven and earth. Its arrows carry a freezing cold.",
         "Um arco longo feito da essência do céu e da terra. Suas flechas trazem um frio congelante."),
    29: ("gilded_sword", "sword", "Gilded Dragon-Tiger Sword", "Espada Dourada Dragão-Tigre",
         "A luxurious gilded one-handed sword refined in the forge.",
         "Uma luxuosa espada dourada de uma mão refinada na forja."),
    30: ("gilded_axe", "axe", "Gilded Fishtail Axe", "Machado Dourado Rabo-de-Peixe",
         "A luxurious gilded fishtail axe refined in the forge.",
         "Um luxuoso machado dourado rabo-de-peixe refinado na forja."),
    31: ("gilded_mace", "hammer", "Gilded Octagonal Mace", "Maça Octogonal Dourada",
         "A luxurious gilded mace refined in the forge.",
         "Uma luxuosa maça dourada refinada na forja."),
    32: ("gilded_trident", "polearm", "Gilded Three-Pointed Spear", "Lança Dourada de Três Pontas",
         "A luxurious gilded three-pointed, double-edged spear refined in the forge.",
         "Uma luxuosa lança dourada de três pontas e dois gumes refinada na forja."),
    33: ("gilded_snake_spear", "polearm", "Gilded Serpent Spear", "Lança Serpente Dourada",
         "A luxurious gilded serpent spear refined in the forge.",
         "Uma luxuosa lança serpente dourada refinada na forja."),
    34: ("bamboo_sword", "sword", "Jade Bamboo Sword", "Espada de Bambu de Jade",
         "A light sword of jade blood bamboo and spirit stones that carries Qi well.",
         "Uma espada leve de bambu de sangue de jade e pedras espirituais que conduz bem o Qi."),
    35: ("bamboo_blade", "sword", "Jade Blood Saber", "Sabre de Sangue de Jade",
         "A light saber of jade blood bamboo and spirit stones that carries Qi well.",
         "Um sabre leve de bambu de sangue de jade e pedras espirituais que conduz bem o Qi."),
    36: ("bamboo_knives", "thrown", "Jade Bamboo Flying Knives", "Facas Voadoras de Bambu de Jade",
         "Throwing knives of jade blood bamboo, charged with Qi.",
         "Facas de arremesso de bambu de sangue de jade, carregadas de Qi."),
    37: ("bamboo_spear", "spear", "Jade Bamboo Spear", "Lança de Bambu de Jade",
         "A light spear of jade blood bamboo and spirit stones that carries Qi well.",
         "Uma lança leve de bambu de sangue de jade e pedras espirituais que conduz bem o Qi."),
    38: ("jumen_axe", "axe", "Jumen Axe", "Machado Jumen",
         "An axe of ancient divine soldiers, carved all over. It hums when Qi flows into it.",
         "Um machado dos antigos soldados divinos, todo entalhado. Zumbe quando o Qi flui nele."),
    39: ("poison_shuttle", "dagger", "Poison Heart Shuttle", "Lançadeira do Coração Venenoso",
         "A flying shuttle coated in strong poison, a terrible assassin's weapon.",
         "Uma lançadeira voadora coberta de veneno forte, uma terrível arma de assassino."),
    40: ("jade_hammer", "hammer", "Green Jade Hammer", "Martelo de Jade Verde",
         "A hammer that looks fragile but is solid as a single stone.",
         "Um martelo que parece frágil, mas é sólido como uma pedra só."),
    41: ("wind_demon_blade", "sword", "Wind Demon Saber", "Sabre do Demônio do Vento",
         "A great saber that unleashes the wind. Its veins throb faintly when swung.",
         "Um grande sabre que solta o vento. Suas veias pulsam de leve ao ser brandido."),
    42: ("thunder_lance", "polearm", "Thunder King Lance", "Lança do Rei do Trovão",
         "A long lance crackling with lightning, tamed to be more stable.",
         "Uma lança longa que estala com raios, domada para ser mais estável."),
    43: ("rat_whisker_sword", "sword", "Rat Whisker Sword", "Espada Bigode de Rato",
         "A sword as thin as a rat's whisker, hence its name.",
         "Uma espada fina como um bigode de rato, daí o nome."),
    44: ("dragon_cannon", "cannon", "Dragon Head Cannon", "Canhão Cabeça de Dragão",
         "A dragon-headed blaster that fires burning shells.",
         "Um canhão com cabeça de dragão que dispara projéteis em brasa."),
    45: ("fly_grenade", "grenade", "Fly Grenade", "Granada Mosca",
         "Small grenades that harass enemies like a swarm of flies.",
         "Pequenas granadas que atormentam o inimigo como um enxame de moscas."),
    46: ("ghost_crossbow", "rifle", "Ghost Fire Repeating Crossbow", "Besta de Repetição do Fogo-Fátuo",
         "A repeating crossbow with a ghost fire array that shoots short bolts with great force.",
         "Uma besta de repetição com uma formação de fogo-fátuo que dispara virotes curtos com muita força."),
    47: ("iron_balls", "thrown", "Iron Crow Balls", "Bolas de Ferro do Corvo",
         "Fist-sized iron balls that float and strike at their owner's will.",
         "Bolas de ferro do tamanho de um punho que flutuam e atacam pela vontade do dono."),
    48: ("bandage", "dagger", "Martial Artist Wraps", "Faixas de Artista Marcial",
         "Cloth wrapped around the fists to protect the fingers.",
         "Pano enrolado nos punhos para proteger os dedos."),
    49: ("gloves", "dagger", "Boxing Gloves", "Luvas de Boxe",
         "Heavy leather gloves. Their punches land like cannonballs.",
         "Luvas pesadas de couro. Os socos acertam como balas de canhão."),
    50: ("mystic_sword", "sword", "Mystic Sword", "Espada Mística",
         "A one-handed sword with a faint aura and a strong cutting power.",
         "Uma espada de uma mão com uma aura leve e grande poder de corte."),
    51: ("splitting_axe", "axe", "Mountain Splitting Axe", "Machado Racha-Montanha",
         "An axe with faint Qi that can even split stone.",
         "Um machado com Qi leve que racha até pedra."),
    52: ("wind_saber", "sword", "Wind Chasing Saber", "Sabre Caça-Vento",
         "A heavy two-handed saber with faint Qi, for brave fighters.",
         "Um sabre pesado de duas mãos com Qi leve, para lutadores corajosos."),
    53: ("rock_mace", "mace", "Dragon Rock Mace", "Maça de Rocha do Dragão",
         "A mace with faint Qi that smashes armor or at least stuns.",
         "Uma maça com Qi leve que esmaga armaduras ou ao menos atordoa."),
    54: ("heart_spear", "spear", "Heart Piercing Spear", "Lança Perfura-Coração",
         "A spear with faint Qi that strikes like an extension of the arm.",
         "Uma lança com Qi leve que golpeia como uma extensão do braço."),
    55: ("push_dagger", "dagger", "Push Dagger", "Punhal de Soco",
         "A small push dagger with faint Qi. Very fast and deadly.",
         "Um pequeno punhal de soco com Qi leve. Muito rápido e letal."),
    56: ("harquebus", "rifle", "Triple Shot Harquebus", "Arcabuz de Três Tiros",
         "An old harquebus of excellent material that fires three shots in a row.",
         "Um arcabuz antigo de material excelente que dispara três tiros seguidos."),
    57: ("eagle_musket", "musket", "Eagle Eye Musket", "Mosquete Olho de Águia",
         "A long range musket with spiritual bullets.",
         "Um mosquete de longo alcance com balas espirituais."),
    58: ("locust_bow", "musket", "Flying Locust Bow", "Arco do Gafanhoto Voador",
         "A light, flexible bow strengthened with Qi.",
         "Um arco leve e flexível reforçado com Qi."),
    59: ("throwing_darts", "thrown", "Throwing Darts", "Dardos de Arremesso",
         "Throwing knives set with spirit crystals. Many travelers carry a few.",
         "Facas de arremesso com cristais espirituais. Muitos viajantes levam algumas."),
    60: ("flail", "mace", "Flail", "Mangual",
         "A threshing tool of two sticks joined by a chain, also a fine striking weapon.",
         "Uma ferramenta de debulhar com dois paus unidos por corrente, também uma boa arma."),
    61: ("t_stick", "dagger", "Tonfa", "Tonfa",
         "A stick from farm tools, good both for attack and guard.",
         "Um bastão vindo das ferramentas da roça, bom para atacar e defender."),
    62: ("sword_breaker", "mace", "Jian Mace", "Maça Jian",
         "A four-sided blunt rod that can shatter swords.",
         "Uma barra rombuda de quatro lados capaz de quebrar espadas."),
    63: ("hand_fork", "dagger", "Hand Fork", "Garfo de Mão",
         "A short fork with side guards, good for blocking weapons.",
         "Um garfo curto com guardas laterais, bom para bloquear armas."),
    64: ("long_stick", "spear", "Long Staff", "Bastão Longo",
         "A plain long staff. In skilled hands it is devastating.",
         "Um bastão longo simples. Em mãos hábeis é devastador."),
    65: ("short_dao", "dagger", "Short Dao", "Dao Curto",
         "A simple short sword, a bit longer than a dagger.",
         "Uma espada curta simples, um pouco maior que uma adaga."),
    66: ("wood_chopper", "axe", "Wood Chopper", "Facão de Lenha",
         "A broad chopping blade for wood, kitchen and self defense.",
         "Uma lâmina larga para lenha, cozinha e defesa pessoal."),
    67: ("ring_dao", "sword", "Ring-Pommel Dao", "Dao de Pomo em Anel",
         "A traditional saber with a ring on the hilt.",
         "Um sabre tradicional com um anel no cabo."),
    68: ("tang_dao", "sword", "Tang Dao", "Dao Tang",
         "A finely made military saber, sharp enough to pierce armor.",
         "Um sabre militar bem feito, afiado o bastante para furar armaduras."),
    69: ("yanling_dao", "sword", "Yanling Dao", "Dao Yanling",
         "A curved saber shaped like a feather, loved by officers.",
         "Um sabre curvo em forma de pena, querido pelos oficiais."),
    70: ("liuye_dao", "dagger", "Willow Leaf Dao", "Dao Folha de Salgueiro",
         "A short curved blade like a willow leaf, extremely sharp.",
         "Uma lâmina curta e curva como folha de salgueiro, afiadíssima."),
    71: ("miao_dao", "sword", "Miao Dao", "Dao Miao",
         "A very long saber, deadly against unarmored foes.",
         "Um sabre muito longo, mortal contra inimigos sem armadura."),
    72: ("general_sword", "sword", "General's Sword", "Espada do General",
         "A beautiful longsword given to generals as a ceremonial gift.",
         "Uma bela espada longa dada aos generais como presente cerimonial."),
    73: ("hook_sword", "sword", "Hook Sword", "Espada-Gancho",
         "A strange weapon with a bladed guard and a hooked tip.",
         "Uma arma estranha com guarda afiada e ponta em gancho."),
    74: ("yue_axe", "axe", "Yue Axe", "Machado Yue",
         "An ancient two-handed broad axe, now mostly ceremonial.",
         "Um antigo machado largo de duas mãos, hoje mais cerimonial."),
    75: ("zhanma_dao", "axe", "Zhanma Dao", "Dao Zhanma",
         "A huge two-handed saber made to cut rider and horse in one stroke.",
         "Um enorme sabre de duas mãos feito para cortar cavaleiro e cavalo de uma vez."),
    76: ("guan_dao", "polearm", "Guan Dao", "Guan Dao",
         "A heavy long-handled saber that only mighty generals can wield.",
         "Um pesado sabre de cabo longo que só generais fortes conseguem usar."),
    77: ("halberd", "polearm", "Ji Halberd", "Alabarda Ji",
         "A halberd that both stabs and catches enemy weapons.",
         "Uma alabarda que estoca e prende as armas do inimigo."),
    78: ("heavy_lance", "spear", "Cavalry Lance", "Lança de Cavalaria",
         "The king of weapons, with terrifying piercing power.",
         "A rainha das armas, com um poder de perfuração assustador."),
    79: ("red_tassel_spear", "spear", "Red Tassel Spear", "Lança de Borla Vermelha",
         "A spear with red tassels that keep blood off the shaft.",
         "Uma lança com borlas vermelhas que impedem o sangue de escorrer na haste."),
    80: ("horn_fork", "spear", "Ox Horn Fork", "Forcado Chifre de Boi",
         "A farm fork shaped like an ox horn that keeps short weapons at bay.",
         "Um forcado em forma de chifre de boi que mantém armas curtas à distância."),
}

GRADES = {  # grau: (CN, EN, PT, preço base, nível de Craft)
    "一品": ("【一品】", "[1st Grade] ", "[1º Grau] ", 60000, 7),
    "二品": ("【二品】", "[2nd Grade] ", "[2º Grau] ", 15000, 5),
    "三品": ("【三品】", "[3rd Grade] ", "[3º Grau] ", 4000, 3),
    None: ("", "", "", 1200, 2),
}
QMULT = [1.0, 1.05, 1.17, 1.4]  # mesmo ganho das qualidades do jogo

# Ingrediente do RimWorld -> (tipo_ID do jogo, divisor, mínimo)
ING = {
    "Steel": ("12_24", 10, 2), "Plasteel": ("12_20", 5, 2), "Gold": ("12_23", 5, 1), "Silver": ("12_15", 5, 2),
    "Jade": ("12_96111", 10, 1), "Uranium": ("12_17", 10, 1), "ComponentIndustrial": ("12_25", 1, 1),
    "ComponentSpacer": ("12_33", 1, 1), "Cloth": ("12_1", 4, 2), "WoodLog": ("12_10", 4, 2),
    "RI_Resource_SpiritualCrystal": ("12_96111", 1, 1), "RI_Resource_LiquidJade": ("12_96115", 1, 1),
    "RI_Resource_BlackSteel": ("12_96117", 2, 1), "RI_Resource_YangBronze": ("12_96118", 1, 1),
    "RI_Resource_DeviceCoreComponent": ("12_96119", 1, 1), "RI_Resource_SpiritCoreComponent": ("12_96120", 1, 1),
    "RI_Resource_AncientWeaponRelic": ("12_96121", 1, 1), "RI_Resource_JadeBloodBamboo": ("12_96122", 4, 2),
    "RI_Talisman_Firefighter": ("12_96112", 1, 1), "RI_Talisman_ToxicBell": ("12_96112", 1, 1),
    "STUFF_Metallic": ("12_16", 4, 2), "STUFF_Fabric": ("12_1", 4, 2),
}
NO_RECIPE = [("RI_Resource_AncientWeaponRelic", 3), ("RI_Resource_BlackSteel", 20), ("RI_Resource_SpiritualCrystal", 10)]

# Bônus do RimWorld -> efeito do jogo (EffectEntries) e texto do painel
def effects(off):
    out = []
    for stat, v in off.items():
        if stat == "MoveSpeed": out.append(("3031", round(v / 4.6, 3), "移动速度", "Move speed", "Velocidade"))
        elif stat == "MeleeDodgeChance": out.append(("2021", round(v / 100, 3), "近战闪避率", "Melee dodge", "Esquiva corpo a corpo"))
        elif stat == "MeleeHitChance": out.append(("2011", round(v / 100, 3), "格斗伤害", "Melee damage", "Dano corpo a corpo"))
        elif stat == "PainShockThreshold": out.append(("3011", round(-v / 2, 3), "承受伤害", "Damage taken", "Dano recebido"))
        elif stat == "ResearchSpeed": out.append(("7020", v, "科研效率", "Research", "Pesquisa"))
        elif stat == "PlantWorkSpeed": out.append(("4000", v, "种植业效率", "Farming", "Plantio"))
        elif stat == "ButcheryFleshSpeed": out.append(("6000", v, "屠宰效率", "Butchering", "Abate"))
        elif stat == "TameAnimalChance": out.append(("5020", v, "驯服成功概率", "Taming", "Domar"))
        elif stat == "TrainAnimalChance": out.append(("5040", v, "训练成功概率", "Training", "Treinar"))
        elif stat == "MechBandwidth": out.append(("8000", 0.1, "制造效率", "Crafting", "Fabricação"))
    return out

def pct(v):
    return f"{'+' if v >= 0 else ''}{round(v * 100)}%"

def convert(ings):
    total = {}
    for k, n in ings:
        if k not in ING: continue  # Wastepack
        tid, div, mn = ING[k]
        total[tid] = total.get(tid, 0) + max(mn, round(n / div))
    return total

def mats_str(total, mult=1.0, floor=1):
    return "|".join(f"{k}_{max(floor, round(n * mult))}" for k, n in total.items())

# ---------------------------------------------------------------- XML
def item(name, value):
    return f'      <Item Name="{name}">{escape(str(value))}</Item>\n'

def row(table, depend, fields):
    s = f'    <ModDefRow Name="{table}" DependID="{depend}">\n'
    for k, v in fields: s += item(k, v)
    return s + "    </ModDefRow>\n"

out = ["\n    <!-- ===================== Fase 3: forjas, armas e armaduras do RimImmortal (gerado por tools/gen_phase3.py) ===================== -->\n"]

recipes = []      # (ID, forja, campos)
bench_of = {}
icons = []        # (origem, destino)

out.append("\n    <!-- Materiais de forja: cópias do 再生组织 (T_Material 92), como os da Fase 1. -->\n")
for key, mid, cn, en, ptn, dcn, den, dpt, price, tex in MATS:
    rec = next(r for r in MAT_RECIPES if r[2] == key)
    out.append(row("T_MaterialDef", 92, [
        ("ID", mid), ("Name", tr(cn, en, ptn)), ("Feature", tr("炼器材料", "Forging material", "Material de forja")),
        ("Desc", tr(dcn, den, dpt)), ("Obtain", BENCH_CN[rec[1]]), ("Price", price), ("D_value2", price),
        ("RawIcon", f"wuxia_{key}.png"),
    ]))
    icons.append((tex and os.path.join(FORGE_TEX, tex), f"wuxia_{key}.png"))
for rid, bench, key, cnt, wl, ings in MAT_RECIPES:
    m = next(x for x in MATS if x[0] == key)
    recipes.append((rid, bench, [
        ("ID", rid), ("SubType", 1), ("OverrideModuleId", -1), ("Materials", ings),
        ("OutMaterial", f"12_{m[1]}_{cnt}"), ("Workload", wl), ("Name", m[2]), ("Desc", m[5]),
        ("Apply", "True"), ("IsRandom", "False"), ("Craft", 2),
        ("Show_Toast", tr("分类：炼器材料", "Category: Forging materials", "Categoria: Materiais de forja")),
    ]))

out.append("\n    <!-- Armas: cada uma copia as 4 qualidades de uma arma do jogo (visual na mão, som, tipo de ataque)\n"
           "         e tem ClassID próprio, para a melhoria de qualidade achar a próxima. ID = 97000 + 10*n + qualidade-1.\n"
           "         Corpo a corpo: dano por golpe = 60 * (maior poder do RimWorld)^0.6 * intervalo do modelo.\n"
           "         Distância: dano por tiro escalado do RimWorld, alcance * 1.15 (máximo 60). -->\n")
wn = cn_ = 0
weapons, clothes = [], []
for idx in sorted(ITEMS):
    key, tmpl, en, ptn, den, dpt = ITEMS[idx]
    d = RI[idx]
    grade = next((g for g in ("一品", "二品", "三品") if g in d["cn"]), None)
    gcn, gen, gpt, gprice, craft = GRADES[grade]
    base_cn = re.sub(r"【.*?】", "", d["cn"]).strip()
    name = tr(gcn + base_cn, gen + en, gpt + ptn)
    desc = tr(d["cndesc"] or base_cn, den, dpt)
    uses_spirit = d["ing"] is None or any(k in ("RI_Resource_SpiritCoreComponent", "RI_Resource_AncientWeaponRelic") for k, _ in d["ing"])
    bench = "spirit" if grade == "一品" or uses_spirit else "energy"
    total = convert(d["ing"] if d["ing"] else NO_RECIPE)
    work = max(10, min(60, round(d["work"] / 1000))) if d["work"] else 30
    price = gprice + round((d["work"] or 20000) / 10)
    eff = effects(d["off"])
    icon = f"wuxia_{'w' if tmpl in WEAPON_TMPL else 'a'}_{key}.png"
    icons.append((os.path.join(WS, d["tex"]), icon))
    obtain = BENCH_CN[bench]
    if tmpl in WEAPON_TMPL:
        base = 97000 + 10 * wn; wn += 1
        weapons.append(base)
        t = WEAPON_TMPL[tmpl]
        ranged = tmpl in ("musket", "rifle", "minigun", "grenade", "thrown", "cannon")
        for q in range(4):
            m = QMULT[q]
            f = [("ID", base + q), ("ClassID", f"wuxia_{key}"), ("Name", name), ("Desc", desc),
                 ("Price", price), ("D_value6", price), ("Weight", d["mass"]), ("D_value5", d["mass"]),
                 ("RawIcon", icon), ("Obtain", obtain)]
            if eff: f.append(("AdditionEffect", "|".join(f"{e[0]}_{e[1]}" for e in eff)))
            if ranged:
                rng, burst, dmg, radius = d["verb"]
                r = min(60, round(rng * 1.15))
                if tmpl == "musket": atk, f2 = dmg * 4, [("DamageType", 0), ("Show_value5", "动能")]
                elif tmpl == "rifle": atk, f2 = dmg * 2, [("DamageType", 0), ("Show_value5", "动能"), ("Attack_Bullet", burst), ("Max_Bullet", burst * 2)]
                elif tmpl == "minigun": atk, f2 = round(dmg * 1.3), [("DamageType", 0), ("Show_value5", "动能")]
                elif tmpl == "cannon": atk, f2 = dmg * 5 * burst, []
                elif tmpl == "grenade": atk, f2 = 350, []
                else: atk, f2 = dmg * 5, [("DamageType", 0), ("DamageRange", 32), ("Show_value5", "动能")]
                atk = round(atk * m)
                f += f2 + [("ATK", atk), ("D_value2", atk), ("Range", r), ("Distance", round(r * 1.2)), ("D_value4", r)]
            else:
                interval = {"dagger": 0.5, "sword": 0.5, "spear": 0.5, "polearm": 0.5, "mace": 1.0, "hammer": 1.0, "axe": 1.0}[tmpl]
                mel = round(60 * max(8, d["power"]) ** 0.6 * interval * m)  # as faixas de punho do RimWorld têm poder 1-2
                f += [("Melee", mel), ("D_value2", mel)]
            up = mats_str(total, (0.5, 1, 2)[q]) if q < 3 else "-1"
            f += [("Upgrade_Materials", up), ("RepairMaterials", mats_str(total, (0.2, 0.3, 0.4, 0.5)[q]))]
            out.append(row("T_WeaponDef", t + q, f))
        outm = f"18_{base}_1"
    else:
        base = 98000 + 10 * cn_; cn_ += 1
        clothes.append(base)
        t, four = CLOTH_TMPL[tmpl]
        head = tmpl.startswith("head") or tmpl == "hat"
        defend = round(d["sharp"] * (25 if head else 35))
        eff_cn = "，".join(f"{e[2]}{pct(e[1])}" for e in eff) or "-"
        eff_en = ", ".join(f"{e[3]} {pct(e[1])}" for e in eff) or "-"
        eff_pt = ", ".join(f"{e[4]} {pct(e[1])}" for e in eff) or "-"
        eff_txt = tr(eff_cn, eff_en, eff_pt) if eff else "-"
        for q in range(4 if four else 1):
            dq = round(defend * QMULT[q])
            f = [("ID", base + q), ("ClassID", f"wuxia_{key}"), ("Name", name), ("Desc", desc),
                 ("Price", price), ("Show_value4", price), ("Weight", d["mass"]), ("D_value8", d["mass"]),
                 ("RawIcon", icon), ("Obtain", obtain), ("Defend", dq), ("Show_value6", dq), ("D_value3", dq),
                 ("AdditionEffect", "|".join(f"{e[0]}_{e[1]}" for e in eff) or "-1"), ("D_value9", eff_txt)]
            if not four: f.append(("DefendType", 1))  # o chapéu e o casaco do jogo não protegem (tipo 0)
            up = mats_str(total, (0.5, 1, 2)[q]) if four and q < 3 else "-1"
            f += [("Upgrade_Materials", up), ("RepairMaterials", mats_str(total, (0.2, 0.3, 0.4, 0.5)[q]))]
            out.append(row("T_ClothDef", t + q, f))
        outm = f"17_{base}_1"
    recipes.append((96601 + idx, bench, [
        ("ID", 96601 + idx), ("SubType", 1), ("OverrideModuleId", -1), ("Materials", mats_str(total)),
        ("OutMaterial", outm), ("Workload", work), ("Name", name), ("Desc", desc),
        ("Apply", "True"), ("IsRandom", "False"), ("Craft", craft),
        ("Show_Toast", tr("分类：兵器", "Category: Weapons", "Categoria: Armas") if tmpl in WEAPON_TMPL
         else tr("分类：护甲", "Category: Armor", "Categoria: Armaduras")),
    ]))

out.append("\n    <!-- Forjas: cópias do 锻造炉 (B_Manufacture 130, 3x1, a lenha) e do 近战武器流水线 (132, 3x3, energia).\n"
           "         Usam o modelo e o ícone do jogo. Sem caixa ao desmontar. -->\n")
for key, bid, tmpl, cn, en, ptn, dcn, den, dpt, mats in BENCHES:
    out.append(row("F_ManufactureDef", tmpl, [
        ("ID", bid), ("Name", tr(cn, en, ptn)), ("Desc", tr(dcn, den, dpt)), ("Materials", mats),
        ("Reinstall", 0), ("Uninstall", -1),
        ("ProductID", "|".join(f"{PKG}_{rid}" for rid, b, _ in recipes if b == key)),
    ]))

out.append("\n    <!-- Receitas das forjas -->\n")
for rid, bench, f in recipes:
    out.append(row("F_ProductListDef", -1, f))

out.append("\n    <!-- Textos em inglês da Fase 3 -->\n")
for cn, en in lang.items():
    out.append('    <ModDefRow Name="LanguageDef">\n' + item("Key", "id_" + cn) + item("CN", cn) + item("EN", en) + "    </ModDefRow>\n")
out.append("    <!-- ===================== fim da Fase 3 ===================== -->\n")

src = open(DEF, encoding="utf-8").read()
src = re.sub(r"\n    <!-- =+ Fase 3:.*?fim da Fase 3 =+ -->\n", "", src, flags=re.S)
src = src.replace("  </rows>", "".join(out) + "  </rows>")
open(DEF, "w", encoding="utf-8").write(src)

# ---------------------------------------------------------------- português
ptj = json.load(open(PT_FILE, encoding="utf-8"))
ptj.update(pt)
with open(PT_FILE, "w", encoding="utf-8") as fh:
    json.dump(ptj, fh, ensure_ascii=False, indent=2)
    fh.write("\n")

# ---------------------------------------------------------------- ícones (64x64, recortados)
def fit(img, w, h):
    img = img.convert("RGBA")
    bbox = img.getbbox()
    if bbox: img = img.crop(bbox)
    img.thumbnail((w, h), Image.LANCZOS)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.paste(img, ((w - img.width) // 2, (h - img.height) // 2), img)
    return canvas

def bamboo():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    g = ImageDraw.Draw(img)
    for x, top in ((14, 6), (28, 2), (42, 8)):
        g.rounded_rectangle((x, top, x + 9, 60), 3, fill=(46, 120, 74, 255), outline=(20, 60, 36, 255))
        g.line((x + 3, top + 3, x + 3, 57), fill=(120, 196, 130, 255), width=2)
        for y in range(top + 14, 58, 14):
            g.line((x, y, x + 9, y), fill=(150, 30, 40, 255), width=2)
    return img

for src_path, dst in icons:
    dst_path = os.path.join(TEX_OUT, dst)
    if src_path is None:
        bamboo().save(dst_path)
    elif os.path.exists(src_path):
        fit(Image.open(src_path), 64, 64).save(dst_path)
    elif not os.path.exists(dst_path):
        print("faltou a imagem", src_path)

print(f"{len(weapons)} armas, {len(clothes)} roupas, {len(recipes)} receitas, {len(lang)} textos")
