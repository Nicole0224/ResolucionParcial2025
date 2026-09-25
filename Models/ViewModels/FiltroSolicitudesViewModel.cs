using System.ComponentModel.DataAnnotations;
using GestionCreditos.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionCreditos.Models.ViewModels;

public class FiltroSolicitudesViewModel
{
    [Display(Name = "Estado")]
    public EstadoSolicitud? Estado { get; set; }

    [Display(Name = "Monto mínimo")]
    [Range(0, double.MaxValue, ErrorMessage = "El monto mínimo no puede ser negativo.")]
    public decimal? MontoDesde { get; set; }

    [Display(Name = "Monto máximo")]
    [Range(0, double.MaxValue, ErrorMessage = "El monto máximo no puede ser negativo.")]
    public decimal? MontoHasta { get; set; }

    [Display(Name = "Fecha desde")]
    [DataType(DataType.Date)]
    public DateTime? FechaDesde { get; set; }

    [Display(Name = "Fecha hasta")]
    [DataType(DataType.Date)]
    public DateTime? FechaHasta { get; set; }

    public List<SolicitudCredito> Solicitudes { get; set; } = [];

    public SelectList? Estados { get; set; }
}