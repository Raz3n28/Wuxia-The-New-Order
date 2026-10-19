# Gera as linhas da Fase 1 (alquimia) do def.xml e os ícones.
# Uso: python3 -I gen_phase1.py <pasta do RimImmortal> <repo>
import sys, os, re
from xml.sax.saxutils import escape
from PIL import Image, ImageOps, ImageEnhance

RI, REPO = sys.argv[1], sys.argv[2]
TEX_OUT = os.path.join(REPO, "Mod/v_common/Resources/Textures")
DEF = os.path.join(REPO, "Mod/v_common/Resources/Def/def.xml")
PKG = "pauloraz3n.wuxia"
RITEX = os.path.join(RI, "Textures/Things")

# ---------------------------------------------------------------- bancadas
# key, ID, CN, EN, descCN, descEN, w, h, materials, texture, tint
BENCHES = [
    ("little", 96901, "小型炼丹炉", "Small Alchemy Cauldron",
     "一个方便携带的小型炼丹炉，可以炼制三品丹药。更高品阶的丹药需要太乙炼丹炉。",
     "A small, portable alchemy cauldron for refining third-grade pills. Higher-grade pills need the Taiyi Cauldron.",
     1, 1, "12_15_20|12_6_10", "Building/Production/Cauldrons/RI_Building_LittleCauldrons_south.png", True),
    ("taiyi", 96902, "太乙炼丹炉", "Taiyi Alchemy Cauldron",
     "一台大型标准炼丹炉，可以炼制各品丹药，包括效果极强的一品丹药和丹魄真火。",
     "A large standard alchemy cauldron. It refines pills of every grade, including the potent first-grade elixirs and Spirit Fire.",
     3, 3, "12_15_80|12_6_30|12_23_1", "Building/Production/Cauldrons/RI_Building_Cauldrons_south.png", True),
    ("bench", 96903, "制药工作台", "Elixir Workbench",
     "一张用来制备药材和药粉的工作台：丹辅、金砂、灵石、各种药材和药散都在这里制作。",
     "A workbench for preparing herbs and powders: pill base, gold sand, spirit stones, medicinal herbs and powders are made here.",
     3, 1, "12_15_30|12_87_20", None, False),
]

