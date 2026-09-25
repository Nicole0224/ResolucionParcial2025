using System.Text.Json;
using GestionCreditos.Data;
using GestionCreditos.Models;
using GestionCreditos.Models.Messaging;
using GestionCreditos.Models.ViewModels;
using GestionCreditos.Services.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GestionCreditos.Controllers;

[Authorize]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IDistributedCache _cache;
    private readonly INotificacionPublisher _publisher;
    private readonly ILogger<SolicitudesController> _logger;

    public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache, INotificacionPublisher publisher, ILogger<SolicitudesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
        _publisher = publisher;
        _logger = logger;
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

        var solicitudes = await GetListadoCacheadAsync(cliente.Id);

        ValidarRangos(filtro);

        if (ModelState.IsValid)
        {
            if (filtro.Estado.HasValue)
            {
                solicitudes = solicitudes.Where(s => s.Estado == filtro.Estado.Value).ToList();
            }

            if (filtro.MontoDesde.HasValue)
            {
                solicitudes = solicitudes.Where(s => s.MontoSolicitado >= filtro.MontoDesde.Value).ToList();
            }

            if (filtro.MontoHasta.HasValue)
            {
                solicitudes = solicitudes.Where(s => s.MontoSolicitado <= filtro.MontoHasta.Value).ToList();
            }

            if (filtro.FechaDesde.HasValue)
            {
                solicitudes = solicitudes.Where(s => s.FechaSolicitud >= filtro.FechaDesde.Value).ToList();
            }

            if (filtro.FechaHasta.HasValue)
            {
                solicitudes = solicitudes.Where(s => s.FechaSolicitud <= filtro.FechaHasta.Value).ToList();
            }
        }

        filtro.Solicitudes = solicitudes;
        filtro.Estados = BuildEstados();

        return View(filtro);
    }

    /// <summary>
    /// Devuelve el listado completo de solicitudes del cliente, cacheado 60 segundos en Redis.
    /// </summary>
    private async Task<List<SolicitudCredito>> GetListadoCacheadAsync(int clienteId)
    {
        var clave = $"{ApplicationDbContext.PrefijoClaveListadoSolicitudes}{clienteId}";

        try
        {
            var bytes = await _cache.GetAsync(clave);
            if (bytes is not null)
            {
                return JsonSerializer.Deserialize<List<SolicitudCredito>>(bytes) ?? [];
            }
        }
        catch (Exception)
        {
            // Si el caché falla, se consulta la base de datos.
        }

        var solicitudes = await _context.SolicitudesCredito
            .AsNoTracking()
            .Where(s => s.ClienteId == clienteId)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();

        try
        {
            await _cache.SetAsync(clave, JsonSerializer.SerializeToUtf8Bytes(solicitudes),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });
        }
        catch (Exception)
        {
            // Sin caché, la consulta a la base de datos sigue funcionando.
        }

        return solicitudes;
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

        HttpContext.Session.SetInt32("UltimaSolicitudId", solicitud.Id);
        HttpContext.Session.SetString("UltimaSolicitudMonto", solicitud.MontoSolicitado.ToString("C"));

        return View(solicitud);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        var cliente = await GetClienteActivoAsync();
        if (cliente is null)
        {
            TempData["MensajeError"] = "Necesitas tener un cliente activo para solicitar crédito.";
            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearSolicitudViewModel modelo)
    {
        var cliente = await GetClienteActivoAsync();
        if (cliente is null)
        {
            ModelState.AddModelError(string.Empty, "Necesitas tener un cliente activo para solicitar crédito.");
            return View(modelo);
        }

        if (await _context.SolicitudesCredito.AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente))
        {
            ModelState.AddModelError(string.Empty, "Ya tienes una solicitud de crédito pendiente.");
        }

        if (modelo.MontoSolicitado > cliente.IngresosMensuales * 10)
        {
            ModelState.AddModelError(nameof(modelo.MontoSolicitado),
                $"El monto solicitado no puede superar 10 veces tus ingresos mensuales ({cliente.IngresosMensuales * 10:C}).");
        }

        if (ModelState.IsValid)
        {
            var solicitud = new SolicitudCredito
            {
                ClienteId = cliente.Id,
                MontoSolicitado = modelo.MontoSolicitado,
                FechaSolicitud = DateTime.UtcNow,
                Estado = EstadoSolicitud.Pendiente
            };

            _context.SolicitudesCredito.Add(solicitud);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Ya tienes una solicitud de crédito pendiente.");
                return View(modelo);
            }

            // Publicar evento SolicitudRegistrada en RabbitMQ (cola durable solicitudes.notificaciones) con persistencia y publisher confirms.
            // Si falla la publicación, se conserva la solicitud en BD y se registra el error.
            // Reenvío manual documentado: reutilizar el mismo MessageId para mantener idempotencia en el consumidor.
            // Ejemplo: await _publisher.ReenviarAsync(messageId, solicitud.Id, usuarioId, fechaEventoUtc);
            var usuarioId = _userManager.GetUserId(User) ?? string.Empty;
            var messageId = Guid.NewGuid().ToString();
            var evento = new SolicitudRegistradaEvent
            {
                MessageId = messageId,
                SolicitudId = solicitud.Id,
                UsuarioId = usuarioId,
                FechaEventoUtc = DateTime.UtcNow,
                Tipo = "SolicitudRegistrada"
            };

            var publicado = await _publisher.PublicarAsync(evento);
            if (!publicado)
            {
                _logger.LogError(
                    "No se pudo publicar SolicitudRegistrada MessageId={MessageId} SolicitudId={SolicitudId} UsuarioId={UsuarioId} FechaEventoUtc={FechaEventoUtc}. " +
                    "Solicitud conservada en BD. Reenvio manual: reutilizar el mismo MessageId con INotificacionPublisher.ReenviarAsync(messageId, solicitudId, usuarioId, fechaEventoUtc).",
                    messageId, solicitud.Id, usuarioId, evento.FechaEventoUtc);
            }

            TempData["MensajeExito"] = "Solicitud de crédito registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        return View(modelo);
    }

    private async Task<Cliente?> GetClienteActivoAsync()
    {
        var cliente = await GetClienteAsync();
        return cliente is { Activo: true } ? cliente : null;
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