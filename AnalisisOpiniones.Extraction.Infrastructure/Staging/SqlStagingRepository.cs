using System.Data;
using AnalisisOpiniones.Extraction.Application.Abstractions;
using AnalisisOpiniones.Extraction.Application.Models;
using AnalisisOpiniones.Extraction.Domain.Models;
using AnalisisOpiniones.Extraction.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.Extraction.Infrastructure.Staging;

public sealed class SqlStagingRepository(IOptions<StagingOptions> options) : IStagingRepository
{
    public async Task<long> StartRunAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO stg.EjecucionesExtraccion (Estado)
            OUTPUT INSERTED.IdEjecucion
            VALUES ('EN_PROCESO');
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public Task<StagingWriteResult> SaveSurveysAsync(
        long runId,
        ExtractionBatch<SurveyRecord> batch,
        CancellationToken cancellationToken = default) =>
        SaveSurveysCoreAsync(runId, batch, cancellationToken);

    public Task<StagingWriteResult> SaveWebReviewsAsync(
        long runId,
        ExtractionBatch<WebReviewRecord> batch,
        CancellationToken cancellationToken = default) =>
        SaveWebReviewsCoreAsync(runId, batch, cancellationToken);

    public Task<StagingWriteResult> SaveSocialCommentsAsync(
        long runId,
        ExtractionBatch<SocialCommentRecord> batch,
        CancellationToken cancellationToken = default) =>
        SaveSocialCommentsCoreAsync(runId, batch, cancellationToken);

