namespace AnalisisOpiniones.Extraction.Domain.Models;

public sealed record SocialCommentRecord(
    string IdComment,
    int? IdCliente,
    int IdProducto,
    string Fuente,
    DateOnly Fecha,
    string Comentario);
