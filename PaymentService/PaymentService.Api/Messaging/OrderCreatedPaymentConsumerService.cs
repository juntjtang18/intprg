using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
using PaymentService.Api.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Api.Messaging;

public class OrderCreatedPaymentConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private IModel? _channel;

    public OrderCreatedPaymentConsumerService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;

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
        const string exchangeName = "order-created-exchange";
        const string queueName = "order-created-payment";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection?.Dispose();
                _channel?.Dispose();

                _connection = _factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(
                    exchange: exchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false);

                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _channel.QueueBind(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: "");

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
                            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                            var exists = await db.Payments.AnyAsync(
                                p => p.OrderId == msg.OrderId,
                                stoppingToken);

                            if (!exists)
                            {
                                var unitPrice = await GetProductPriceAsync(msg.ProductId, stoppingToken);

                                var payment = new Payment
                                {
                                    OrderId = msg.OrderId,
                                    CustomerId = msg.CustomerId,
                                    Amount = unitPrice * msg.Quantity,
                                    Status = "Completed",
                                    CreatedAt = DateTime.UtcNow
                                };

                                await db.Payments.AddAsync(payment, stoppingToken);
                                await db.SaveChangesAsync(stoppingToken);
                            }
                        }

                        _channel!.BasicAck(ea.DeliveryTag, false);
                    }
                    catch
                    {
                        _channel!.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                while (!stoppingToken.IsCancellationRequested && _connection.IsOpen)
                    await Task.Delay(1000, stoppingToken);
            }
            catch
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task<decimal> GetProductPriceAsync(int productId, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("ProductService");
        using var response = await client.GetAsync($"/api/products/{productId}", ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var product = await JsonSerializer.DeserializeAsync<ProductDto>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, ct);

        if (product == null)
            throw new InvalidOperationException($"Product {productId} not found.");

        return product.Price;
    }

    private sealed class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
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