using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductService.Api.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProductService.Api.Messaging;

/// <summary>
/// Consumes OrderCreated events and updates Product stock.
///
/// Resiliency note:
/// RabbitMQ can take a few seconds to be ready after container start.
/// This consumer uses a retry loop so ProductService does NOT crash on startup races.
/// </summary>
public class OrderCreatedConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private IModel? _channel;

    public OrderCreatedConsumerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        _factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest",
            DispatchConsumersAsync = true
        };
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
        return Task.CompletedTask;
    }

    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
        const string queueName = "order-created";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection?.Dispose();
                _channel?.Dispose();

                _connection = _factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                _channel.BasicQos(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.Received += async (_, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var msg = JsonSerializer.Deserialize<OrderCreated>(json);

                        if (msg != null)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == msg.ProductId, stoppingToken);
                            if (product != null)
                            {
                                product.Stock -= msg.Quantity;
                                if (product.Stock < 0) product.Stock = 0;
                                await db.SaveChangesAsync(stoppingToken);
                            }
                        }

                        _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch
                    {
                        // don’t lose message if something failed
                        _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                // Keep running while the connection is healthy.
                while (!stoppingToken.IsCancellationRequested && _connection.IsOpen)
                    await Task.Delay(1000, stoppingToken);
            }
            catch
            {
                // RabbitMQ not reachable (yet) or connection dropped; wait then retry.
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
