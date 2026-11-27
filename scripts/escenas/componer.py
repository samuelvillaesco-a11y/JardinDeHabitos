#!/usr/bin/env python3
"""Compone las escenas de fondo (1080x1920) con piezas CC0 de Kenney "Background Elements Redux"
recoloreadas a la paleta del jardín. Uso: componer.py <carpeta PNG/Retina de Kenney> <salida>
Requiere ImageMagick 7 (magick)."""
import subprocess, sys, os, tempfile
K, OUT = sys.argv[1], sys.argv[2]
W, H = 1080, 1920
os.makedirs(OUT, exist_ok=True)
tmp = tempfile.mkdtemp()

def pieza(nombre, alto, mod="100,100,100", tinte=None, voltear=False):
    """Escala una pieza a 'alto' px; -modulate brillo,saturación,tono; tinte opcional (color,porcentaje)."""
    dst = os.path.join(tmp, f"{nombre}-{alto}-{mod.replace(',','_')}-{tinte}-{voltear}.png")
    cmd = ["magick", os.path.join(K, nombre + ".png"), "-resize", f"x{alto}", "-modulate", mod]
    if tinte: cmd += ["-fill", tinte[0], "-colorize", str(tinte[1])]
    if voltear: cmd += ["-flop"]
    subprocess.run(cmd + [dst], check=True)
    return dst

def colina(color, y, amp, fase=0.0, ondas=1.5):
    """Colina suave: borde superior senoidal desde y, rellena hasta abajo."""
    import math
    pts = [(x, y + amp * math.sin((x / W) * math.pi * 2 * ondas / 2 + fase)) for x in range(0, W + 1, 20)]
    path = " ".join(f"{x},{v:.0f}" for x, v in pts) + f" {W},{H} 0,{H}"
    return ["-fill", color, "-draw", f"polygon {path}"]

def flores(centros, n=9, r=11, semilla=1):
    """Florecitas rosas y blancas sobre los arbustos (círculos con centro ocre)."""
    import random
    random.seed(semilla); cmd = []
    for (cx, cy, rx, ry) in centros:
        for _ in range(n):
            x, y = cx + random.uniform(-rx, rx), cy + random.uniform(-ry, ry)
            color = random.choice(["#F2B8C6", "#F7E7EC", "#E99AAE"])
            cmd += ["-fill", color, "-draw", f"circle {x:.0f},{y:.0f} {x+r:.0f},{y:.0f}",
                    "-fill", "#D9A441", "-draw", f"circle {x:.0f},{y:.0f} {x+r*0.35:.0f},{y:.0f}"]
    return cmd

def escena(nombre, cielo, capas, objetos, encima=()):
    cmd = ["magick", "-size", f"{W}x{H}", f"gradient:{cielo[0]}-{cielo[1]}"]
    for c in capas: cmd += c
    for (archivo, x, yBase) in objetos:  # yBase = donde apoya la pieza (borde inferior)
        alto = int(subprocess.run(["magick", "identify", "-format", "%h", archivo], capture_output=True, text=True).stdout)
        cmd += [archivo, "-geometry", f"+{x}+{yBase - alto}", "-composite"]
    cmd += list(encima)
    # Ilustración plana: pocos colores comprimen mucho mejor (sin tramado para no ensuciar los degradados).
    cmd += ["+dither", "-colors", "128", "-strip", os.path.join(OUT, f"escena_{nombre}.png")]
    subprocess.run(cmd, check=True)

nube = lambda n, alto, a: pieza(f"cloud{n}", alto, "104,40,100", ("#FFF6E4", a))
suave = "92,55,100"  # menos saturado: lo-fi