# ---------------------------------------------------------------- materiais
# key, ID, CN, EN, descCN, descEN, price, texture
MATS = [
    ("drug_base", 96101, "丹辅", "Pill Base",
     "制作大部分药丸所需的材料，充当粘合剂和调味剂，让药材的药效不至于过于强烈。",
     "Needed for most pills. It binds and flavors the herbs so their effect is not too harsh.",
     30, "Item/Resource/RawMedical/RI_Resource_DrugBase/a.png"),
    ("gold_sand", 96102, "金砂", "Gold Sand",
     "将黄金磨碎成砂粉，一种具有药效的金属药材。对修炼者来说，金砂能稳固丹药的药性。",
     "Gold ground into sand, a metallic medicinal ingredient. For cultivators it stabilizes the power of a pill.",
     200, "Item/Resource/RawMedical/RI_Resource_GoldSand.png"),
    ("tusk_powder", 96103, "象牙粉", "Tusk Powder",
     "一种具有药效的动物药材，有吸附杂质的神奇作用，复杂的丹药需要用它来辅助。",
     "An animal ingredient that draws out impurities. Complex pills need it.",
     100, "Item/Resource/RawMedical/RI_Resource_ElephantTuskPowder.png"),
    ("lingzhi", 96104, "灵芝", "Lingzhi",
     "一种具有药效的土属性药材，营养极其丰富，常用于调和刺激性的药材。",
     "An earth-aspected medicinal mushroom. Very nourishing, often used to soften harsh ingredients.",
     60, "Item/Resource/RawMedical/RI_Resource_LingZhi.png"),
    ("dusky_mushroom", 96105, "紫雾菇", "Dusky Mushroom",
     "一种具有药效的水属性药材，蕴含丰富的营养，一些特殊丹药会用到。",
     "A water-aspected medicinal mushroom, rich in nutrients and used in some special pills.",
     60, "Item/Resource/RawMedical/RI_Resource_DuskyMushroom.png"),
    ("plantain_seed", 96106, "车前子", "Plantain Seed",
     "一种具有药效的木属性药材，能促进伤口愈合，并带有轻微的杀菌效果。",
     "A wood-aspected medicinal seed. It helps wounds heal and is mildly antiseptic.",
     40, "Item/Resource/RawMedical/RI_Resource_PlantainSeed.png"),
    ("snow_lotus", 96107, "雪莲", "Snow Lotus",
     "一种具有药效的水属性药材，药效极其强烈，能清热解毒。",
     "A water-aspected medicinal flower with a very strong effect. It clears heat and toxins.",
     80, "Item/Resource/RawMedical/RI_Resource_SnowLotus.png"),
    ("lantern_berries", 96108, "提灯果", "Lantern Berries",
     "一种具有药效的火属性药材，如同灯笼一般散发淡淡光芒，药效十分温和。",
     "Fire-aspected medicinal berries that glow like little lanterns. Their effect is gentle.",
     60, "Item/Resource/RawMedical/RI_Resource_LanternBerries.png"),
    ("silver_ginseng", 96109, "银芯参", "Silver Ginseng",
     "一种具有药效的金属性药材，生长时间越长，银色越多，药效也越强。",
     "A metal-aspected medicinal root. The older it is, the more silver it shows and the stronger it gets.",
     60, "Item/Resource/RawMedical/RI_Resource_SilverGinseng.png"),
    ("dragon_shoot", 96110, "火龙芽", "Fire Dragon Shoot",
     "一种具有药效的火属性药材，味道奇辣无比，蕴含极其猛烈的刺激性元素。",
     "A fire-aspected medicinal shoot, fiercely spicy and full of stimulating essence.",
     70, "Item/Resource/RawMedical/RI_Resource_TillandsiaDragonShoot.png"),
    ("spirit_stone", 96111, "灵石", "Spirit Stone",
     "一颗灵气固化而成的半透明石头，完全耐火。炼制高品丹药时用来提供灵气。",
     "A translucent stone of solidified Qi, completely fireproof. It supplies Qi when refining high-grade pills.",
     150, "Item/Resource/RI_Resource_SpiritualCrystal/a.png"),
    ("spirit_fire", 96112, "丹魄真火", "Spirit Fire",
     "丹魄真火，也被称为先天之火。炼制二品和一品丹药都离不开它。",
     "Spirit Fire, also called innate fire. Second- and first-grade pills cannot be refined without it.",
     500, "Item/Resource/RI_Resource_SpiritFire/a.png"),
    ("jungou_bezoar", 96113, "菌狗胆石", "Jungou Bezoar",
     "一种具有奇妙药效的金属性动物药材，在胆囊内逐渐形成。",
     "A metal-aspected animal ingredient with remarkable effects, formed slowly in the gallbladder.",
     120, "Item/Resource/RawMedical/RI_Resource_JunGouBezoar.png"),
    ("pilose_antler", 96114, "玃如茸角", "Pilose Antler",
     "一种具有奇妙药效的木属性动物药材，来自长寿而聪颖的玃如。",
     "A wood-aspected animal ingredient with remarkable effects, from the long-lived and clever Jueru.",
     150, "Item/Resource/RawMedical/RI_Resource_PiloseAntler.png"),
]

