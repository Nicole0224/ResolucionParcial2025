using GestionCreditos.Models.Messaging;

namespace GestionCreditos.Services.Messaging;

public interface INotificacionPublisher
{
    /// <summary>
    /// Publica un evento SolicitudRegistrada de forma persistente con publisher confirms.
    /// Si RabbitMQ no está disponible, se mantiene la solicitud en BD y se registra el error.
    /// Reenvío manual: reutilizar el mismo MessageId y llamar nuevamente a este método.
    /// </summary>
    Task<bool> PublicarAsync(SolicitudRegistradaEvent evento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reenvío manual documentado: permite reintentar la publicación usando el mismo MessageId
    /// que se generó al crear la solicitud. Conservar el MessageId original garantiza idempotencia
    /// en el consumidor (no duplica Notificacion si llega duplicado).
    /// Ejemplo: await publisher.PublicarAsync(new SolicitudRegistradaEvent { MessageId = messageIdOriginal, SolicitudId = ..., UsuarioId = ..., FechaEventoUtc = ... });
    /// </summary>
    Task<bool> ReenviarAsync(string messageId, int solicitudId, string usuarioId, DateTime fechaEventoUtc, CancellationToken cancellationToken = default);
}
