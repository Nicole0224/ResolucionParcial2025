using GestionCreditos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionCreditos.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes", table => table
            .HasCheckConstraint("CK_Clientes_IngresosPositivo", "[IngresosMensuales] > 0"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UsuarioId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(c => c.UsuarioId)
            .IsUnique();

        builder.Property(c => c.IngresosMensuales)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true);
    }
}