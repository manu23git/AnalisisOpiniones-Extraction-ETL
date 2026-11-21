from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Diagramas"
W, H = 1800, 1120

NAVY = "#17324D"
BLUE = "#2A6F97"
TEAL = "#238A8D"
GOLD = "#CF8B17"
RED = "#B84A4A"
TEXT = "#203247"
MUTED = "#5C6E80"
BG = "#F7FAFC"
WHITE = "#FFFFFF"
PALE_BLUE = "#E8F2F8"
PALE_TEAL = "#E6F4F2"
PALE_GOLD = "#FFF4D6"
PALE_RED = "#FBEAEA"
LINE = "#71869A"


def font(size, bold=False):
    directory = Path("C:/Windows/Fonts")
    names = ["segoeuib.ttf" if bold else "segoeui.ttf", "arialbd.ttf" if bold else "arial.ttf"]
    for name in names:
        path = directory / name
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


TITLE = font(42, True)
SUBTITLE = font(21)
H2 = font(26, True)
BODY = font(20)
SMALL = font(17)
BADGE = font(16, True)


def centered(draw, text, y, fnt, fill):
    box = draw.textbbox((0, 0), text, font=fnt)
    draw.text(((W - (box[2] - box[0])) / 2, y), text, font=fnt, fill=fill)


def card(draw, box, title, lines, color=BLUE, fill=PALE_BLUE, badge=None):
    x1, y1, x2, y2 = box
    draw.rounded_rectangle(box, radius=18, fill=fill, outline=color, width=3)
    draw.rounded_rectangle((x1, y1, x2, y1 + 56), radius=18, fill=color)
    draw.rectangle((x1, y1 + 38, x2, y1 + 56), fill=color)
    draw.text((x1 + 20, y1 + 13), title, font=H2, fill=WHITE)
    y = y1 + 76
    for line in lines:
        draw.text((x1 + 20, y), line, font=BODY, fill=TEXT)
        y += 32
    if badge:
        badge_box = draw.textbbox((0, 0), badge, font=BADGE)
        width = badge_box[2] - badge_box[0] + 24
        draw.rounded_rectangle((x2 - width - 16, y1 + 13, x2 - 16, y1 + 43), radius=10, fill=WHITE)
        draw.text((x2 - width - 4, y1 + 18), badge, font=BADGE, fill=color)


def arrow(draw, start, end, color=LINE, width=4, dashed=False):
    x1, y1 = start
    x2, y2 = end
    if dashed:
        segments = 12
        for i in range(segments):
            if i % 2 == 0:
                a = i / segments
                b = (i + 1) / segments
                draw.line((x1 + (x2 - x1) * a, y1 + (y2 - y1) * a,
                           x1 + (x2 - x1) * b, y1 + (y2 - y1) * b), fill=color, width=width)
    else:
        draw.line((x1, y1, x2, y2), fill=color, width=width)
    import math
    angle = math.atan2(y2 - y1, x2 - x1)
    size = 14
    points = [
        (x2, y2),
        (x2 - size * math.cos(angle - 0.55), y2 - size * math.sin(angle - 0.55)),
        (x2 - size * math.cos(angle + 0.55), y2 - size * math.sin(angle + 0.55)),
    ]
    draw.polygon(points, fill=color)