# ---------------------------------------------------------------- pílulas
# key, ID, CN, EN, descCN, descEN, price, subType, recoverHP, recoverTime, immunity,
#   effectCN, effectEN (linha curta do painel), durationH (0 = instantâneo), texture
PILLS = [
    ("guiling", 96001, "【三品】归灵丸", "[3rd Grade] Spirit Return Pill",
     "三品药丸，蕴含浓缩灵气，味道甜甜的。服用后立即获得50点灵气。",
     "A third-grade pill full of condensed Qi, sweet to the taste. Grants 50 Qi at once.",
     400, 9, 0, 1, 0, "灵气+50", "Qi +50", 0, "Item/Drugs/RI_Drug_CultivateEnergyPill/a.png"),
    ("baoxue", 96002, "【三品】保血丸", "[3rd Grade] Blood Saving Pill",
     "三品药丸，味道微苦，可以增强血液循环，加速治愈。24小时内接受治疗的恢复效果+50%。",
     "A slightly bitter third-grade pill that strengthens circulation and speeds healing. Healing received +50% for 24 hours.",
     350, 1, 15, 12, 2, "每小时意识回复15", "Recovers 15 per hour", 24, "Item/Drugs/RI_Drug_BloodSavingPill/a.png"),
    ("lingyun", 96003, "【三品】凌云丸", "[3rd Grade] Cloud Soaring Pill",
     "三品药丸，味道奇甜无比，宛若蜜糖。3小时内移动速度+50%。",
     "A third-grade pill as sweet as honey. Move speed +50% for 3 hours.",
     350, 9, 0, 1, 0, "移动速度+50%", "Move speed +50%", 3, "Item/Drugs/RI_Drug_LingYunPill/a.png"),
    ("sanshi", 96004, "【三品】三石散", "[3rd Grade] Three Stones Powder",
     "三品药散，一种灰色药粉，武行间非常推崇。4小时内格斗伤害+30%，近战闪避+20%。",
     "A gray third-grade powder prized by martial artists. Melee damage +30% and melee dodge +20% for 4 hours.",
     350, 9, 0, 1, 0, "格斗伤害+30%", "Melee damage +30%", 4, "Item/Drugs/RI_Drug_ThreeRockPowder/a.png"),
    ("guyuan", 96005, "【三品】固元散", "[3rd Grade] Origin Fortifying Powder",
     "三品药散，以金砂为主要材料。8小时内灵气吸收+25%，突破成功率+10%，适合在突破前服用。",
     "A third-grade powder made mainly from gold sand. Qi absorption +25% and breakthrough chance +10% for 8 hours. Take it before a breakthrough.",
     350, 9, 0, 1, 0, "突破成功率+10%", "Breakthrough chance +10%", 8, "Item/Drugs/RI_Drug_StrongPhysiquePowder/a.png"),
    ("bailiang", 96006, "【三品】百粮丸", "[3rd Grade] Hundred Grains Pill",
     "三品药丸，味道极苦微酸，可以消除饥饿。服用后吃饱，72小时内不会饥饿。",
     "A very bitter third-grade pill that banishes hunger. Fills the stomach and stops hunger for 72 hours.",
     350, 9, 0, 1, 0, "72小时不会饥饿", "No hunger for 72 hours", 72, "Item/Drugs/RI_Drug_AllCropsPill/a.png"),
    ("qingfeng", 96007, "【三品】清风膏", "[3rd Grade] Clear Breeze Balm",
     "三品药膏，气味清凉，味道极苦，可以清除疲劳。服用后恢复50点精力，18小时内不消耗精力。",
     "A cool-smelling, very bitter third-grade balm that clears fatigue. Restores 50 energy and stops energy loss for 18 hours.",
     400, 9, 0, 1, 0, "18小时不消耗精力", "No energy loss for 18 hours", 18, "Item/Drugs/RI_Drug_GreenBalm/a.png"),
    ("dieda", 96008, "【三品】跌打膏", "[3rd Grade] Bruise Balm",
     "三品药膏，一种棕黑的膏药，敷在伤口上，有比草药更好的治疗效果。",
     "A brown-black third-grade salve. Applied to wounds, it heals better than plain herbs.",
     250, 1, 15, 15, 1, "每小时意识回复15", "Recovers 15 per hour", 0, "Item/Drugs/RI_Drug_HealBalm/a.png"),
    ("xuanhan", 96009, "【二品】玄寒丸", "[2nd Grade] Profound Cold Pill",
     "二品丹药，味道清凉微苦，可以抵御酷热。14小时内可承受的温度上限+25℃。",
     "A cool, slightly bitter second-grade pill that wards off heat. Maximum comfortable temperature +25°C for 14 hours.",
     800, 9, 0, 1, 0, "温度上限+25℃", "Max temperature +25°C", 14, "Item/Drugs/RI_Drug_ExpelHotPill/a.png"),
    ("zhifeng", 96010, "【二品】炙风丸", "[2nd Grade] Scorching Wind Pill",
     "二品丹药，味道辛辣微甜，热力渗入四肢，使人能抵御严寒。14小时内可承受的温度下限-25℃。",
     "A spicy-sweet second-grade pill whose heat spreads through the limbs to resist the cold. Minimum comfortable temperature -25°C for 14 hours.",
     800, 9, 0, 1, 0, "温度下限-25℃", "Min temperature -25°C", 14, "Item/Drugs/RI_Drug_ExpelColdPill/a.png"),
    ("lingyu", 96011, "【二品】灵玉丸", "[2nd Grade] Spirit Jade Pill",
     "二品药丸，用灵石提炼而成。立即获得150点灵气，6小时内灵气吸收+50%。",
     "A second-grade pill refined from spirit stones. Grants 150 Qi at once and Qi absorption +50% for 6 hours.",
     900, 9, 0, 1, 0, "灵气+150", "Qi +150", 6, "Item/Drugs/RI_Drug_CultivateMoreEnergyPill/a.png"),
    ("jinxue", 96012, "【二品】金血丸", "[2nd Grade] Golden Blood Pill",
     "二品药丸，在体表形成一层柔韧的气膜护体。6小时内承受伤害-50%，但移动速度-50%。",
     "A second-grade pill that forms a tough film of Qi over the skin. Damage taken -50% but move speed -50% for 6 hours.",
     900, 9, 0, 1, 0, "承受伤害-50%", "Damage taken -50%", 6, "Item/Drugs/RI_Drug_GoldBloodPill/a.png"),
    ("huichun", 96013, "【二品】回春散", "[2nd Grade] Spring Return Powder",
     "二品药散，一种淡绿色的药粉，可以增强对各种疾病的免疫力。48小时内接受治疗的恢复效果+50%。",
     "A pale green second-grade powder that strengthens immunity against disease. Healing received +50% for 48 hours.",
     800, 2, 0, 48, 6, "免疫力回复6", "Immunity +6", 48, "Item/Drugs/RI_Drug_HerbPowder/a.png"),
    ("shengji", 96014, "【一品】生肌理髓丹", "[1st Grade] Flesh Mending Marrow Elixir",
     "一品丹药，可以在12小时内大大增强愈合能力，接受治疗的恢复效果+200%。",
     "A first-grade elixir that greatly boosts healing for 12 hours. Healing received +200%.",
     3200, 1, 60, 12, 4, "每小时意识回复60", "Recovers 60 per hour", 12, "Item/Drugs/Elixir/RI_Drug_MedullaOblongataElixir.png"),
    ("xiuluo", 96015, "【一品】修罗魔喰丹", "[1st Grade] Asura Devouring Elixir",
     "一品丹药，24小时内强行激发潜力：格斗伤害+100%，近战闪避+50%，但全局效率-50%。",
     "A first-grade elixir that forces out hidden potential for 24 hours: melee damage +100% and melee dodge +50%, but global efficiency -50%.",
     2800, 9, 0, 1, 0, "格斗伤害+100%", "Melee damage +100%", 24, "Item/Drugs/Elixir/RI_Drug_DemonElixir.png"),
    ("tianling", 96016, "【一品】天灵宝玉丹", "[1st Grade] Heavenly Spirit Jade Elixir",
     "一品丹药，在10天内极大地提升灵气的吸收效率（+150%）。",
     "A first-grade elixir that hugely raises Qi absorption (+150%) for 10 days.",
     3800, 9, 0, 1, 0, "灵气吸收+150%", "Qi absorption +150%", 240, "Item/Drugs/Elixir/RI_Drug_FiveTreasuresElixir.png"),
    ("tongxuan", 96017, "【一品】通玄筑基丹", "[1st Grade] Foundation Establishment Elixir",
     "一品丹药，凝气境突破至筑基境的必备之物。凝气圆满时服用会立即尝试突破；否则24小时内灵气吸收+25%。",
     "A first-grade elixir required to break through from Qi Condensation to Foundation Establishment. Taken at the peak of Qi Condensation it attempts the breakthrough at once; otherwise Qi absorption +25% for 24 hours.",
     3000, 9, 0, 1, 0, "筑基突破", "Foundation breakthrough", 24, "Item/Drugs/Elixir/RI_Drug_FoundationBuildingElixir.png"),
]

