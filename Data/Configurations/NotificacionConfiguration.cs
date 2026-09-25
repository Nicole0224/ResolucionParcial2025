using GestionCreditos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionCreditos.Data.Configurations;

public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.MessageId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(n => n.MessageId)
            .IsUnique();

        builder.Property(n => n.UsuarioId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(n => n.Texto)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(n => n.Solicitud)
            .WithMany()
            .HasForeignKey(n => n.SolicitudId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
