using GestionCreditos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Data;

public static class DbInitializer
{
    public const string RolAnalista = "Analista";

    public const string EmailAnalista = "analista@creditos.local";
    public const string EmailCliente1 = "cliente1@creditos.local";
    public const string EmailCliente2 = "cliente2@creditos.local";
    public const string PasswordSeed = "Analista.123!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        await EnsureRoleAsync(roleManager, RolAnalista);

        var analista = await EnsureUserAsync(userManager, EmailAnalista);
        if (analista is not null && !await userManager.IsInRoleAsync(analista, RolAnalista))
        {
            await userManager.AddToRoleAsync(analista, RolAnalista);
        }

        var cliente1User = await EnsureUserAsync(userManager, EmailCliente1);
        var cliente2User = await EnsureUserAsync(userManager, EmailCliente2);

        if (cliente1User is null || cliente2User is null || analista is null)
        {
            return;
        }

        if (!await context.Clientes.AnyAsync())
        {
            context.Clientes.AddRange(
                new Cliente { UsuarioId = cliente1User.Id, IngresosMensuales = 50000m, Activo = true },
                new Cliente { UsuarioId = cliente2User.Id, IngresosMensuales = 80000m, Activo = true });

            await context.SaveChangesAsync();
        }

        if (!await context.SolicitudesCredito.AnyAsync())
        {
            var clientes = await context.Clientes.OrderBy(c => c.Id).AsNoTracking().ToListAsync();
            if (clientes.Count >= 2)
            {
                context.SolicitudesCredito.AddRange(
                    new SolicitudCredito
                    {
                        ClienteId = clientes[0].Id,
                        MontoSolicitado = 15000m,
                        FechaSolicitud = DateTime.UtcNow,
                        Estado = EstadoSolicitud.Pendiente
                    },
                    new SolicitudCredito
                    {
                        ClienteId = clientes[1].Id,
                        MontoSolicitado = 25000m,
                        FechaSolicitud = DateTime.UtcNow,
                        Estado = EstadoSolicitud.Aprobado
                    });

                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task<IdentityUser?> EnsureUserAsync(UserManager<IdentityUser> userManager, string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, PasswordSeed);
        return result.Succeeded ? user : null;
    }
}