MAT_ID = {k: i for k, i, *_ in MATS}
PILL_ID = {k: i for k, i, *_ in PILLS}
NAME = {k: (cn, en) for k, _, cn, en, *_ in MATS + PILLS}

# Itens do jogo usados como ingrediente (tipo_id).
V = {"leaf": "29_0", "roots": "29_5", "stamen": "29_6", "potato": "13_2", "wheat": "13_0",
     "berry": "13_3", "mushroom": "13_4", "gold": "12_23", "gland": "12_73", "tissue": "12_92",
     "silicon": "12_13", "log": "12_10"}

def ing(key):
    if key in MAT_ID: return f"12_{MAT_ID[key]}"
    if key in PILL_ID: return f"29_{PILL_ID[key]}"
    return V[key]

# ID, bench, out, outCount, workload, [(ingrediente, qtd)], nameCN, nameEN (None = nome do produto)
RECIPES = [
    # Bancada de Elixires: materiais
    (96501, "bench", "drug_base", 5, 19, [("potato", 4)], "丹辅（土豆）", "Pill Base (potatoes)"),
    (96502, "bench", "drug_base", 5, 19, [("wheat", 4)], "丹辅（小麦）", "Pill Base (wheat)"),
    (96503, "bench", "gold_sand", 5, 19, [("gold", 1)], None, None),
    (96504, "bench", "tusk_powder", 10, 19, [("gland", 1)], None, None),
    (96505, "bench", "lingzhi", 2, 8, [("mushroom", 3), ("leaf", 1)], None, None),
    (96506, "bench", "dusky_mushroom", 2, 8, [("mushroom", 4)], None, None),
    (96507, "bench", "plantain_seed", 3, 6, [("leaf", 2)], None, None),
    (96508, "bench", "snow_lotus", 2, 8, [("roots", 2)], None, None),
    (96509, "bench", "lantern_berries", 2, 6, [("berry", 4)], None, None),
    (96510, "bench", "silver_ginseng", 2, 8, [("potato", 3), ("roots", 1)], None, None),
    (96511, "bench", "dragon_shoot", 2, 8, [("stamen", 2)], None, None),
    (96512, "bench", "spirit_stone", 1, 19, [("silicon", 5), ("lingzhi", 1)], None, None),
    (96513, "bench", "jungou_bezoar", 4, 19, [("gland", 1)], None, None),
    (96514, "bench", "pilose_antler", 1, 19, [("tissue", 4)], None, None),
    # Bancada de Elixires: pós
    (96521, "bench", "sanshi", 1, 8, [("leaf", 1), ("silver_ginseng", 1)], None, None),
    (96522, "bench", "guyuan", 1, 8, [("leaf", 1), ("dusky_mushroom", 1)], None, None),
    (96523, "bench", "huichun", 1, 8, [("leaf", 1), ("plantain_seed", 2), ("spirit_stone", 1)], None, None),
    # Caldeirões: pílulas de terceiro grau (pequeno e Taiyi)
    (96531, "little+taiyi", "dieda", 1, 8, [("leaf", 1), ("plantain_seed", 2)], None, None),
    (96532, "little+taiyi", "baoxue", 1, 8, [("plantain_seed", 2), ("drug_base", 1)], None, None),
    (96533, "little+taiyi", "lingyun", 1, 8, [("drug_base", 1), ("lantern_berries", 1)], None, None),
    (96534, "little+taiyi", "guiling", 1, 24, [("drug_base", 1), ("lingzhi", 1)], None, None),
    (96535, "little+taiyi", "qingfeng", 1, 11, [("lingzhi", 1), ("leaf", 1)], None, None),
    (96536, "little+taiyi", "bailiang", 3, 24, [("drug_base", 1), ("snow_lotus", 3), ("silver_ginseng", 3)], None, None),
    # Caldeirão Taiyi: segundo e primeiro grau
    (96541, "taiyi", "spirit_fire", 1, 24, [("spirit_stone", 2), ("log", 10)], None, None),
    (96542, "taiyi", "xuanhan", 3, 24, [("tusk_powder", 3), ("snow_lotus", 5), ("spirit_fire", 1), ("drug_base", 1)], None, None),
    (96543, "taiyi", "zhifeng", 3, 24, [("tusk_powder", 3), ("dragon_shoot", 5), ("spirit_fire", 1), ("drug_base", 1)], None, None),
    (96544, "taiyi", "lingyu", 3, 24, [("spirit_fire", 1), ("lingzhi", 5), ("spirit_stone", 7), ("drug_base", 1)], None, None),
    (96545, "taiyi", "jinxue", 3, 24, [("gold_sand", 10), ("spirit_fire", 1), ("dragon_shoot", 5), ("silver_ginseng", 5), ("drug_base", 1)], None, None),
    (96546, "taiyi", "shengji", 1, 33, [("gold_sand", 15), ("plantain_seed", 10), ("spirit_fire", 1), ("jungou_bezoar", 2), ("drug_base", 1)], None, None),
    (96547, "taiyi", "xiuluo", 1, 33, [("gold_sand", 15), ("dragon_shoot", 5), ("spirit_fire", 1), ("pilose_antler", 2), ("drug_base", 1)], None, None),
    (96548, "taiyi", "tianling", 1, 33, [("dusky_mushroom", 5), ("gold_sand", 15), ("spirit_fire", 1), ("jungou_bezoar", 2), ("drug_base", 1)], None, None),
    (96549, "taiyi", "tongxuan", 1, 33, [("snow_lotus", 3), ("lingzhi", 3), ("lantern_berries", 3), ("drug_base", 1)], None, None),
]

