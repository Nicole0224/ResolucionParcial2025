using GestionCreditos.Data.Configurations;
using GestionCreditos.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<SolicitudCredito> SolicitudesCredito => Set<SolicitudCredito>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ClienteConfiguration());
        builder.ApplyConfiguration(new SolicitudCreditoConfiguration());
    }
}