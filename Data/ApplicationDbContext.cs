using GestionCreditos.Data.Configurations;
using GestionCreditos.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GestionCreditos.Data;

public class ApplicationDbContext : IdentityDbContext
{
    private readonly IDistributedCache? _cache;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDistributedCache? cache = null)
        : base(options)
    {
        _cache = cache;
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<SolicitudCredito> SolicitudesCredito => Set<SolicitudCredito>();

    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    /// <summary>Prefijo de la clave de caché del listado de solicitudes de un cliente.</summary>
    public const string PrefijoClaveListadoSolicitudes = "solicitudes:cliente:";

    public override int SaveChanges()
    {
        var clienteIdsAfectados = ObtenerClienteIdsAfectados();
        var resultado = base.SaveChanges();
        InvalidarCacheSolicitudes(clienteIdsAfectados);
        return resultado;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var clienteIdsAfectados = ObtenerClienteIdsAfectados();
        var resultado = await base.SaveChangesAsync(cancellationToken);
        await InvalidarCacheSolicitudesAsync(clienteIdsAfectados, cancellationToken);
        return resultado;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ClienteConfiguration());
        builder.ApplyConfiguration(new SolicitudCreditoConfiguration());
        builder.ApplyConfiguration(new NotificacionConfiguration());
    }

    /// <summary>
    /// Detecta clientes cuyas solicitudes fueron creadas o modificadas (p. ej. cambio de estado)
    /// para invalidar su listado cacheado en Redis.
    /// </summary>
    private List<int> ObtenerClienteIdsAfectados()
        => ChangeTracker.Entries<SolicitudCredito>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity.ClienteId)
            .Distinct()
            .ToList();

    private void InvalidarCacheSolicitudes(List<int> clienteIdsAfectados)
    {
        if (_cache is null)
        {
            return;
        }

        foreach (var clienteId in clienteIdsAfectados)
        {
            _cache.Remove($"{PrefijoClaveListadoSolicitudes}{clienteId}");
        }
    }

    private async Task InvalidarCacheSolicitudesAsync(List<int> clienteIdsAfectados, CancellationToken cancellationToken)
    {
        if (_cache is null)
        {
            return;
        }

        foreach (var clienteId in clienteIdsAfectados)
        {
            await _cache.RemoveAsync($"{PrefijoClaveListadoSolicitudes}{clienteId}", cancellationToken);
        }
    }
}