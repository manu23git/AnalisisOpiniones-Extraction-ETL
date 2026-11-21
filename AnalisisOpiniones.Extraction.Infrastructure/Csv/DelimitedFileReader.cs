using System.Text;

namespace AnalisisOpiniones.Extraction.Infrastructure.Csv;

internal sealed class DelimitedFileReader
{
    public async Task<IReadOnlyList<DelimitedRow>> ReadAsync(
        string path,
        IReadOnlyList<string> expectedHeaders,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró el archivo CSV requerido.", path);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            8192,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true);
        var parsedRows = await ParseAsync(reader, path, cancellationToken);

        if (parsedRows.Count == 0)
            throw new InvalidDataException($"El archivo '{path}' está vacío.");

        var headers = parsedRows[0].Values
            .Select((value, index) => index == 0 ? value.TrimStart('\uFEFF').Trim() : value.Trim())
            .ToArray();
        ValidateHeaders(path, headers, expectedHeaders);

        var rows = new List<DelimitedRow>(parsedRows.Count - 1);
        for (var index = 1; index < parsedRows.Count; index++)
        {
            var parsed = parsedRows[index];
            if (parsed.Values.Count == 1 && string.IsNullOrWhiteSpace(parsed.Values[0]))
                continue;

            if (parsed.Values.Count != headers.Length)
            {
                throw new InvalidDataException(
                    $"'{path}', fila {parsed.RowNumber}: se esperaban {headers.Length} columnas y se encontraron {parsed.Values.Count}.");
            }

            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var fieldIndex = 0; fieldIndex < headers.Length; fieldIndex++)
                fields.Add(headers[fieldIndex], parsed.Values[fieldIndex]);

            rows.Add(new DelimitedRow(parsed.RowNumber, fields));
        }

        return rows;
    }

    private static void ValidateHeaders(
        string path,
        IReadOnlyList<string> actual,
        IReadOnlyList<string> expected)
    {
        if (actual.Count != expected.Count)
            throw new InvalidDataException($"'{path}' no contiene la cantidad esperada de columnas.");

        for (var index = 0; index < expected.Count; index++)
        {
            if (!string.Equals(actual[index], expected[index], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"'{path}': la columna {index + 1} debe ser '{expected[index]}' y se recibió '{actual[index]}'.");
            }
        }
    }

    private static async Task<List<ParsedRow>> ParseAsync(
        TextReader reader,
        string path,
        CancellationToken cancellationToken)
    {
        var rows = new List<ParsedRow>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var physicalLine = 1;
        var rowStartLine = 1;
        var buffer = new char[1];

        while (await reader.ReadAsync(buffer.AsMemory(0, 1), cancellationToken) > 0)
        {
            var character = buffer[0];
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        await reader.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
                        field.Append('"');
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                    if (character == '\n')
                        physicalLine++;
                }
                continue;
            }

            switch (character)
            {
                case '"' when field.Length == 0:
                    inQuotes = true;
                    break;
                case '"':
                    throw new InvalidDataException($"'{path}', fila {physicalLine}: comilla inesperada.");
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    if (reader.Peek() == '\n')
                        await reader.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
                    AddRow(rows, fields, field, rowStartLine);
                    physicalLine++;
                    rowStartLine = physicalLine;
                    break;
                case '\n':
                    AddRow(rows, fields, field, rowStartLine);
                    physicalLine++;
                    rowStartLine = physicalLine;
                    break;
                default:
                    field.Append(character);
                    break;
            }
        }

        if (inQuotes)
            throw new InvalidDataException($"'{path}', fila {rowStartLine}: campo entre comillas sin cerrar.");

        if (field.Length > 0 || fields.Count > 0)
            AddRow(rows, fields, field, rowStartLine);

        return rows;
    }

    private static void AddRow(
        ICollection<ParsedRow> rows,
        ICollection<string> fields,
        StringBuilder field,
        int rowNumber)
    {
        fields.Add(field.ToString());
        field.Clear();
        rows.Add(new ParsedRow(rowNumber, [.. fields]));
        fields.Clear();
    }

    private sealed record ParsedRow(int RowNumber, IReadOnlyList<string> Values);
}

internal sealed record DelimitedRow(
    int RowNumber,
    IReadOnlyDictionary<string, string> Fields)
{
    public string this[string name] => Fields[name];
}
