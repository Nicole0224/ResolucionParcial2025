using GestionCreditos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionCreditos.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NombreCompleto)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Email)
            .IsUnique();

        builder.Property(c => c.Telefono)
            .HasMaxLength(20);

        builder.Property(c => c.Direccion)
            .HasMaxLength(250);

        builder.Property(c => c.FechaRegistro)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}