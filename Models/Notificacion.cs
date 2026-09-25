namespace GestionCreditos.Models;

public class Notificacion
{
    public int Id { get; set; }

    /// <summary>UUID del mensaje RabbitMQ para idempotencia.</summary>
    public string MessageId { get; set; } = string.Empty;

    public int SolicitudId { get; set; }

    public SolicitudCredito? Solicitud { get; set; }

    public string UsuarioId { get; set; } = string.Empty;

    public string Texto { get; set; } = string.Empty;

    public DateTime FechaProcesamientoUtc { get; set; }
}
