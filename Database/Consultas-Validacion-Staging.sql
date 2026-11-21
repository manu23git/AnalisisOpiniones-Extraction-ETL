USE AnalisisOpinionesDW;
GO

-- Últimas ejecuciones del Worker
SELECT TOP 10
    IdEjecucion,
    FechaInicio,
    FechaFin,
    Estado,
    RegistrosExtraidos,
    RegistrosInsertados,
    RegistrosDuplicados,
    RegistrosRechazados,
    DuracionMs,
    MensajeError
FROM stg.EjecucionesExtraccion
ORDER BY IdEjecucion DESC;

-- Resultado por fuente de la última ejecución
DECLARE @UltimaEjecucion BIGINT =
    (SELECT MAX(IdEjecucion) FROM stg.EjecucionesExtraccion);

SELECT
    Fuente,
    RegistrosExtraidos,
    RegistrosInsertados,
    RegistrosDuplicados,
    RegistrosRechazados,
    DuracionMs
FROM stg.DetalleExtraccion
WHERE IdEjecucion = @UltimaEjecucion
ORDER BY Fuente;

-- Cantidad almacenada en cada tabla staging
SELECT 'Encuestas CSV' AS Fuente, COUNT(*) AS Total FROM stg.Encuestas
UNION ALL
SELECT 'Reseñas SQL Server', COUNT(*) FROM stg.ResenasWeb
UNION ALL
SELECT 'Comentarios API REST', COUNT(*) FROM stg.ComentariosApi;

-- Muestras de los tres orígenes
SELECT TOP 10 * FROM stg.Encuestas ORDER BY IdStaging DESC;
SELECT TOP 10 * FROM stg.ResenasWeb ORDER BY IdStaging DESC;
SELECT TOP 10 * FROM stg.ComentariosApi ORDER BY IdStaging DESC;

-- Verificación de duplicados por identificador natural
SELECT IdOpinion, COUNT(*) AS Repeticiones
FROM stg.Encuestas
GROUP BY IdOpinion
HAVING COUNT(*) > 1;

SELECT IdReview, COUNT(*) AS Repeticiones
FROM stg.ResenasWeb
GROUP BY IdReview
HAVING COUNT(*) > 1;

SELECT IdComment, COUNT(*) AS Repeticiones
FROM stg.ComentariosApi
GROUP BY IdComment
HAVING COUNT(*) > 1;
