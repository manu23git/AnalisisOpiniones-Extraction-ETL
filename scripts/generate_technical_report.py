from pathlib import Path

from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Documentacion" / "Informe-Tecnico-Extraccion.docx"
ARCHITECTURE = ROOT / "Diagramas" / "Arquitectura-Extraccion.png"
FLOW = ROOT / "Diagramas" / "Flujo-Extraccion.png"

BLACK = "000000"
GRAY = "666666"
LIGHT_GRAY = "F2F2F2"
BORDER = "D9D9D9"


def set_run_font(run, name="Calibri", size=13, bold=False, italic=False, color=BLACK):
    run.font.name = name
    fonts = run._element.get_or_add_rPr().get_or_add_rFonts()
    fonts.set(qn("w:ascii"), name)
    fonts.set(qn("w:hAnsi"), name)
    fonts.set(qn("w:eastAsia"), name)
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic
    run.font.color.rgb = RGBColor.from_string(color)


def remove_title_border(style):
    properties = style.element.get_or_add_pPr()
    border = properties.find(qn("w:pBdr"))
    if border is not None:
        properties.remove(border)


def configure_styles(document):
    normal = document.styles["Normal"]
    normal.font.name = "Calibri"
    normal.font.size = Pt(13)
    normal.font.color.rgb = RGBColor.from_string(BLACK)
    normal.paragraph_format.line_spacing = 1.3
    normal.paragraph_format.space_after = Pt(8)

    title = document.styles["Title"]
    title.font.name = "Calibri"
    title.font.size = Pt(26)
    title.font.bold = True
    title.font.color.rgb = RGBColor.from_string(BLACK)
    remove_title_border(title)

    heading_1 = document.styles["Heading 1"]
    heading_1.font.name = "Calibri"
    heading_1.font.size = Pt(16)
    heading_1.font.bold = True
    heading_1.font.color.rgb = RGBColor.from_string(BLACK)
    heading_1.paragraph_format.space_before = Pt(10)
    heading_1.paragraph_format.space_after = Pt(5)
    heading_1.paragraph_format.keep_with_next = True

    heading_2 = document.styles["Heading 2"]
    heading_2.font.name = "Calibri"
    heading_2.font.size = Pt(13)
    heading_2.font.bold = True
    heading_2.font.color.rgb = RGBColor.from_string(BLACK)
    heading_2.paragraph_format.space_before = Pt(8)
    heading_2.paragraph_format.space_after = Pt(3)
    heading_2.paragraph_format.keep_with_next = True

    for style_name in ("List Bullet", "List Number"):
        style = document.styles[style_name]
        style.font.name = "Calibri"
        style.font.size = Pt(13)
        style.font.color.rgb = RGBColor.from_string(BLACK)
        style.paragraph_format.line_spacing = 1.25
        style.paragraph_format.space_after = Pt(6)


def add_cover_field(document, label, value, first=False):
    label_paragraph = document.add_paragraph()
    label_paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
    label_paragraph.paragraph_format.space_before = Pt(12 if first else 9)
    label_paragraph.paragraph_format.space_after = Pt(2)
    label_paragraph.paragraph_format.keep_with_next = True
    set_run_font(label_paragraph.add_run(f"{label}:"), size=15, bold=True)

    value_paragraph = document.add_paragraph()
    value_paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
    value_paragraph.paragraph_format.space_after = Pt(11)
    set_run_font(value_paragraph.add_run(value), size=13.5)


def add_heading(document, text, level=1):
    heading = document.add_heading(text, level=level)
    heading.paragraph_format.keep_with_next = True
    return heading


def add_labeled_bullet(document, label, text):
    paragraph = document.add_paragraph()
    paragraph.paragraph_format.left_indent = Cm(0.85)
    paragraph.paragraph_format.first_line_indent = Cm(-0.5)
    paragraph.paragraph_format.line_spacing = 1.25
    paragraph.paragraph_format.space_after = Pt(6)
    set_run_font(paragraph.add_run("•  "))
    set_run_font(paragraph.add_run(f"{label}: "), bold=True)
    set_run_font(paragraph.add_run(text))
    return paragraph


def add_numbered_step(document, number, text):
    paragraph = document.add_paragraph()
    paragraph.paragraph_format.left_indent = Cm(0.7)
    paragraph.paragraph_format.first_line_indent = Cm(-0.7)
    paragraph.paragraph_format.space_after = Pt(6)
    set_run_font(paragraph.add_run(f"{number}. "), bold=True)
    set_run_font(paragraph.add_run(text))
    return paragraph


