using System.ComponentModel.DataAnnotations;

namespace GestionCreditos.Models.ViewModels;

public class CrearSolicitudViewModel
{
    [Required(ErrorMessage = "El monto solicitado es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor que cero.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Monto solicitado")]
    public decimal MontoSolicitado { get; set; }
}