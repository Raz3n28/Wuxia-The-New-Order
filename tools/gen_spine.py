# Gera as imagens das roupas e armas do mod no boneco (sistema oficial de skins do jogo,
# o mesmo do exemplo oficial "CharacterCostume", Workshop 3818437148) e o arquivo
# Resources/Textures/role-customize.xml. Também põe o campo Skin das roupas e os nomes das
# armas no def.xml.
#
# Uso: python3 -I gen_spine.py <pasta 294100 do Workshop> <repo>
#
# Como o jogo usa (SpineCombine, Assembly-CSharp):
# - Cada <ModSpineSkin> copia uma skin modelo do jogo (capacete DG_TK_002_M, tronco
#   DG_KJ_000_M) e troca as peças listadas pela nossa imagem. A imagem fica centrada no
#   lugar da peça original, 1 pixel = 1 unidade do boneco; nas malhas (tronco, barra) a
#   imagem é esticada sobre a malha.
# - Peça não listada continua com a arte do modelo: por isso listamos braços e mãos com uma
#   imagem transparente (a armadura do RimImmortal é só o tronco).
# - Direções: f frente, 3 diagonal para baixo, s lado, 9 diagonal para cima, b costas. O
#   RimImmortal tem south, east e north.
# - As imagens vão em Resources/Textures/spine_wuxia.pack; o mod separa em spine_wuxia/ ao iniciar.
# - As medidas das peças (tools/spine_geometry.json) vieram da exportação feita no jogo
#   (SpineDump, "dumpSpine" no config.json).
import sys, os, json, re
from PIL import Image

WS, REPO = sys.argv[1], sys.argv[2]
TOOLS = os.path.join(REPO, "tools")
TEX = os.path.join(REPO, "Mod/v_common/Resources/Textures")
OUT_DIR = "spine_wuxia"  # dentro de Resources/Textures
DEF = os.path.join(REPO, "Mod/v_common/Resources/Def/def.xml")

GEO = json.load(open(os.path.join(TOOLS, "spine_geometry.json")))
CLOTH = json.load(open(os.path.join(TOOLS, "spine_cloth_map.json")))
DIRS = ["f", "3", "s", "9", "b"]
SLOT_DIR = {"f": "00_front", "3": "00_3", "s": "00_side", "9": "00_9", "b": "00_back"}
HELMET_SRC = {"f": "south", "3": "east", "s": "east", "9": "east", "b": "north"}
TOP_SRC = {"f": "south", "3": "south", "s": "east", "9": "north", "b": "north"}
HEAD_FRACTION = 0.5   # largura da cabeça do RimWorld na tela da textura (cabeça e capacete dividem a tela)
MESH_PPU = 2          # pixels por unidade nas malhas (a imagem é esticada, então pode ter mais resolução)

written = {}  # nome -> bytes do PNG; tudo vai num arquivo só (spine_wuxia.pack)


def rel(name):
    return OUT_DIR + "\\" + name


SAVED = {}  # conteúdo -> arquivo (imagens iguais, p.ex. arma de lado e nas diagonais, viram um arquivo só)


def save(img, name):
    key = (img.size, img.tobytes())
    if key in SAVED:
        return rel(SAVED[key])
    SAVED[key] = name
    import io
    buf = io.BytesIO()
    img.save(buf, "PNG", optimize=True)
    written[name] = buf.getvalue()
    return rel(name)


def load(path):
    im = Image.open(path).convert("RGBA")
    # Fundo quase transparente conta como vazio.
    a = im.split()[3].point(lambda v: 255 if v > 24 else 0)
    im.putalpha(Image.composite(im.split()[3], Image.new("L", im.size, 0), a))
    return im, a.getbbox()


def part(skin, slot, att):
    return GEO[skin][slot + "|" + att]


def head(d):
    for k, v in GEO["00azipifuman"].items():
        if k.startswith(SLOT_DIR[d] + "/head"):
            return v
    raise KeyError(d)


def place(world, art, box, scale, x0, y_top):
    """Desenha 'art' (só a parte box) na imagem do mundo: canto esquerdo x0, topo y_top."""
    crop = art.crop(box)
    w, h = max(1, round(crop.width * scale)), max(1, round(crop.height * scale))
    world["items"].append((crop.resize((w, h), Image.LANCZOS), x0, y_top))


