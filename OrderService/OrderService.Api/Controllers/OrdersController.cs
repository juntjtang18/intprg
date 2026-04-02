using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Dtos;
using OrderService.Api.Messaging;
using OrderService.Api.Models;
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
        var orders = await _context.Orders
            .Select(o => ToResponse(o))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();
        return Ok(ToResponse(order));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than 0.");

        var customerExists = await _customerClient.CustomerExistsAsync(request.CustomerId);
        if (!customerExists)
            return BadRequest("Customer does not exist.");

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
            Total = unitPrice.Value * request.Quantity,
            Id = 0
        };

        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);

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
            return StatusCode(503, "Order saved, but event publish failed because RabbitMQ is unavailable.");
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
    }

    private static OrderResponse ToResponse(Order order) => new()
    {
        Id = order.Id,
        Total = order.Total,
        CustomerId = order.CustomerId,
        ProductId = order.ProductId,
        Quantity = order.Quantity
    };
}
