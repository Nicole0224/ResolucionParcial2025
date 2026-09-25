using GestionCreditos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionCreditos.Data.Configurations;

public class SolicitudCreditoConfiguration : IEntityTypeConfiguration<SolicitudCredito>
{
    public void Configure(EntityTypeBuilder<SolicitudCredito> builder)
    {
        builder.ToTable("SolicitudesCredito");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.MontoSolicitado)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(s => s.FechaSolicitud)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EstadoSolicitud.Pendiente);

        builder.Property(s => s.MotivoRechazo)
            .HasMaxLength(1000);

        builder.HasOne(s => s.Cliente)
            .WithMany(c => c.Solicitudes)
            .HasForeignKey(s => s.ClienteId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.ClienteId);
        builder.HasIndex(s => s.Estado);
        builder.HasIndex(s => s.FechaSolicitud);

        builder.HasIndex(s => s.ClienteId, "IX_SolicitudesCredito_ClienteId_PendienteUnico")
            .IsUnique()
            .HasFilter("Estado = 'Pendiente'");

        builder.HasCheckConstraint("CK_SolicitudCredito_MontoPositivo", "[MontoSolicitado] > 0");
    }
}