def render(world, x0, y0, x1, y1, ppu):
    """Recorta o retângulo do mundo [x0,x1]x[y0,y1] (y para cima) numa imagem."""
    W, H = max(1, round((x1 - x0) * ppu)), max(1, round((y1 - y0) * ppu))
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for img, ax, atop in world["items"]:
        im = img if ppu == 1 else img.resize((img.width * ppu, img.height * ppu), Image.LANCZOS)
        out.alpha_composite(im, (round((ax - x0) * ppu), round((y1 - atop) * ppu)))
    return out


TRANSPARENT = None


def transparent():
    global TRANSPARENT
    if TRANSPARENT is None:
        TRANSPARENT = save(Image.new("RGBA", (2, 2), (0, 0, 0, 0)), "empty.png")
    return TRANSPARENT


# ------------------------------------------------------------------ capacetes
def helmet(key, stem):
    items = []
    front_w = head("f")["bbox"][2] - head("f")["bbox"][0]
    for d in DIRS:
        art, box = load(os.path.join(WS, stem + "_" + HELMET_SRC[d] + ".png"))
        hb = head(d)["bbox"]
        hx, hy = (hb[0] + hb[2]) / 2, (hb[1] + hb[3]) / 2
        s = front_w / (HEAD_FRACTION * art.width)
        # O centro da tela da textura fica no centro da cabeça.
        ax0 = hx + (box[0] - art.width / 2) * s
        atop = hy - (box[1] - art.height / 2) * s
        world = {"items": []}
        place(world, art, box, s, ax0, atop)
        p = part("DG_TK_002_M", SLOT_DIR[d] + "/casque " + d, "casque " + d)
        cx, cy = p["center"]
        img, x0, top = world["items"][0]
        ext_x = max(abs(x0 - cx), abs(x0 + img.width - cx)) + 1
        ext_y = max(abs(top - cy), abs(top - img.height - cy)) + 1
        png = render(world, cx - ext_x, cy - ext_y, cx + ext_x, cy + ext_y, 1)
        items.append(("head_helmet_" + d, save(png, f"{key}_helmet_{d}.png")))
    return items


# ------------------------------------------------------------------ tronco
TOP_KEYS = {  # chave do jogo -> (slot, anexo) no modelo DG_KJ_000_M, por direção
    "f": {"body": "body f", "leftUpperArm": "Left big arm f", "leftForeArm": "left lower arm f", "leftHand": "Handl f",
          "rightUpperArm": "Right big arm f", "rightForeArm": "Right lower arm f", "rightHand": "handr f",
          "leftHem": "skirtl f", "midHem": "skirtm f", "rightHem": "skirtr f"},
    "3": {"body": "body 3", "leftUpperArm": "Left big arm 3", "leftForeArm": "left lower arm 3", "leftHand": "handl 3",
          "rightUpperArm": "Right big arm 3", "rightForeArm": "Right lower arm 3", "rightHand": "handr 3",
          "leftHem": "skirtl 3", "midHem": "skirtm 3", "rightHem": "skirtr 3"},
    "s": {"body": "body s", "leftUpperArm": "Left big arm s", "leftForeArm": "left lower arm s", "leftHand": "handl s",
          "rightUpperArm": "Right big arm s", "rightForeArm": "Right lower arm s", "rightHand": "handr s",
          "leftHem": "skirt s", "rightHem": "skritr s"},
    "9": {"body": "body 9", "leftUpperArm": "Left big arm 9", "leftForeArm": "left lower arm 9", "leftHand": "handr 3",
          "rightUpperArm": "Right big arm 9", "rightForeArm": "Right lower arm 9", "rightHand": "handr 9", "midHem": "skirt 9"},
    "b": {"body": "body b", "leftUpperArm": "Left big arm b", "leftForeArm": "left lower arm b", "leftHand": "handl b",
          "rightUpperArm": "Right big arm b", "rightForeArm": "Right lower arm b", "rightHand": "handr b", "midHem": "skirt b"},
}
MESH_PARTS = ("body", "leftHem", "midHem", "rightHem")


