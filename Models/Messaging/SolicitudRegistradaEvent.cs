namespace GestionCreditos.Models.Messaging;

public class SolicitudRegistradaEvent
{
    public string Tipo { get; set; } = "SolicitudRegistrada";

    /// <summary>UUID para idempotencia y publisher confirms.</summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    public int SolicitudId { get; set; }

    public string UsuarioId { get; set; } = string.Empty;

    public DateTime FechaEventoUtc { get; set; } = DateTime.UtcNow;
}
