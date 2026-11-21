namespace AnalisisOpiniones.Extraction.Domain.Models;

public sealed record WebReviewRecord(
    string IdReview,
    int? IdCliente,
    int IdProducto,
    DateOnly Fecha,
    string Comentario,
    byte? Rating);