def set_cell_shading(cell, fill):
    properties = cell._tc.get_or_add_tcPr()
    shading = properties.find(qn("w:shd"))
    if shading is None:
        shading = OxmlElement("w:shd")
        properties.append(shading)
    shading.set(qn("w:fill"), fill)


def set_cell_margins(cell, top=120, start=150, bottom=120, end=150):
    properties = cell._tc.get_or_add_tcPr()
    margins = properties.first_child_found_in("w:tcMar")
    if margins is None:
        margins = OxmlElement("w:tcMar")
        properties.append(margins)
    for name, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = margins.find(qn(f"w:{name}"))
        if node is None:
            node = OxmlElement(f"w:{name}")
            margins.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_table_borders(table, color=BORDER, size="6"):
    properties = table._tbl.tblPr
    borders = properties.first_child_found_in("w:tblBorders")
    if borders is None:
        borders = OxmlElement("w:tblBorders")
        properties.append(borders)
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        node = borders.find(qn(f"w:{edge}"))
        if node is None:
            node = OxmlElement(f"w:{edge}")
            borders.append(node)
        node.set(qn("w:val"), "single")
        node.set(qn("w:sz"), size)
        node.set(qn("w:color"), color)


def add_code_block(document, lines):
    table = document.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = True
    set_table_borders(table)
    cell = table.cell(0, 0)
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    set_cell_shading(cell, LIGHT_GRAY)
    set_cell_margins(cell)
    cell.text = ""
    for index, line in enumerate(lines):
        paragraph = cell.paragraphs[0] if index == 0 else cell.add_paragraph()
        paragraph.paragraph_format.space_after = Pt(0)
        paragraph.paragraph_format.line_spacing = 1.0
        set_run_font(paragraph.add_run(line), name="Consolas", size=9.5)
    spacer = document.add_paragraph()
    spacer.paragraph_format.space_after = Pt(1)