    public async Task CompleteRunAsync(
        long runId,
        ExtractionRunSummary summary,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE stg.EjecucionesExtraccion
            SET FechaFin = SYSUTCDATETIME(),
                Estado = 'COMPLETADO',
                RegistrosExtraidos = @Extraidos,
                RegistrosInsertados = @Insertados,
                RegistrosDuplicados = @Duplicados,
                RegistrosRechazados = @Rechazados,
                DuracionMs = @DuracionMs
            WHERE IdEjecucion = @IdEjecucion;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdEjecucion", SqlDbType.BigInt).Value = runId;
        command.Parameters.Add("@Extraidos", SqlDbType.Int).Value = summary.TotalExtracted;
        command.Parameters.Add("@Insertados", SqlDbType.Int).Value = summary.TotalInserted;
        command.Parameters.Add("@Duplicados", SqlDbType.Int).Value = summary.TotalDuplicates;
        command.Parameters.Add("@Rechazados", SqlDbType.Int).Value = summary.TotalRejected;
        command.Parameters.Add("@DuracionMs", SqlDbType.BigInt).Value =
            Convert.ToInt64(summary.TotalDuration.TotalMilliseconds);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task FailRunAsync(
        long runId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE stg.EjecucionesExtraccion
            SET FechaFin = SYSUTCDATETIME(),
                Estado = 'ERROR',
                MensajeError = LEFT(@MensajeError, 2000)
            WHERE IdEjecucion = @IdEjecucion;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdEjecucion", SqlDbType.BigInt).Value = runId;
        command.Parameters.Add("@MensajeError", SqlDbType.NVarChar, 2000).Value = errorMessage;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<StagingWriteResult> SaveSurveysCoreAsync(
        long runId,
        ExtractionBatch<SurveyRecord> batch,
        CancellationToken cancellationToken)
    {
        const string createTempTable = """
            CREATE TABLE #EncuestasBatch
            (
                IdOpinion VARCHAR(50) NOT NULL,
                IdCliente INT NULL,
                IdProducto INT NOT NULL,
                Fecha DATE NOT NULL,
                Comentario NVARCHAR(MAX) NOT NULL,
                Clasificacion NVARCHAR(20) NULL,
                PuntajeSatisfaccion TINYINT NULL,
                Fuente NVARCHAR(50) NOT NULL
            );
            """;
        const string insertSql = """
            INSERT INTO stg.Encuestas
                (IdEjecucion, IdOpinion, IdCliente, IdProducto, Fecha, Comentario,
                 Clasificacion, PuntajeSatisfaccion, Fuente)
            SELECT
                @IdEjecucion, b.IdOpinion, b.IdCliente, b.IdProducto, b.Fecha,
                b.Comentario, b.Clasificacion, b.PuntajeSatisfaccion, b.Fuente
            FROM #EncuestasBatch b
            WHERE NOT EXISTS
            (
                SELECT 1 FROM stg.Encuestas target WHERE target.IdOpinion = b.IdOpinion
            );
            SELECT @@ROWCOUNT;
            """;

        var table = CreateSurveyTable(batch.Records);
        return await SaveBatchAsync(
            runId,
            batch.Source,
            batch.Records.Count,
            batch.RejectedRecords,
            batch.Duration,
            "#EncuestasBatch",
            createTempTable,
            insertSql,
            table,
            cancellationToken);
    }

    private async Task<StagingWriteResult> SaveWebReviewsCoreAsync(
        long runId,
        ExtractionBatch<WebReviewRecord> batch,
        CancellationToken cancellationToken)
    {
        const string createTempTable = """
            CREATE TABLE #ResenasBatch
            (
                IdReview VARCHAR(50) NOT NULL,
                IdCliente INT NULL,
                IdProducto INT NOT NULL,
                Fecha DATE NOT NULL,
                Comentario NVARCHAR(MAX) NOT NULL,
                Rating TINYINT NULL
            );
            """;
        const string insertSql = """
            INSERT INTO stg.ResenasWeb
                (IdEjecucion, IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating)
            SELECT
                @IdEjecucion, b.IdReview, b.IdCliente, b.IdProducto,
                b.Fecha, b.Comentario, b.Rating
            FROM #ResenasBatch b
            WHERE NOT EXISTS
            (
                SELECT 1 FROM stg.ResenasWeb target WHERE target.IdReview = b.IdReview
            );
            SELECT @@ROWCOUNT;
            """;

        var table = CreateWebReviewTable(batch.Records);
        return await SaveBatchAsync(
            runId,
            batch.Source,
            batch.Records.Count,
            batch.RejectedRecords,
            batch.Duration,
            "#ResenasBatch",
            createTempTable,
            insertSql,
            table,
            cancellationToken);
    }

    private async Task<StagingWriteResult> SaveSocialCommentsCoreAsync(
        long runId,
        ExtractionBatch<SocialCommentRecord> batch,
        CancellationToken cancellationToken)
    {
        const string createTempTable = """
            CREATE TABLE #ComentariosBatch
            (
                IdComment VARCHAR(50) NOT NULL,
                IdCliente INT NULL,
                IdProducto INT NOT NULL,
                Fuente NVARCHAR(50) NOT NULL,
                Fecha DATE NOT NULL,
                Comentario NVARCHAR(MAX) NOT NULL
            );
            """;
        const string insertSql = """
            INSERT INTO stg.ComentariosApi
                (IdEjecucion, IdComment, IdCliente, IdProducto, Fuente, Fecha, Comentario)
            SELECT
                @IdEjecucion, b.IdComment, b.IdCliente, b.IdProducto,
                b.Fuente, b.Fecha, b.Comentario
            FROM #ComentariosBatch b
            WHERE NOT EXISTS
            (
                SELECT 1 FROM stg.ComentariosApi target WHERE target.IdComment = b.IdComment
            );
            SELECT @@ROWCOUNT;
            """;

        var table = CreateSocialCommentTable(batch.Records);
        return await SaveBatchAsync(
            runId,
            batch.Source,
            batch.Records.Count,
            batch.RejectedRecords,
            batch.Duration,
            "#ComentariosBatch",
            createTempTable,
            insertSql,
            table,
            cancellationToken);
    }

    private async Task<StagingWriteResult> SaveBatchAsync(
        long runId,
        string source,
        int extracted,
        int rejected,
        TimeSpan extractionDuration,
        string temporaryTable,
        string createTemporaryTableSql,
        string insertSql,
        DataTable data,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var createCommand = new SqlCommand(
                             createTemporaryTableSql,
                             connection,
                             transaction))
            {
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (data.Rows.Count > 0)
            {
                using var bulkCopy = new SqlBulkCopy(
                    connection,
                    SqlBulkCopyOptions.TableLock,
                    transaction)
                {
                    DestinationTableName = temporaryTable,
                    BatchSize = 1000,
                    BulkCopyTimeout = 60
                };
                foreach (DataColumn column in data.Columns)
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                await bulkCopy.WriteToServerAsync(data, cancellationToken);
            }

            int inserted;
            await using (var insertCommand = new SqlCommand(insertSql, connection, transaction))
            {
                insertCommand.Parameters.Add("@IdEjecucion", SqlDbType.BigInt).Value = runId;
                inserted = Convert.ToInt32(await insertCommand.ExecuteScalarAsync(cancellationToken));
            }

            var result = new StagingWriteResult(
                source,
                extracted,
                inserted,
                extracted - inserted,
                rejected,
                extractionDuration);
            await InsertDetailAsync(runId, result, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task InsertDetailAsync(
        long runId,
        StagingWriteResult result,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO stg.DetalleExtraccion
                (IdEjecucion, Fuente, RegistrosExtraidos, RegistrosInsertados,
                 RegistrosDuplicados, RegistrosRechazados, DuracionMs)
            VALUES
                (@IdEjecucion, @Fuente, @Extraidos, @Insertados,
                 @Duplicados, @Rechazados, @DuracionMs);
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@IdEjecucion", SqlDbType.BigInt).Value = runId;
        command.Parameters.Add("@Fuente", SqlDbType.NVarChar, 100).Value = result.Source;
        command.Parameters.Add("@Extraidos", SqlDbType.Int).Value = result.Extracted;
        command.Parameters.Add("@Insertados", SqlDbType.Int).Value = result.Inserted;
        command.Parameters.Add("@Duplicados", SqlDbType.Int).Value = result.Duplicates;
        command.Parameters.Add("@Rechazados", SqlDbType.Int).Value = result.Rejected;
        command.Parameters.Add("@DuracionMs", SqlDbType.BigInt).Value =
            Convert.ToInt64(result.ExtractionDuration.TotalMilliseconds);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(options.Value.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static DataTable CreateSurveyTable(IEnumerable<SurveyRecord> records)
    {
        var table = new DataTable();
        table.Columns.Add("IdOpinion", typeof(string));
        table.Columns.Add("IdCliente", typeof(int));
        table.Columns.Add("IdProducto", typeof(int));
        table.Columns.Add("Fecha", typeof(DateTime));
        table.Columns.Add("Comentario", typeof(string));
        table.Columns.Add("Clasificacion", typeof(string));
        table.Columns.Add("PuntajeSatisfaccion", typeof(byte));
        table.Columns.Add("Fuente", typeof(string));

        foreach (var record in records)
        {
            table.Rows.Add(
                record.IdOpinion,
                DbValue(record.IdCliente),
                record.IdProducto,
                record.Fecha.ToDateTime(TimeOnly.MinValue),
                record.Comentario,
                DbValue(record.Clasificacion),
                DbValue(record.PuntajeSatisfaccion),
                record.Fuente);
        }
        return table;
    }

    private static DataTable CreateWebReviewTable(IEnumerable<WebReviewRecord> records)
    {
        var table = new DataTable();
        table.Columns.Add("IdReview", typeof(string));
        table.Columns.Add("IdCliente", typeof(int));
        table.Columns.Add("IdProducto", typeof(int));
        table.Columns.Add("Fecha", typeof(DateTime));
        table.Columns.Add("Comentario", typeof(string));
        table.Columns.Add("Rating", typeof(byte));

        foreach (var record in records)
        {
            table.Rows.Add(
                record.IdReview,
                DbValue(record.IdCliente),
                record.IdProducto,
                record.Fecha.ToDateTime(TimeOnly.MinValue),
                record.Comentario,
                DbValue(record.Rating));
        }
        return table;
    }

    private static DataTable CreateSocialCommentTable(IEnumerable<SocialCommentRecord> records)
    {
        var table = new DataTable();
        table.Columns.Add("IdComment", typeof(string));
        table.Columns.Add("IdCliente", typeof(int));
        table.Columns.Add("IdProducto", typeof(int));
        table.Columns.Add("Fuente", typeof(string));
        table.Columns.Add("Fecha", typeof(DateTime));
        table.Columns.Add("Comentario", typeof(string));

        foreach (var record in records)
        {
            table.Rows.Add(
                record.IdComment,
                DbValue(record.IdCliente),
                record.IdProducto,
                record.Fuente,
                record.Fecha.ToDateTime(TimeOnly.MinValue),
                record.Comentario);
        }
        return table;
    }

    private static object DbValue<T>(T? value) => value is null ? DBNull.Value : value;
}
