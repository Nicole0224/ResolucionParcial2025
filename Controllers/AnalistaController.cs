using GestionCreditos.Data;
using GestionCreditos.Hubs;
using GestionCreditos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Controllers;

[Authorize(Roles = "Analista")]
[Route("Analista")]
public class AnalistaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<SolicitudesHub> _hubContext;

    public AnalistaController(ApplicationDbContext context, IHubContext<SolicitudesHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var solicitudes = await _context.SolicitudesCredito
            .AsNoTracking()
            .Include(s => s.Cliente)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderBy(s => s.FechaSolicitud)
            .ToListAsync();

        return View(solicitudes);
    }

    [HttpPost]
    [Route("~/Analista/Aprobar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud is null)
        {
            TempData["MensajeError"] = "La solicitud no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["MensajeError"] = "La solicitud ya fue procesada y no puede volver a aprobarse.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.Cliente is null)
        {
            TempData["MensajeError"] = "La solicitud no tiene cliente asociado.";
            return RedirectToAction(nameof(Index));
        }

        var limite = solicitud.Cliente.IngresosMensuales * 5;
        if (solicitud.MontoSolicitado > limite)
        {
            TempData["MensajeError"] =
                $"No se puede aprobar: el monto solicitado ({solicitud.MontoSolicitado:C}) supera 5 veces los ingresos mensuales del cliente ({limite:C}).";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Aprobado;
        solicitud.MotivoRechazo = null;

        await _context.SaveChangesAsync();

        await _hubContext.Clients.User(solicitud.Cliente.UsuarioId)
            .SendAsync("SolicitudEstadoActualizado", new
            {
                SolicitudId = solicitud.Id,
                Estado = solicitud.Estado.ToString(),
                MotivoRechazo = solicitud.MotivoRechazo
            });

        TempData["MensajeExito"] = $"Solicitud #{solicitud.Id} aprobada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("~/Analista/Rechazar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string? motivo)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud is null)
        {
            TempData["MensajeError"] = "La solicitud no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["MensajeError"] = "La solicitud ya fue procesada y no puede volver a rechazarse.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["MensajeError"] = "Debes indicar un motivo para rechazar la solicitud.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = motivo.Trim();

        await _context.SaveChangesAsync();

        if (solicitud.Cliente is not null)
        {
            await _hubContext.Clients.User(solicitud.Cliente.UsuarioId)
                .SendAsync("SolicitudEstadoActualizado", new
                {
                    SolicitudId = solicitud.Id,
                    Estado = solicitud.Estado.ToString(),
                    MotivoRechazo = solicitud.MotivoRechazo
                });
        }

        TempData["MensajeExito"] = $"Solicitud #{solicitud.Id} rechazada.";
        return RedirectToAction(nameof(Index));
    }
}