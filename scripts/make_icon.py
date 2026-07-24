#!/usr/bin/env python3
"""Generates src/QuickFiles/Assets/QuickFiles.ico (blue tile + white document)."""
import os
from PIL import Image, ImageDraw

S = 256
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# Blue rounded-square tile
d.rounded_rectangle([8, 8, S - 8, S - 8], radius=52, fill=(37, 99, 235, 255))

# White document with folded corner
dx0, dy0, dx1, dy1 = 76, 52, 180, 204
fold = 30
d.polygon(
    [(dx0, dy0), (dx1 - fold, dy0), (dx1, dy0 + fold), (dx1, dy1), (dx0, dy1)],
    fill=(255, 255, 255, 255),
)
d.polygon(
    [(dx1 - fold, dy0), (dx1 - fold, dy0 + fold), (dx1, dy0 + fold)],
    fill=(191, 219, 254, 255),
)

# Text lines on the document
for i, y in enumerate(range(dy0 + 52, dy1 - 24, 26)):
    w = 72 if i % 2 == 0 else 56
    d.rounded_rectangle([dx0 + 16, y, dx0 + 16 + w, y + 10], radius=5,
                        fill=(37, 99, 235, 255))

# Small clock badge, bottom-right of the tile
cx, cy, r = 178, 178, 44
d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(30, 64, 175, 255),
          outline=(255, 255, 255, 255), width=8)
d.line([cx, cy, cx, cy - 22], fill=(255, 255, 255, 255), width=8)
d.line([cx, cy, cx + 16, cy + 8], fill=(255, 255, 255, 255), width=8)

out = os.path.join(os.path.dirname(__file__), "..", "src", "QuickFiles", "Assets", "QuickFiles.ico")
os.makedirs(os.path.dirname(out), exist_ok=True)
img.save(out, sizes=[(256, 256), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)])
print("wrote", os.path.abspath(out))
