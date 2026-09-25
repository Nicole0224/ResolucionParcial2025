namespace GestionCreditos.Models;

public class SolicitudCredito
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public Cliente Cliente { get; set; } = null!;

    public decimal MontoSolicitado { get; set; }

    public int PlazoMeses { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaEvaluacion { get; set; }
}