def architecture():
    img = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(img)
    centered(draw, "Arquitectura del proceso de extracción", 24, TITLE, NAVY)
    centered(draw, "Worker Service .NET 8 · CSV + SQL Server + API REST · staging analítico", 78, SUBTITLE, MUTED)

    draw.text((70, 138), "FUENTES", font=H2, fill=NAVY)
    card(draw, (70, 185, 470, 350), "Encuestas CSV", ["surveys_part1.csv", "Lectura y validación"], BLUE, PALE_BLUE, "CSV")
    card(draw, (70, 405, 470, 570), "Base relacional", ["AnalisisOpinionesBD", "dbo.web_reviews"], TEAL, PALE_TEAL, "SQL")
    card(draw, (70, 625, 470, 790), "API REST", ["GET /api/comments", "IHttpClientFactory"], GOLD, PALE_GOLD, "HTTP")

    draw.text((625, 138), "WORKER SERVICE", font=H2, fill=NAVY)
    draw.rounded_rectangle((585, 180, 1180, 855), radius=24, fill=WHITE, outline=NAVY, width=4)
    card(draw, (635, 225, 1130, 385), "Extractores", ["IExtractor<T>", "Implementaciones independientes"], BLUE, PALE_BLUE)
    card(draw, (635, 420, 1130, 570), "Orquestación", ["Tareas asíncronas en paralelo", "CancellationToken + Stopwatch"], NAVY, "#EDF1F5")
    card(draw, (635, 605, 1130, 755), "Persistencia staging", ["IStagingRepository", "SqlBulkCopy + transacciones"], TEAL, PALE_TEAL)
    draw.rounded_rectangle((635, 790, 1130, 850), radius=14, fill=PALE_GOLD, outline=GOLD, width=2)
    draw.text((660, 806), "ILogger · configuración JSON · variables de entorno", font=BODY, fill=TEXT)

    draw.text((1285, 138), "DESTINO Y CONSUMO", font=H2, fill=NAVY)
    card(draw, (1260, 185, 1730, 430), "AnalisisOpinionesDW", ["stg.Encuestas", "stg.ResenasWeb", "stg.ComentariosApi", "stg.EjecucionesExtraccion"], TEAL, PALE_TEAL, "STAGING")
    card(draw, (1260, 500, 1730, 665), "Modelo estrella", ["FactOpinion", "Dimensiones analíticas"], NAVY, "#EDF1F5", "ETL")
    card(draw, (1260, 735, 1730, 900), "Dashboard", ["Indicadores y análisis", "por fuente y sentimiento"], GOLD, PALE_GOLD, "BI")

    arrow(draw, (470, 268), (635, 268))
    arrow(draw, (470, 488), (635, 290))
    arrow(draw, (470, 708), (635, 315))
    arrow(draw, (1130, 680), (1260, 300), TEAL)
    arrow(draw, (1495, 430), (1495, 500), NAVY, dashed=True)
    arrow(draw, (1495, 665), (1495, 735), GOLD)

    draw.rounded_rectangle((270, 955, 1530, 1055), radius=16, fill=WHITE, outline="#CBD6E0", width=2)
    draw.text((300, 970), "Atributos de calidad", font=H2, fill=NAVY)
    draw.text((575, 978), "Rendimiento", font=BADGE, fill=BLUE)
    draw.text((715, 978), "Escalabilidad", font=BADGE, fill=TEAL)
    draw.text((855, 978), "Seguridad", font=BADGE, fill=GOLD)
    draw.text((970, 978), "Mantenibilidad", font=BADGE, fill=NAVY)
    draw.text((575, 1012), "Paralelismo · fuentes modulares · secretos externos · capas y SOLID", font=SMALL, fill=MUTED)

    img.save(OUT / "Arquitectura-Extraccion.png", optimize=True)


def flow():
    flow_height = 1320
    img = Image.new("RGB", (W, flow_height), BG)
    draw = ImageDraw.Draw(img)
    centered(draw, "Flujo del proceso de extracción", 24, TITLE, NAVY)
    centered(draw, "Una ejecución del Worker Service desde la configuración hasta la auditoría", 78, SUBTITLE, MUTED)

    def node(x, y, width, height, title, subtitle="", color=BLUE, fill=PALE_BLUE):
        draw.rounded_rectangle((x, y, x + width, y + height), radius=18, fill=fill, outline=color, width=3)
        title_box = draw.textbbox((0, 0), title, font=H2)
        draw.text((x + (width - (title_box[2] - title_box[0])) / 2, y + 18), title, font=H2, fill=color)
        if subtitle:
            sub_box = draw.textbbox((0, 0), subtitle, font=SMALL)
            draw.text((x + (width - (sub_box[2] - sub_box[0])) / 2, y + 58), subtitle, font=SMALL, fill=MUTED)

    node(700, 130, 400, 90, "Inicio", "Worker activado", NAVY, "#EDF1F5")
    node(650, 275, 500, 105, "Configuración", "rutas · conexiones · API · intervalo", BLUE, PALE_BLUE)
    node(650, 435, 500, 105, "Registrar ejecución", "Estado EN_PROCESO", TEAL, PALE_TEAL)

    node(80, 650, 430, 125, "CSV", "Leer y validar encuestas", BLUE, PALE_BLUE)
    node(685, 650, 430, 125, "SQL Server", "Consultar reseñas web", TEAL, PALE_TEAL)
    node(1290, 650, 430, 125, "API REST", "Consumir comentarios", GOLD, PALE_GOLD)

    node(650, 845, 500, 105, "Consolidar resultados", "extraídos · rechazados · duración", NAVY, "#EDF1F5")
    node(1230, 845, 490, 105, "Error controlado", "ILogger + estado ERROR", RED, PALE_RED)
    node(650, 995, 500, 90, "Carga staging y resumen", "SqlBulkCopy · auditoría", TEAL, PALE_TEAL)
    node(650, 1160, 500, 105, "Fin", "ejecución completada o con error", NAVY, "#EDF1F5")

    arrow(draw, (900, 220), (900, 275))
    arrow(draw, (900, 380), (900, 435))
    arrow(draw, (900, 540), (295, 650))
    arrow(draw, (900, 540), (900, 650))
    arrow(draw, (900, 540), (1505, 650))
    arrow(draw, (295, 775), (770, 845))
    arrow(draw, (900, 775), (900, 845))
    arrow(draw, (1505, 775), (1030, 845))
    arrow(draw, (900, 950), (900, 995), TEAL)
    arrow(draw, (900, 1085), (900, 1160), TEAL)
    arrow(draw, (1115, 712), (1230, 890), RED, dashed=True)
    arrow(draw, (1475, 950), (1150, 1212), RED, dashed=True)

    img.save(OUT / "Flujo-Extraccion.png", optimize=True)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    architecture()
    flow()
    print(OUT / "Arquitectura-Extraccion.png")
    print(OUT / "Flujo-Extraccion.png")