# ---------------------------------------------------------------- XML
lang = {}  # chave -> (cn, en), na ordem de criação
def tr(cn, en):
    if cn not in lang: lang[cn] = en
    return cn

def item(name, value):
    return f'      <Item Name="{name}">{escape(str(value))}</Item>\n'

def row(table, depend, fields, comment=None):
    s = ""
    if comment: s += f"    <!-- {comment} -->\n"
    s += f'    <ModDefRow Name="{table}" DependID="{depend}">\n'
    for k, v in fields: s += item(k, v)
    return s + "    </ModDefRow>\n"

out = []
out.append("\n    <!-- ===================== Fase 1: alquimia (gerado por tools/gen_phase1.py) ===================== -->\n")
out.append("    <!-- Bancadas: cópias da 药品加工台 (B_Manufacture 51, sem energia). ProductID usa o ID completo das nossas receitas. -->\n")
bench_recipes = {b[0]: [] for b in BENCHES}
for rid, benches, *_ in RECIPES:
    for b in benches.split("+"): bench_recipes[b].append(f"{PKG}_{rid}")
for key, bid, cn, en, dcn, den, w, h, mats, tex, tint in BENCHES:
    # Sem textura (a Bancada de Elixires): usa o ícone e o modelo da bancada de remédios do jogo.
    icon = [("RawIcon", f"wuxia_{key}_cauldron.png")] if tex else []
    out.append(row("F_ManufactureDef", 51, [
        ("ID", bid), ("Name", tr(cn, en)), ("Desc", tr(dcn, den)),
        *icon,
        ("Width", w), ("Height", h),
        # Mark = casa onde o colono trabalha, contada em Width x Height (veja Produce_Base.getProduceOperatePoint).
        # w*h + w//2 = logo abaixo do meio da construção. A cópia da bancada (3x1) trazia 4,
        # que no caldeirão 1x1 punha a cadeira 4 casas para baixo.
        ("Mark", w * h + w // 2),
        ("Materials", mats),
        ("ProductID", "|".join(bench_recipes[key])),
    ]))

out.append("\n    <!-- Materiais: cópias do 再生组织 (T_Material 92). -->\n")
for key, mid, cn, en, dcn, den, price, tex in MATS:
    obtain = "太乙炼丹炉" if key == "spirit_fire" else "制药工作台"
    out.append(row("T_MaterialDef", 92, [
        ("ID", mid), ("Name", tr(cn, en)), ("Feature", tr("药材", "Medicinal ingredient")),
        ("Desc", tr(dcn, den)), ("Obtain", obtain), ("Price", price), ("D_value2", price),
        ("RawIcon", f"wuxia_{key}.png"),
    ]))

out.append("\n    <!-- Pílulas: cópias do 治疗叶片 (T_Medicine 0). SubType 1/2 = cura que a IA usa sozinha;\n"
           "         SubType 9 = pílula de cultivo/bônus, só pelo botão direito (efeito no WuxiaMod.dll, veja PillEffects). -->\n")
TYPE_TXT = {1: "缓释自愈型", 2: "免疫增强型", 9: tr("修炼丹药", "Cultivation pill")}
for (key, pid, cn, en, dcn, den, price, sub, hp, rtime, imm, ecn, een, dur, tex) in PILLS:
    if dur > 0: dur_txt = tr(f"{dur}小时", f"{dur} hours")
    elif sub == 9: dur_txt = tr("立即", "Instant")
    else: dur_txt = tr(f"{rtime}小时", f"{rtime} hours")
    fields = [
        ("ID", pid), ("SubType", sub), ("Name", tr(cn, en)), ("Desc", tr(dcn, den)),
        ("Price", price), ("D_value1", price), ("Weight", 0.1), ("D_value2", 0.1),
        ("Rotten", -1), ("Recover_HP", hp), ("RecoverTime", rtime), ("Recover_TemporaryHP", 0),
        ("Recover_Immunity", imm), ("RawIcon", f"wuxia_pill_{key}.png"),
        ("Show_type2", "null"), ("Show_value2", "null"),
        ("Show_type4", tr("丹药功效", "Pill effect")), ("Show_value4", tr(ecn, een)),
        ("Show_value5", TYPE_TXT[sub]), ("D_value3", dur_txt),
    ]
    out.append(row("T_MedicineDef", 0, fields))

out.append("\n    <!-- Receitas. Materials/OutMaterial usam o ID curto (tipo_ID_qtd); o WuxiaMod.dll registra os IDs curtos. -->\n")
for rid, benches, outk, cnt, wl, ings, rcn, ren in RECIPES:
    is_pill = outk in PILL_ID
    pcn, pen = NAME[outk]
    ncn, nen = (rcn, ren) if rcn else (pcn, pen)
    ing_cn = "、".join(NAME[k][0] if k in NAME else {"leaf":"治疗叶片","roots":"杀菌根系","stamen":"麻痹花蕊","potato":"土豆","wheat":"小麦","berry":"浆果","mushroom":"蘑菇","gold":"金锭","gland":"生物腺体","tissue":"再生组织","silicon":"硅矿石","log":"原木"}[k] for k, _ in ings)
    ing_en = ", ".join(NAME[k][1] if k in NAME else {"leaf":"healing leaves","roots":"antibacterial roots","stamen":"paralysis stamens","potato":"potatoes","wheat":"wheat","berry":"berries","mushroom":"mushrooms","gold":"gold ingot","gland":"bio-gland","tissue":"regenerative tissue","silicon":"silicon ore","log":"logs"}[k] for k, _ in ings)
    verb_cn, verb_en = ("炼制", "Refine") if is_pill or outk == "spirit_fire" else ("制作", "Make")
    dcn = f"用{ing_cn}{verb_cn}{pcn}。"
    den = f"{verb_en} {pen} from {ing_en}."
    fields = [
        ("ID", rid), ("SubType", 3 if is_pill else 1), ("OverrideModuleId", -1),
        ("Materials", "|".join(f"{ing(k)}_{n}" for k, n in ings)),
        ("OutMaterial", f"{ing(outk)}_{cnt}"), ("Workload", wl),
        ("Name", tr(ncn, nen)), ("Desc", tr(dcn, den)), ("Apply", "True"), ("IsRandom", "False"),
        ("Show_Toast", tr("分类：丹药", "Category: Pills") if is_pill else "分类：材料"),
    ]
    out.append(row("F_ProductListDef", -1, fields))

out.append("\n    <!-- Textos em inglês da Fase 1 -->\n")
for cn, en in lang.items():
    out.append('    <ModDefRow Name="LanguageDef">\n' + item("Key", "id_" + cn) + item("CN", cn) + item("EN", en) + "    </ModDefRow>\n")
out.append("    <!-- ===================== fim da Fase 1 ===================== -->\n")

src = open(DEF, encoding="utf-8").read()
src = re.sub(r"\n    <!-- =+ Fase 1:.*?fim da Fase 1 =+ -->\n", "", src, flags=re.S)
src = src.replace("  </rows>", "".join(out) + "  </rows>")
open(DEF, "w", encoding="utf-8").write(src)

# ---------------------------------------------------------------- imagens
def fit(img, w, h):
    img = img.convert("RGBA")
    bbox = img.getbbox()
    if bbox: img = img.crop(bbox)
    img.thumbnail((w, h), Image.LANCZOS)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.paste(img, ((w - img.width) // 2, (h - img.height) // 2), img)
    return canvas

def bronze(img):
    a = img.getchannel("A")
    gray = ImageOps.grayscale(img)
    gray = ImageEnhance.Contrast(gray).enhance(1.15)
    col = ImageOps.colorize(gray, black=(30, 18, 8), mid=(122, 72, 34), white=(232, 176, 98))
    col.putalpha(a)
    return col

for key, *_rest, tex in [(m[0], m[-1]) for m in MATS]:
    pass
for m in MATS:
    fit(Image.open(os.path.join(RITEX, m[-1])), 64, 64).save(os.path.join(TEX_OUT, f"wuxia_{m[0]}.png"), optimize=True)
for p in PILLS:
    fit(Image.open(os.path.join(RITEX, p[-1])), 64, 64).save(os.path.join(TEX_OUT, f"wuxia_pill_{p[0]}.png"), optimize=True)
for key, bid, cn, en, dcn, den, w, h, mats, tex, tint in BENCHES:
    if not tex: continue
    img = Image.open(os.path.join(RITEX, tex)).convert("RGBA")
    if tint: img = bronze(img)
    img = fit(img, 40 * w, 40 * h)
    if w == 1:
        # O menu de construção estica cada ícone até encher o quadro: sem a borda vazia,
        # o caldeirão pequeno parecia maior que o Taiyi. No mapa a borda não aparece.
        canvas = Image.new("RGBA", (80, 80), (0, 0, 0, 0))
        canvas.paste(img, (20, 20), img)
        img = canvas
    img.save(os.path.join(TEX_OUT, f"wuxia_{key}_cauldron.png"), optimize=True)

print("linhas:", len(BENCHES), len(MATS), len(PILLS), len(RECIPES), "textos:", len(lang))
