"""Monta a capa (preview.png) e o ícone (icon.png) do Workshop a partir das artes do Canva.

Uso: python3 tools/make_workshop_art.py <capa.jpg|png> <icone.jpg|png> <pasta de saída>
"""
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

FONT_DIRS = ["/usr/share/fonts/truetype/liberation/", "/usr/share/fonts/truetype/dejavu/"]


def font(names, size):
    for d in FONT_DIRS:
        for n in names:
            try:
                return ImageFont.truetype(d + n, size)
            except OSError:
                pass
    return ImageFont.load_default()


def cover_crop(im, w, h):
    r = max(w / im.width, h / im.height)
    im = im.resize((round(im.width * r), round(im.height * r)), Image.LANCZOS)
    x, y = (im.width - w) // 2, (im.height - h) // 2
    return im.crop((x, y, x + w, y + h))


def gold_text(base, xy, text, fnt, anchor="mm", glow=(255, 190, 80)):
    """Texto dourado com degradê, contorno escuro e brilho."""
    w, h = base.size
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).text(xy, text, font=fnt, fill=255, anchor=anchor)
    # brilho
    halo = Image.new("RGBA", (w, h), glow + (0,))
    halo.putalpha(mask.filter(ImageFilter.GaussianBlur(fnt.size // 6)).point(lambda v: min(255, v * 2)))
    base.alpha_composite(halo)
    # contorno
    outline = Image.new("RGBA", (w, h), (30, 15, 5, 0))
    outline.putalpha(mask.filter(ImageFilter.MaxFilter(5)))
    base.alpha_composite(outline)
    # degradê
    bbox = mask.getbbox()
    grad = Image.new("RGBA", (w, h))
    gd = ImageDraw.Draw(grad)
    top, bottom = (255, 245, 200), (215, 145, 40)
    for y in range(bbox[1], bbox[3] + 1):
        t = (y - bbox[1]) / max(1, bbox[3] - bbox[1])
        gd.line([(0, y), (w, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    grad.putalpha(mask)
    base.alpha_composite(grad)


def banner(base, center, text, fnt, pad=(48, 12)):
    """Faixa 'UNDER CONSTRUCTION' em vermelho com listras de obra nas pontas."""
    d = ImageDraw.Draw(base)
    l, t, r, b = d.textbbox(center, text, font=fnt, anchor="mm")
    box = (l - pad[0], t - pad[1], r + pad[0], b + pad[1])
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    ld.rounded_rectangle((box[0] + 4, box[1] + 6, box[2] + 4, box[3] + 6), 10, fill=(0, 0, 0, 140))
    ld.rounded_rectangle(box, 10, fill=(200, 30, 30, 240), outline=(255, 210, 90, 255), width=3)
    # listras amarelas e pretas nas pontas
    stripe_w = 22
    for side in (box[0] + 6, box[2] - 6 - stripe_w):
        s = Image.new("RGBA", (stripe_w, box[3] - box[1] - 12), (25, 25, 25, 255))
        sd = ImageDraw.Draw(s)
        for k in range(-s.height, s.width + s.height, 12):
            sd.polygon([(k, 0), (k + 6, 0), (k + 6 - s.height, s.height), (k - s.height, s.height)], fill=(250, 200, 30, 255))
        layer.alpha_composite(s, (side, box[1] + 6))
    ld.text(center, text, font=fnt, fill=(255, 255, 255, 255), anchor="mm")
    base.alpha_composite(layer)


def make_preview(src, out, size=(1280, 720)):
    W, H = size
    im = cover_crop(Image.open(src).convert("RGB"), W, H).convert("RGBA")
    # escurece a faixa de baixo para o texto
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade)
    for y in range(H // 2, H):
        a = round(200 * ((y - H / 2) / (H / 2)) ** 1.4)
        sd.line([(0, y), (W, y)], fill=(5, 5, 20, a))
    im.alpha_composite(shade)
    gold_text(im, (W // 2, int(H * 0.70)), "WUXIA", font(["LiberationSerif-Bold.ttf", "DejaVuSerif-Bold.ttf"], 150))
    gold_text(im, (W // 2, int(H * 0.82)), "THE NEW DAWN", font(["LiberationSerif-Bold.ttf", "DejaVuSerif-Bold.ttf"], 58))
    banner(im, (W // 2, int(H * 0.93)), "ALPHA  ·  UNDER CONSTRUCTION", font(["LiberationSans-Bold.ttf", "DejaVuSans-Bold.ttf"], 30))
    im.convert("RGB").save(out)


def make_icon(src, out, size=512):
    im = cover_crop(Image.open(src).convert("RGB"), size, size).convert("RGBA")
    # fita "ALPHA" no canto
    rib = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    rd = ImageDraw.Draw(rib)
    band = size // 9
    rd.polygon([(size - 3 * band, 0), (size - 1.6 * band, 0), (size, 1.6 * band), (size, 3 * band)], fill=(200, 30, 30, 240))
    f = font(["LiberationSans-Bold.ttf", "DejaVuSans-Bold.ttf"], int(band * 0.62))
    txt = Image.new("RGBA", (int(3 * band), band), (0, 0, 0, 0))
    ImageDraw.Draw(txt).text((txt.width // 2, txt.height // 2), "ALPHA", font=f, fill="white", anchor="mm")
    txt = txt.rotate(-45, expand=True, resample=Image.BICUBIC)
    c = size - 1.15 * band
    rib.alpha_composite(txt, (int(c - txt.width / 2), int(1.15 * band - txt.height / 2)))
    im.alpha_composite(rib)
    im.convert("RGB").save(out)


if __name__ == "__main__":
    make_preview(sys.argv[1], sys.argv[3] + "/preview.png")
    make_icon(sys.argv[2], sys.argv[3] + "/icon.png")