def top(key, stem, sex):
    items = []
    for d in DIRS:
        art, box = load(os.path.join(WS, f"{stem}_{sex}_{TOP_SRC[d]}.png"))
        parts = {k: part("DG_KJ_000_M", SLOT_DIR[d] + "/" + a, a) for k, a in TOP_KEYS[d].items()}
        # Área do tronco + barra; a arte encaixa pela largura, encostada nos ombros.
        mesh = [parts[k]["bbox"] for k in MESH_PARTS if k in parts]
        body = parts["body"]["bbox"]
        tx0, tx1 = body[0] - 2, body[2] + 2
        ttop, tbot = body[3] + 1, min(b[1] for b in mesh)
        aw, ah = box[2] - box[0], box[3] - box[1]
        s = min((tx1 - tx0) / aw, (ttop - tbot) / ah)
        world = {"items": []}
        place(world, art, box, s, (tx0 + tx1) / 2 - aw * s / 2, ttop)
        for k, a in TOP_KEYS[d].items():
            if k in MESH_PARTS:
                b = parts[k]["bbox"]
                png = render(world, b[0], b[1], b[2], b[3], MESH_PPU)
                items.append((f"top_{k}_{d}", save(png, f"{key}_{sex.lower()}_{k}_{d}.png")))
            else:
                items.append((f"top_{k}_{d}", transparent()))
    return items


# ------------------------------------------------------------------ xml e def.xml
def skin_xml(name, kind, items):
    lines = [f'\t\t<ModSpineSkin Name="{name}" SkinType="{kind}">']
    lines += [f'\t\t\t<PartItem Name="{n}" Texture="{t}" />' for n, t in items]
    lines.append("\t\t</ModSpineSkin>")
    return "\n".join(lines)


# ------------------------------------------------------------------ armas
# O jogo cria o anexo da arma copiando um anexo modelo por tipo (S_CharacterSpinePartsDef.Rack,
# p.ex. sword/042qishijianf) e põe a nossa imagem no centro dele. Tipo da arma = slot da arma
# do jogo que o nosso item copia (DependID). Medidas (largura, altura) do anexo por direção e
# para onde a ponta aponta na imagem: horizontal = ponta à direita; nas verticais "up"/"down".
WEAPON_TYPE = {10810: "sword", 90910: "sword", 91110: "sword", 11010: "spear", 21010: "spear",
               91130: "spear", 11110: "spear", 10610: "gun", 10310: "gun", 40410: "gun",
               10710: "thrown", 90720: "launcher"}
WEAPON_BOX = {  # tipo -> {direção: (largura, altura, ponta)}
    "sword": {"f": (16, 60, "up"), "b": (16, 60, "up"), "s": (76, 24, "right"), "3": (76, 24, "right"), "9": (76, 24, "right")},
    "spear": {"f": (14, 104, "up"), "b": (14, 104, "down"), "s": (104, 14, "right"), "3": (104, 14, "right"), "9": (104, 14, "right")},
    "gun": {"f": (18, 56, "down"), "b": (18, 56, "up"), "s": (124, 44, "right"), "3": (124, 44, "right"), "9": (124, 44, "right")},
    "launcher": {"f": (44, 124, "down"), "b": (44, 124, "up"), "s": (70, 42, "right"), "3": (70, 42, "right"), "9": (70, 42, "right")},
    "thrown": {d: (28, 20, "right") for d in "f3s9b"},
}
TEXTURE_ATTR = {"b": "Texture_T", "9": "Texture_RT", "s": "Texture_S", "3": "Texture_RB", "f": "Texture_B"}


def straighten(icon):
    """Gira o ícone para a arma ficar deitada com a ponta à direita (eixo principal dos pixels)."""
    import math
    a = icon.split()[3]
    pts = [(x, y) for y in range(icon.height) for x in range(icon.width) if a.getpixel((x, y)) > 40]
    if len(pts) < 4:
        return icon
    mx = sum(p[0] for p in pts) / len(pts); my = sum(p[1] for p in pts) / len(pts)
    sxx = sum((p[0] - mx) ** 2 for p in pts); syy = sum((p[1] - my) ** 2 for p in pts)
    sxy = sum((p[0] - mx) * (p[1] - my) for p in pts)
    ang = 0.5 * math.degrees(math.atan2(2 * sxy, sxx - syy))  # ângulo do eixo, y para baixo
    # No RimWorld a ponta fica para a direita (deitada) ou para cima à direita (diagonal).
    out = icon.rotate(ang, resample=Image.BICUBIC, expand=True)
    return out.crop(out.split()[3].point(lambda v: 255 if v > 24 else 0).getbbox())


