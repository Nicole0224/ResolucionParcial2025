using System.Text;
using System.Text.Json;
using GestionCreditos.Data;
using GestionCreditos.Models;
using GestionCreditos.Models.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GestionCreditos.Services.Messaging;

public class NotificacionConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificacionConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public NotificacionConsumerService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificacionConsumerService> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Preparado para producción: se puede deshabilitar vía variable de entorno RabbitMq__ConsumerEnabled=false
        // y sobrescribir conexión/cola vía RabbitMq__ConnectionString / RabbitMq__QueueName
        var consumerEnabled = _configuration.GetValue<bool?>("RabbitMq:ConsumerEnabled") ?? true;
        if (!consumerEnabled)
        {
            _logger.LogInformation("Consumidor RabbitMQ deshabilitado por RabbitMq:ConsumerEnabled=false");
            return;
        }

        // Reintentos de conexión si RabbitMQ no está disponible al iniciar
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connectionString = _configuration["RabbitMq:ConnectionString"] ?? "amqp://guest:guest@localhost:5672/";
                var queueName = _configuration["RabbitMq:QueueName"] ?? "solicitudes.notificaciones";

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(connectionString),
                    AutomaticRecoveryEnabled = true,
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Cola durable
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Fair dispatch
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (sender, ea) =>
                {
                    var deliveryTag = ea.DeliveryTag;
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        SolicitudRegistradaEvent? evento;
                        try
                        {
                            evento = JsonSerializer.Deserialize<SolicitudRegistradaEvent>(json);
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogWarning(jsonEx, "JSON inválido recibido, se descarta sin requeue. Body: {Body}", json);
                            _channel!.BasicNack(deliveryTag, false, false);
                            return;
                        }

                        if (evento is null || string.IsNullOrWhiteSpace(evento.MessageId))
                        {
                            _logger.LogWarning("Mensaje inválido (MessageId vacío) recibido, se descarta sin requeue. Body: {Body}", json);
                            _channel!.BasicNack(deliveryTag, false, false);
                            return;
                        }

                        // Idempotencia: no duplicar si mismo MessageId ya procesado -> ACK (no crea otra)
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var yaExiste = await db.Notificaciones.AsNoTracking()
                            .AnyAsync(n => n.MessageId == evento.MessageId);

                        if (yaExiste)
                        {
                            _logger.LogInformation("Mensaje duplicado ignorado: MessageId={MessageId}", evento.MessageId);
                            _channel!.BasicAck(deliveryTag, false);
                            return;
                        }

                        var notificacion = new Notificacion
                        {
                            MessageId = evento.MessageId,
                            SolicitudId = evento.SolicitudId,
                            UsuarioId = evento.UsuarioId,
                            Texto = $"Solicitud #{evento.SolicitudId} registrada - Tipo: {evento.Tipo}",
                            FechaProcesamientoUtc = DateTime.UtcNow
                        };

                        try
                        {
                            db.Notificaciones.Add(notificacion);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception dbEx)
                        {
                            _logger.LogError(dbEx, "Error transitorio guardando Notificación MessageId={MessageId}, se hará Nack con requeue.", evento.MessageId);
                            _channel!.BasicNack(deliveryTag, false, true);
                            return;
                        }

                        // ACK manual solo después de guardar correctamente
                        _channel!.BasicAck(deliveryTag, false);
                        _logger.LogInformation("Notificación guardada: MessageId={MessageId} SolicitudId={SolicitudId}", evento.MessageId, evento.SolicitudId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error inesperado procesando mensaje RabbitMQ, se hará Nack con requeue. DeliveryTag={DeliveryTag}", deliveryTag);
                        try
                        {
                            _channel!.BasicNack(deliveryTag, false, true);
                        }
                        catch (Exception nackEx)
                        {
                            _logger.LogError(nackEx, "Fallo al hacer Nack");
                        }
                    }
                };

                _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("Consumidor RabbitMQ iniciado en cola {QueueName}", queueName);

                // Mantener el servicio vivo hasta cancelación
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en NotificacionConsumerService, reintentando en 5s...");
                try { _channel?.Close(); } catch { }
                try { _connection?.Close(); } catch { }
                _channel?.Dispose();
                _connection?.Dispose();
                _channel = null;
                _connection = null;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
