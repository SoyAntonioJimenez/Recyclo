import os
from PIL import Image, ImageDraw, ImageFont

PROJ = r"C:\Users\soyan\Projects\\Recyclo"
DOCS = os.path.join(PROJ, "docs")
os.makedirs(DOCS, exist_ok=True)

FONT_B = r"C:\Windows\Fonts\segoeuib.ttf"
FONT_R = r"C:\Windows\Fonts\segoeui.ttf"
if not os.path.exists(FONT_B):
    FONT_B = r"C:\Windows\Fonts\arialbd.ttf"
if not os.path.exists(FONT_R):
    FONT_R = r"C:\Windows\Fonts\arial.ttf"

def font(path, size):
    return ImageFont.truetype(path, size)

# ---------- logo ----------
ico = Image.open(os.path.join(PROJ, "assets", "app.ico"))
ico.size = (256, 256)
logo = ico.convert("RGBA")

# ---------- hero ----------
W, H = 1280, 380
hero = Image.new("RGB", (W, H), "#0E1013")
d = ImageDraw.Draw(hero)

# degradado suave
for y in range(H):
    t = y / H
    r = int(14 + 10 * t)
    g = int(16 + 6 * t)
    b = int(19 + 14 * t)
    d.line([(0, y), (W, y)], fill=(r, g, b))

# halo suave detras del icono (desenfocado, sin bandas)
from PIL import ImageFilter
halo = Image.new("RGBA", (520, 520), (0, 0, 0, 0))
hd = ImageDraw.Draw(halo)
hd.ellipse([140, 140, 380, 380], fill=(242, 163, 60, 130))
halo = halo.filter(ImageFilter.GaussianBlur(70))
hero.paste(halo, (10, -60), halo)

icon_big = logo.resize((150, 150), Image.LANCZOS)
hero.paste(icon_big, (118, 115), icon_big)

d.text((300, 118), "RecycloTray", font=font(FONT_B, 76), fill=(245, 246, 248))
d.text((304, 208), "The recycle bin, where it belongs:", font=font(FONT_R, 30), fill=(160, 166, 175))
d.text((304, 248), "in your system tray.", font=font(FONT_R, 30), fill=(160, 166, 175))

hero.save(os.path.join(DOCS, "hero.png"), optimize=True)
print("hero.png")

# ---------- estados animados ----------
preview = Image.open(os.path.join(DOCS, "preview.png")).convert("RGBA")
empty = preview.crop((318, 30, 382, 94))
full = preview.crop((318, 114, 382, 178))

frames = []
for img, label in ((empty, "Empty"), (full, "Full")):
    f = Image.new("RGBA", (320, 320), (0x14, 0x16, 0x1A, 255))
    fd = ImageDraw.Draw(f)
    fd.rounded_rectangle([20, 20, 300, 300], radius=28, fill=(0x1E, 0x21, 0x26, 255))
    scaled = img.resize((160, 160), Image.NEAREST)
    f.alpha_composite(scaled, (80, 70))
    fd.text((110, 250), label, font=font(FONT_B, 26), fill=(0xF2, 0xA3, 0x3C))
    frames.append(f.convert("P", palette=Image.ADAPTIVE, colors=64))

frames[0].save(os.path.join(DOCS, "states.gif"), save_all=True, append_images=frames[1:],
               duration=1400, loop=0, optimize=True)
print("states.gif")

# ---------- captura real de la barra ----------
tray_src = os.path.join(os.environ["TEMP"], "tray-crop.png")
if os.path.exists(tray_src):
    t = Image.open(tray_src).convert("RGB")
    t = t.resize((t.width * 2, t.height * 2), Image.LANCZOS)
    t.save(os.path.join(DOCS, "tray.png"))
    print("tray.png")