# 1. Jardín en flor: mañana cálida.
escena("jardin", ("#FBF1D6", "#F6E2A8"), [
    colina("#B9C98F", 1240, 40, 0.5), colina("#8DAA5C", 1340, 30, 2.0), colina("#5E8A3C", 1470, 25, 1.0)], [
    (pieza("sun", 190, "100,70,100", ("#FFE3A0", 30)), 760, 520),
    (nube(1, 170, 40), 40, 420), (nube(3, 130, 40), 620, 700),
    (pieza("treeLong", 420, suave, ("#6E8F4A", 25)), 90, 1290),
    (pieza("tree", 380, suave, ("#6E8F4A", 25)), 820, 1300),
    (pieza("bush1", 150, suave, ("#5E8A3C", 30)), 300, 1385),
    (pieza("bushAlt2", 120, suave, ("#7A9B4E", 30)), 640, 1380),
    (pieza("bush3", 120, suave, ("#6E9A4E", 30)), 160, 1400),
    (pieza("bush4", 110, suave, ("#6E9A4E", 30)), 860, 1395),
    *[(pieza("fence", 190, "95,60,100", ("#8A5A2B", 20)), x, 1565) for x in range(-20, W, 240)],
], encima=flores([(240, 1330, 90, 35), (920, 1330, 70, 30), (430, 1330, 60, 25)]))

# 2. Casa de campo: tarde dorada.
escena("casadecampo", ("#FBE3B4", "#F4C98A"), [
    colina("#C9B072", 1230, 35, 1.2), colina("#8E8F45", 1350, 30, 0.2), colina("#6E7E3A", 1490, 22, 2.4)], [
    (pieza("sun", 210, "100,70,100", ("#FFD08A", 35)), 120, 640),
    (nube(2, 150, 45), 600, 430),
    (pieza("houseAlt2", 430, "96,55,100", ("#C77B4A", 20)), 360, 1400),
    (pieza("tree", 360, suave, ("#5F7A3A", 25)), 120, 1370),
    (pieza("treeLong", 400, suave, ("#5F7A3A", 25)), 830, 1360),
    (pieza("bush2", 120, suave, ("#6E7E3A", 30)), 760, 1420),
    *[(pieza("fence", 170, "95,60,100", ("#8A5A2B", 20)), x, 1605) for x in range(-20, W, 215)],
])

# 3. Suburbio al atardecer.
escena("suburbio", ("#E9B7A0", "#F2A86B"), [
    colina("#B27A6A", 1300, 18, 0.4, 3), colina("#6B4E5C", 1420, 15, 1.9, 2), colina("#4A3A4E", 1540, 12, 0.7)], [
    (pieza("sun", 260, "100,60,100", ("#FFC98A", 40)), 410, 1330),
    (nube(4, 140, 55), 80, 520), (nube(6, 120, 55), 700, 760),
    (pieza("houseSmall1", 210, "75,40,100", ("#7A4E5A", 55)), 20, 1440),
    (pieza("house1", 330, "75,40,100", ("#7A4E5A", 55)), 250, 1440),
    (pieza("houseSmallAlt2", 220, "75,40,100", ("#7A4E5A", 55)), 560, 1440),
    (pieza("houseAlt1", 300, "75,40,100", ("#7A4E5A", 55)), 800, 1440),
    (pieza("treeLong", 360, "60,40,100", ("#3E3242", 50)), 180, 1500),
    (pieza("tree", 300, "60,40,100", ("#3E3242", 50)), 900, 1520),
])

# 4. Bosque en calma.
escena("bosque", ("#E8EDD2", "#C9D7A8"), [
    colina("#A7BA86", 1180, 50, 2.2), colina("#7E9A62", 1320, 35, 0.6), colina("#3F5E36", 1480, 25, 1.7)], [
    (nube(5, 150, 50), 520, 500),
    (pieza("treePine", 520, "85,50,100", ("#5C7C4A", 35)), 0, 1250),
    (pieza("treePine", 440, "80,50,100", ("#6E8A58", 35)), 300, 1220),
    (pieza("treePine", 600, "75,50,100", ("#4E6E40", 35)), 640, 1300),
    (pieza("treePine", 380, "85,50,100", ("#6E8A58", 35)), 900, 1230),
    (pieza("treePine", 640, "65,50,100", ("#35502E", 40)), 160, 1560),
    (pieza("treePine", 700, "60,50,100", ("#2F4A2A", 40)), 760, 1600),
    (pieza("bushAlt1", 130, "70,50,100", ("#3F5E36", 35)), 470, 1580),
])
print("listo")
