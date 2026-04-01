using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Contracts;

namespace OrderService.Api.Messaging;

/// <summary>
/// Publishes OrderCreated events to RabbitMQ.
///
/// - Lazy-connect so API can start even if RabbitMQ is still booting.
/// - Retries during publish to handle transient "connection refused".
/// </summary>
public sealed class OrderCreatedPublisher : IDisposable
{
    private const string QueueName = "order-created";

    private readonly object _lock = new();
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderCreatedPublisher()
    {
        _factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest",
            DispatchConsumersAsync = true
        };
    }

    private void EnsureConnectedWithRetry(CancellationToken ct)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        lock (_lock)
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                return;

            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;

            Exception? last = null;
            for (var attempt = 1; attempt <= 10 && !ct.IsCancellationRequested; attempt++)
            {
                try
                {
                    _connection = _factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    var delayMs = Math.Min(4000, 250 * (int)Math.Pow(2, attempt - 1));
                    Thread.Sleep(delayMs);
                }
            }

            throw new InvalidOperationException("RabbitMQ is not reachable after retries.", last);
        }
    }

    public void Publish(OrderCreated message, CancellationToken ct = default)
    {
        EnsureConnectedWithRetry(ct);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: properties, body: body);
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
