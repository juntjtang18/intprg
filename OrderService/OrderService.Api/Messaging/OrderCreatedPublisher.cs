using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Contracts;

namespace OrderService.Api.Messaging;

public sealed class OrderCreatedPublisher : IDisposable
{
    private const string ExchangeName = "order-created-exchange";

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

                    _channel.ExchangeDeclare(
                        exchange: ExchangeName,
                        type: ExchangeType.Fanout,
                        durable: true,
                        autoDelete: false);

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

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: "",
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        _channel?.Dispose();
        _connection?.Dispose();
    }
}