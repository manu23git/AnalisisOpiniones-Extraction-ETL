namespace AnalisisOpiniones.Extraction.Domain.Models;

public sealed record SurveyRecord(
    string IdOpinion,
    int? IdCliente,
    int IdProducto,
    DateOnly Fecha,
    string Comentario,
    string? Clasificacion,
    byte? PuntajeSatisfaccion,
    string Fuente);
