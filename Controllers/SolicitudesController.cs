using GestionCreditos.Data;
using GestionCreditos.Models;
using GestionCreditos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Controllers;

[Authorize]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(FiltroSolicitudesViewModel filtro)
    {
        var cliente = await GetClienteAsync();
        if (cliente is null)
        {
            filtro.Solicitudes = [];
            filtro.Estados = BuildEstados();
            return View(filtro);
        }

        var query = _context.SolicitudesCredito
            .AsNoTracking()
            .Where(s => s.ClienteId == cliente.Id);

        ValidarRangos(filtro);

        if (ModelState.IsValid)
        {
            if (filtro.Estado.HasValue)
            {
                query = query.Where(s => s.Estado == filtro.Estado.Value);
            }

            if (filtro.MontoDesde.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado >= filtro.MontoDesde.Value);
            }

            if (filtro.MontoHasta.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado <= filtro.MontoHasta.Value);
            }

            if (filtro.FechaDesde.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud >= filtro.FechaDesde.Value);
            }

            if (filtro.FechaHasta.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud <= filtro.FechaHasta.Value);
            }
        }

        filtro.Solicitudes = await query
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();

        filtro.Estados = BuildEstados();

        return View(filtro);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var cliente = await GetClienteAsync();
        if (cliente is null)
        {
            return NotFound();
        }

        var solicitud = await _context.SolicitudesCredito
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.ClienteId == cliente.Id);

        if (solicitud is null)
        {
            return NotFound();
        }

        return View(solicitud);
    }

    private async Task<Cliente?> GetClienteAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UsuarioId == userId);
    }

    private void ValidarRangos(FiltroSolicitudesViewModel filtro)
    {
        if (filtro.MontoDesde.HasValue && filtro.MontoHasta.HasValue && filtro.MontoDesde > filtro.MontoHasta)
        {
            ModelState.AddModelError(string.Empty, "El monto máximo no puede ser menor que el monto mínimo.");
        }

        if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue && filtro.FechaDesde > filtro.FechaHasta)
        {
            ModelState.AddModelError(string.Empty, "La fecha final no puede ser anterior a la fecha inicial.");
        }
    }

    private static SelectList BuildEstados()
    {
        var estados = Enum.GetValues<EstadoSolicitud>()
            .Select(e => new SelectListItem { Value = ((int)e).ToString(), Text = e.ToString() });
        return new SelectList(estados, "Value", "Text");
    }
}