def weapon(key, icon_path, wtype):
    art = straighten(Image.open(icon_path).convert("RGBA"))
    attrs = {}
    for d in DIRS:
        w, h, tip = WEAPON_BOX[wtype][d]
        length = max(w, h) * 0.95
        s = length / art.width
        img = art.resize((max(1, round(art.width * s)), max(1, round(art.height * s))), Image.LANCZOS)
        if tip == "up":
            img = img.rotate(90, expand=True)
        elif tip == "down":
            img = img.rotate(-90, expand=True)
        W, H = max(w, img.width), max(h, img.height)
        W += W % 2; H += H % 2
        canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        canvas.alpha_composite(img, ((W - img.width) // 2, (H - img.height) // 2))
        attrs[TEXTURE_ATTR[d]] = save(canvas, f"{key}_weapon_{d}.png")
    return attrs


def main():
    blocks, skins, weapons = [], {}, {}
    for key, tmpl, tex in CLOTH:
        stem = tex[:-4]
        if tmpl.startswith("head") or tmpl == "hat":
            name = "wuxia_" + key
            blocks.append(skin_xml(name, "helmet", helmet(key, stem)))
            skins["wuxia_" + key] = name
        else:
            m, w = "wuxia_" + key + "_m", "wuxia_" + key + "_w"
            blocks.append(skin_xml(m, "top", top(key, stem, "Male")))
            blocks.append(skin_xml(w, "top", top(key, stem, "Female")))
            skins["wuxia_" + key] = m + "|" + w  # ClothSkin: menino|menina
    s = open(DEF, encoding="utf-8").read()
    wlines = []
    for dep, body in re.findall(r'<ModDefRow Name="T_WeaponDef" DependID="(\d+)">(.*?)</ModDefRow>', s, re.S):
        c = re.search(r'"ClassID">([^<]*)', body).group(1)
        icon = re.search(r'"RawIcon">([^<]*)', body).group(1)
        wtype = WEAPON_TYPE.get(int(dep) // 10 * 10)
        if wtype is None or c in weapons:
            continue
        attrs = weapon(c[len("wuxia_"):], os.path.join(TEX, icon), wtype)
        weapons[c] = c
        wlines.append(f'\t\t<ModSpineWeapon Name="{c}" WeaponType="{wtype}" ' + " ".join(f'{k}="{v}"' for k, v in attrs.items()) + " />")
    xml = ('<?xml version="1.0" encoding="utf-8"?>\n<ModXmlSpine xmlns:xsd="http://www.w3.org/2001/XMLSchema" '
           'xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">\n\t<skins>\n' + "\n".join(blocks) +
           "\n\t</skins>\n\t<weapons>\n" + "\n".join(wlines) + "\n\t</weapons>\n</ModXmlSpine>\n")
    open(os.path.join(TEX, "role-customize.xml"), "w", encoding="utf-8").write(xml)
    # Um arquivo só com todas as imagens: o mod (SpinePack.cs) separa em spine_wuxia/ ao iniciar,
    # antes do jogo montar as skins. Assim instalar e publicar mexe em poucos arquivos.
    import struct
    with open(os.path.join(TEX, OUT_DIR + ".pack"), "wb") as f:
        f.write(b"WXPK1" + struct.pack("<i", len(written)))
        for name, data in sorted(written.items()):
            n = name.encode("utf-8")
            f.write(struct.pack("<i", len(n)) + n + struct.pack("<i", len(data)) + data)

    def fix(m):
        body = m.group(0)
        c = re.search(r'"ClassID">([^<]*)', body).group(1)
        if c not in skins:
            return body
        body = re.sub(r'\n\s*<Item Name="Skin">[^<]*</Item>', "", body)
        return body.replace("</ModDefRow>", f'  <Item Name="Skin">{skins[c]}</Item>\n    </ModDefRow>')
    s = re.sub(r'<ModDefRow Name="T_ClothDef".*?</ModDefRow>', fix, s, flags=re.S)

    fields = [f"{d}_{k}_Weapon" for d in ("f", "x3", "s", "x9", "b") for k in ("Slot", "StandBy")]

    def fixw(m):
        body = m.group(0)
        c = re.search(r'"ClassID">([^<]*)', body).group(1)
        if c not in weapons:
            return body
        for f in fields:
            body = re.sub(r'\n\s*<Item Name="' + f + r'">[^<]*</Item>', "", body)
        add = "".join(f'  <Item Name="{f}">{weapons[c]}</Item>\n    ' for f in fields)
        return body.replace("</ModDefRow>", add + "</ModDefRow>")
    s = re.sub(r'<ModDefRow Name="T_WeaponDef".*?</ModDefRow>', fixw, s, flags=re.S)
    open(DEF, "w", encoding="utf-8").write(s)
    print(len(written), "imagens,", len(blocks), "skins,", len(weapons), "armas")


main()
