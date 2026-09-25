namespace GestionCreditos.Models;

public class Cliente
{
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public ICollection<SolicitudCredito> Solicitudes { get; set; } = [];
}