def add_figure(document, image_path, width, caption_text):
    paragraph = document.add_paragraph()
    paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
    paragraph.paragraph_format.keep_with_next = True
    paragraph.add_run().add_picture(str(image_path), width=width)

    caption = document.add_paragraph()
    caption.alignment = WD_ALIGN_PARAGRAPH.CENTER
    caption.paragraph_format.space_before = Pt(3)
    caption.paragraph_format.space_after = Pt(4)
    set_run_font(caption.add_run(caption_text), size=10.5, italic=True, color=GRAY)


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    document = Document()
    section = document.sections[0]
    section.page_width = Cm(21)
    section.page_height = Cm(29.7)
    section.top_margin = Cm(2.5)
    section.bottom_margin = Cm(2.5)
    section.left_margin = Cm(2.8)
    section.right_margin = Cm(2.8)
    configure_styles(document)

    title = document.add_paragraph(style="Title")
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    title.paragraph_format.space_before = Pt(18)
    title.paragraph_format.space_after = Pt(25)
    set_run_font(title.add_run("PRESENTACIÓN"), size=26, bold=True)

    add_cover_field(document, "Nombre", "Manuel Fernández", first=True)
    add_cover_field(document, "Matrícula", "2025-1085")
    add_cover_field(document, "Institución", "Instituto Tecnológico de las Américas (ITLA)")
    add_cover_field(document, "Asignatura", "Electiva 1 (Big Data)")
    add_cover_field(document, "Maestro", "Francis Ramírez")
    add_cover_field(document, "Tema", "Desarrollo del proceso de extracción ETL en .NET")
    add_cover_field(document, "Fecha", "20 de noviembre de 2026")
    document.add_page_break()

    add_heading(document, "Proceso de extracción ETL en .NET")
    document.add_paragraph(
        "La solución implementa la fase de extracción de un proceso ETL para el sistema de análisis de "
        "opiniones de clientes. Un Worker Service en .NET 8 obtiene información desde un archivo CSV, "
        "una base de datos relacional y una API REST. Los registros extraídos se almacenan temporalmente "
        "en tablas staging para su posterior transformación y carga al modelo analítico."
    )
    document.add_paragraph(
        "El proyecto está separado del sistema transaccional existente. Esta organización permite desarrollar "
        "y ejecutar la extracción sin modificar la aplicación anterior ni mezclar sus responsabilidades."
    )

    add_heading(document, "Fuentes y destinos")
    add_labeled_bullet(document, "Archivo CSV", "surveys_part1.csv es leído por SurveyCsvExtractor y sus registros se guardan en stg.Encuestas.")
    add_labeled_bullet(document, "Base relacional", "AnalisisOpinionesBD.dbo.web_reviews es consultada por DatabaseReviewExtractor y se almacena en stg.ResenasWeb.")
    add_labeled_bullet(document, "API REST", "GET /api/comments es consumido por ApiSocialCommentExtractor y su respuesta se guarda en stg.ComentariosApi.")

    add_heading(document, "Estructura de la solución")
    add_labeled_bullet(document, "Domain", "define los modelos de encuestas, reseñas y comentarios.")
    add_labeled_bullet(document, "Application", "contiene los contratos, los resultados y el orquestador de extracción.")
    add_labeled_bullet(document, "Infrastructure", "implementa los extractores y la escritura masiva en SQL Server.")
    add_labeled_bullet(document, "Worker", "controla el ciclo de ejecución, la periodicidad y el cierre de la aplicación.")
    add_labeled_bullet(document, "Api", "expone los comentarios sociales como una fuente REST local y reproducible.")
    document.add_page_break()

    add_heading(document, "Arquitectura del proceso")
    document.add_paragraph(
        "Las fuentes se conectan al Worker mediante implementaciones de IExtractor. El orquestador inicia "
        "las tres extracciones en paralelo, registra el tiempo de ejecución y entrega cada lote al repositorio "
        "staging. La persistencia utiliza transacciones y SqlBulkCopy para reducir el costo de las inserciones."
    )
    add_figure(document, ARCHITECTURE, Cm(15.4), "Figura 1. Arquitectura del proceso de extracción")
    document.add_page_break()

    add_heading(document, "Flujo de ejecución")
    document.add_paragraph(
        "Cada ejecución comienza con la lectura de la configuración y el registro de una fila de auditoría. "
        "Las tres fuentes se consultan al mismo tiempo. Cuando finalizan correctamente, el Worker consolida "
        "los resultados, inserta los lotes en staging y marca la ejecución como completada. Si ocurre una "
        "excepción, ILogger registra el error y la auditoría conserva el estado correspondiente."
    )
    add_figure(document, FLOW, Cm(15.1), "Figura 2. Flujo de una ejecución del Worker Service")
    document.add_page_break()

    add_heading(document, "Componentes principales")
    add_labeled_bullet(document, "SurveyCsvExtractor", "lee el CSV y valida encabezados, tipos, campos obligatorios e identificadores duplicados.")
    add_labeled_bullet(document, "DatabaseReviewExtractor", "ejecuta una consulta asíncrona sobre dbo.web_reviews mediante Microsoft.Data.SqlClient.")
    add_labeled_bullet(document, "ApiSocialCommentExtractor", "consume la API con IHttpClientFactory, valida la respuesta y elimina identificadores repetidos.")
    add_labeled_bullet(document, "ExtractionOrchestrator", "coordina las fuentes con Task.WhenAll y consolida los resultados.")
    add_labeled_bullet(document, "SqlStagingRepository", "inserta los lotes con SqlBulkCopy, transacciones y auditoría por fuente.")
    add_labeled_bullet(document, "ExtractionWorker", "ejecuta el proceso una sola vez o mediante un intervalo configurable.")

    add_heading(document, "Evidencia del código")
    document.add_paragraph("La interfaz genérica separa la aplicación de la tecnología utilizada por cada fuente.")
    add_code_block(document, [
        "public interface IExtractor<TRecord>",
        "{",
        "    string SourceName { get; }",
        "    Task<ExtractionBatch<TRecord>> ExtractAsync(CancellationToken cancellationToken);",
        "}",
    ])
    document.add_paragraph(
        "El orquestador inicia las tres extracciones antes de esperar sus resultados, por lo que las fuentes "
        "pueden trabajar al mismo tiempo."
    )
    add_code_block(document, [
        "var surveyTask = surveyExtractor.ExtractAsync(cancellationToken);",
        "var reviewTask = webReviewExtractor.ExtractAsync(cancellationToken);",
        "var commentTask = socialCommentExtractor.ExtractAsync(cancellationToken);",
        "await Task.WhenAll(surveyTask, reviewTask, commentTask);",
    ])
    document.add_page_break()

    add_heading(document, "Atributos de calidad")
    add_heading(document, "Rendimiento", level=2)
    document.add_paragraph(
        "El Worker ejecuta las tres fuentes mediante tareas asíncronas y mide cada operación con Stopwatch. "
        "La persistencia utiliza SqlBulkCopy para enviar un lote por fuente en lugar de abrir una operación por registro."
    )
    add_heading(document, "Escalabilidad", level=2)
    document.add_paragraph(
        "Cada origen implementa IExtractor<T>. Para agregar una fuente se crea su modelo y su implementación, "
        "y luego se registra mediante inyección de dependencias. Los extractores existentes no requieren cambios."
    )
    add_heading(document, "Seguridad", level=2)
    document.add_paragraph(
        "Las rutas, URL y conexiones se centralizan en la configuración. El proyecto admite User Secrets y "
        "variables de entorno para evitar que las contraseñas se almacenen en appsettings.json o se publiquen en GitHub."
    )
    add_heading(document, "Mantenibilidad", level=2)
    document.add_paragraph(
        "La solución separa dominio, aplicación, infraestructura, proceso hospedado y API. Esta división limita "
        "la responsabilidad de cada clase y permite sustituir componentes de manera independiente."
    )
    add_heading(document, "Errores y trazabilidad", level=2)
    document.add_paragraph(
        "ILogger registra el inicio, el final, las cantidades procesadas y las excepciones. Las tablas de "
        "EjecucionesExtraccion y DetalleExtraccion conservan el estado, la duración y el resultado de cada fuente. "
        "Las transacciones y las claves únicas evitan lotes incompletos y registros duplicados."
    )

    add_heading(document, "Configuración")
    add_labeled_bullet(document, "Worker", "define si el proceso se ejecuta una vez o según un intervalo en minutos.")
    add_labeled_bullet(document, "Sources Csv", "contiene la ruta del archivo de encuestas.")
    add_labeled_bullet(document, "Sources Database", "contiene la conexión de AnalisisOpinionesBD.")
    add_labeled_bullet(document, "Sources Api", "define la URL, el endpoint y el tiempo de espera.")
    add_labeled_bullet(document, "Staging", "contiene la conexión de AnalisisOpinionesDW.")

    add_heading(document, "Preparación y ejecución")
    for number, step in enumerate([
        "Ejecutar Database/Script-Staging-Extraccion.sql en AnalisisOpinionesDW.",
        "Configurar las conexiones mediante User Secrets o variables de entorno.",
        "Iniciar AnalisisOpiniones.Extraction.Api en http://localhost:5084.",
        "Iniciar AnalisisOpiniones.Extraction.Worker.",
        "Revisar la consola y ejecutar Database/Consultas-Validacion-Staging.sql.",
    ], start=1):
        add_numbered_step(document, number, step)

    add_heading(document, "Validaciones realizadas")
    add_labeled_bullet(document, "Compilación", "la solución completa compiló en .NET 8 con cero errores y cero advertencias.")
    add_labeled_bullet(document, "Estado de la API", "el endpoint /health respondió correctamente.")
    add_labeled_bullet(document, "Datos de la API", "el endpoint /api/comments devolvió 200 comentarios sociales.")
    add_labeled_bullet(document, "Base de datos", "el script staging no fue ejecutado y las bases del estudiante no fueron modificadas.")

    add_heading(document, "Evidencias para la entrega")
    add_labeled_bullet(document, "Compilación", "captura del resultado correcto y sin errores.")
    add_labeled_bullet(document, "API", "captura de las respuestas de /health y /api/comments.")
    add_labeled_bullet(document, "Worker", "captura del resumen obtenido después de una ejecución real.")
    add_labeled_bullet(document, "SQL Server", "captura de las tablas staging y de la auditoría de ejecución.")

    add_heading(document, "Resultado del diseño")
    document.add_paragraph(
        "La solución cubre la extracción solicitada mediante un Worker Service en .NET 8. Admite archivos CSV, "
        "SQL Server y una API REST, registra los eventos con ILogger y conserva los datos en staging. La "
        "organización por capas permite incorporar nuevas fuentes sin acoplarlas al ciclo de ejecución."
    )

    document.core_properties.title = "Desarrollo del proceso de extracción ETL en .NET"
    document.core_properties.subject = "Worker Service .NET 8 para la fase de extracción ETL"
    document.core_properties.author = "Manuel Fernández"
    document.core_properties.keywords = "ETL, extracción, Worker Service, .NET 8, SQL Server, API REST"
    document.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    build()
