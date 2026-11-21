# Proceso de extracción de opiniones

Worker Service en .NET 8 que realiza la fase de extracción del ETL desde tres tipos de fuentes:

- CSV: encuestas internas de `surveys_part1.csv`.
- SQL Server: reseñas de `AnalisisOpinionesBD.dbo.web_reviews`.
- API REST: comentarios sociales obtenidos mediante `HttpClient`.

Los registros extraídos se guardan en el esquema `stg` de `AnalisisOpinionesDW`.

## Arquitectura

La solución aplica separación de responsabilidades:

- `Domain`: modelos de las tres fuentes.
- `Application`: contratos, resultados y orquestación.
- `Infrastructure`: extractores CSV, SQL y API, además de la persistencia staging.
- `Worker`: ejecución automática, configuración y ciclo de vida.
- `Api`: servicio REST local que representa la fuente de comentarios sociales.

Todos los extractores implementan `IExtractor<T>`, por lo que se pueden agregar nuevas fuentes sin modificar el Worker.

## Preparación

Requisitos:

- SDK .NET 8.
- SQL Server.
- Las bases `AnalisisOpinionesBD` y `AnalisisOpinionesDW` del proyecto anterior.

Ejecute una sola vez en SQL Server Management Studio:

```text
Database/Script-Staging-Extraccion.sql
```

El script crea las tablas:

- `stg.EjecucionesExtraccion`
- `stg.DetalleExtraccion`
- `stg.Encuestas`
- `stg.ResenasWeb`
- `stg.ComentariosApi`

## Configuración segura

Las opciones se encuentran en `AnalisisOpiniones.Extraction.Worker/appsettings.json`. Las cadenas de conexión no deben contener contraseñas cuando se suba el proyecto a GitHub.

Para usar secretos locales:

```powershell
dotnet user-secrets --project AnalisisOpiniones.Extraction.Worker set "Sources:Database:ConnectionString" "SU_CONEXION_TRANSACCIONAL"
dotnet user-secrets --project AnalisisOpiniones.Extraction.Worker set "Staging:ConnectionString" "SU_CONEXION_ANALITICA"
```

También se pueden usar variables de entorno:

```powershell
$env:Sources__Database__ConnectionString = "SU_CONEXION_TRANSACCIONAL"
$env:Staging__ConnectionString = "SU_CONEXION_ANALITICA"
```

## Ejecución

Primera terminal, iniciar la API local:

```powershell
dotnet run --project AnalisisOpiniones.Extraction.Api
```

La API queda disponible en `http://localhost:5084`:

- `GET /health`
- `GET /api/comments`

Segunda terminal, iniciar el Worker:

```powershell
dotnet run --project AnalisisOpiniones.Extraction.Worker
```

Con `Worker:RunOnce` en `true`, el proceso se ejecuta una vez y termina. Para ejecución periódica se cambia a `false` y se configura `Worker:IntervalMinutes`.

## Rendimiento y calidad

- Rendimiento: las tres extracciones se ejecutan en paralelo y la escritura utiliza `SqlBulkCopy`.
- Escalabilidad: cada fuente está aislada detrás de `IExtractor<T>`.
- Seguridad: se admiten User Secrets y variables de entorno.
- Mantenibilidad: separación por capas, inyección de dependencias y responsabilidades individuales.
- Trazabilidad: `ILogger`, medición con `Stopwatch` y tablas de auditoría.
- Integridad: transacciones, claves únicas y control de duplicados.

## Evidencias sugeridas

1. Compilación con cero errores.
2. API respondiendo en `/health` y `/api/comments`.
3. Consola del Worker mostrando el resumen de las tres fuentes.
4. Contenido de las tablas staging.
5. Registro completado en `stg.EjecucionesExtraccion` y su detalle.

Las consultas para estas evidencias están en `Database/Consultas-Validacion-Staging.sql`.
