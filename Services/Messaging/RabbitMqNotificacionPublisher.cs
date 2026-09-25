using System.Text;
using System.Text.Json;
using GestionCreditos.Models.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace GestionCreditos.Services.Messaging;

public class RabbitMqNotificacionPublisher : INotificacionPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqNotificacionPublisher> _logger;

    public RabbitMqNotificacionPublisher(IConfiguration configuration, ILogger<RabbitMqNotificacionPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> PublicarAsync(SolicitudRegistradaEvent evento, CancellationToken cancellationToken = default)
    {
        // Publisher confirms: garantiza que el broker recibió el mensaje.
        // Si falla, la solicitud permanece en BD y se registra el error.
        // Reenvío manual: reutilizar el mismo MessageId para mantener idempotencia.
        try
        {
            var connectionString = _configuration["RabbitMq:ConnectionString"] ?? "amqp://guest:guest@localhost:5672/";
            var queueName = _configuration["RabbitMq:QueueName"] ?? "solicitudes.notificaciones";

            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                AutomaticRecoveryEnabled = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Cola durable solicitada por el enunciado
            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.ConfirmSelect();

            var json = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.DeliveryMode = 2; // persistente
            props.Persistent = true;
            props.MessageId = evento.MessageId;
            props.ContentType = "application/json";
            props.Headers = new Dictionary<string, object>
            {
                ["Tipo"] = evento.Tipo
            };

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: queueName,
                basicProperties: props,
                body: body);

            // Publisher confirms: espera confirmación del broker (timeout 5s)
            var confirmed = channel.WaitForConfirms(TimeSpan.FromSeconds(5));
            if (!confirmed)
            {
                throw new Exception("Publisher confirm no recibido (timeout o nack).");
            }

            _logger.LogInformation("Mensaje SolicitudRegistrada publicado: MessageId={MessageId} SolicitudId={SolicitudId} UsuarioId={UsuarioId}",
                evento.MessageId, evento.SolicitudId, evento.UsuarioId);

            // Simular async para respetar la firma
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            // Conservar solicitud en BD, registrar error y documentar reenvío manual
            _logger.LogError(ex,
                "Fallo al publicar SolicitudRegistrada. SolicitudId={SolicitudId} MessageId={MessageId} UsuarioId={UsuarioId}. " +
                "La solicitud permanece en BD. Reenvío manual: reutilizar el mismo MessageId e invocar INotificacionPublisher.ReenviarAsync(messageId, solicitudId, usuarioId, fechaEventoUtc).",
                evento.SolicitudId, evento.MessageId, evento.UsuarioId);
            return false;
        }
    }

    public Task<bool> ReenviarAsync(string messageId, int solicitudId, string usuarioId, DateTime fechaEventoUtc, CancellationToken cancellationToken = default)
    {
        var evento = new SolicitudRegistradaEvent
        {
            MessageId = messageId,
            SolicitudId = solicitudId,
            UsuarioId = usuarioId,
            FechaEventoUtc = fechaEventoUtc,
            Tipo = "SolicitudRegistrada"
        };
        return PublicarAsync(evento, cancellationToken);
    }
}
