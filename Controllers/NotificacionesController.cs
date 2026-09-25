using GestionCreditos.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Controllers;

[Authorize]
public class NotificacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificacionesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarioId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(usuarioId))
        {
            return Challenge();
        }

        var notificaciones = await _context.Notificaciones
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaProcesamientoUtc)
            .ToListAsync();

        return View(notificaciones);
    }
}
