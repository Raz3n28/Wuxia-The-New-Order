import csv, os, sys
from PIL import Image
D = "/mnt/user-data/uploads/common--Outer Space/mods/WuxiaMod/dump/"
rows = list(csv.DictReader(open(D + 'parts.csv'), delimiter=';'))
order = {r['slot']: int(r['index']) for r in csv.DictReader(open(D + 'slots.csv'), delimiter=';')}
bones = {r['bone']: r for r in csv.DictReader(open(D + 'bones.csv'), delimiter=';')}
SLOT_DIR = {"f": "00_front", "3": "00_3", "s": "00_side", "9": "00_9", "b": "00_back"}
SKINS = sys.argv[1].split(',') if len(sys.argv) > 1 else ['00azipifuman']
S = int(os.environ.get("S", "4"))
def img(r):
    p = r['png']
    if not p or not os.path.exists(D + p): return None
    im = Image.open(D + p).convert('RGBA')
    if '__deg90' in p: im = im.rotate(-90, expand=True)
    return im if im.width > 2 or im.height > 2 else None
def render(d):
    pre = SLOT_DIR[d] + '/'
    # a skin posterior substitui o anexo do mesmo slot
    chosen = {}
    for sk in SKINS:
        for r in rows:
            if r['kind'] == 'skin' and r['skin'] == sk and r['slot'].startswith(pre):
                chosen[r['slot']] = r
    out = Image.new('RGBA', (110 * S, 150 * S), (0, 0, 0, 0))
    for slot in sorted(chosen, key=lambda s: order.get(s, 0)):
        r = chosen[slot]; im = img(r)
        if im is None: continue
        x0, y0, x1, y1 = (float(r[k]) for k in ('minX', 'minY', 'maxX', 'maxY'))
        if r['type'] == 'RegionAttachment':
            b = bones[r['bone']]
            th = float(b['rotation']) + float(r['rotation'])
            if float(r['scaleX']) * float(b['scaleX']) < 0: im = im.transpose(Image.FLIP_LEFT_RIGHT)
            im = im.rotate(th, expand=True, resample=Image.NEAREST)
        else:
            im = im.resize((max(1, round(x1 - x0)), max(1, round(y1 - y0))), Image.NEAREST)
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        im = im.resize((im.width * S, im.height * S), Image.NEAREST)
        out.alpha_composite(im, (round((cx + 55) * S - im.width / 2), round((135 - cy) * S - im.height / 2)))
    return out
ims = [render(d) for d in SLOT_DIR]
sheet = Image.new('RGBA', (sum(i.width for i in ims), ims[0].height), (60, 70, 80, 255))
x = 0
for d, i in zip(SLOT_DIR, ims):
    sheet.alpha_composite(i, (x, 0)); i.save(f'/tmp/claude-0/molde/base_{d}.png'); x += i.width
sheet.save('/tmp/claude-0/molde/sheet.png')
