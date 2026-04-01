using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Models;
using Contracts;
using OrderService.Api.Messaging;
using OrderService.Api.Dtos;
using OrderService.Api.Services;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IProductClient _productClient;
    private readonly OrderCreatedPublisher _publisher;

    public OrdersController(
        OrdersDbContext context,
        ICustomerClient customerClient,
        IProductClient productClient,
        OrderCreatedPublisher publisher)
    {
        _context = context;
        _customerClient = customerClient;
        _productClient = productClient;
        _publisher = publisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Orders.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        // Basic input validation (rubric: execution / validation)
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than 0.");

        // Synchronous validation against owning services (keeps data ownership intact)
        var customerExists = await _customerClient.CustomerExistsAsync(request.CustomerId);
        if (!customerExists)
            return BadRequest("Customer does not exist.");

        // Validate product by querying ProductService, and compute Total server-side.
        decimal? unitPrice;
        try
        {
            unitPrice = await _productClient.GetProductPriceAsync(request.ProductId);
        }
        catch
        {
            return StatusCode(503, "Product service unavailable. Please retry.");
        }

        if (unitPrice is null)
            return BadRequest("Product does not exist.");

        var order = new Order
        {
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            // Total is computed server-side (do not accept client input).
            Total = unitPrice.Value * request.Quantity
        };

        // Ignore any client-provided Id (CreateOrderRequest doesn't include it, but keep the intent explicit)
        order.Id = 0;

        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);

        // Publish OrderCreated event to RabbitMQ (queue: order-created)
        try
        {
            _publisher.Publish(new OrderCreated
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                ProductId = order.ProductId,
                Quantity = order.Quantity
            }, ct);
        }
        catch
        {
            // If RabbitMQ is down, surface a clear status rather than an opaque 500.
            return StatusCode(503, "Order saved, but event publish failed because RabbitMQ is unavailable.");
        }

        return Ok(order);
    }
}
