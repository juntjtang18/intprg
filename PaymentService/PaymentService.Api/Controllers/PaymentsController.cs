using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
using PaymentService.Api.Dtos;
using PaymentService.Api.Models;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentDbContext _context;

    public PaymentsController(PaymentDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _context.Payments
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => ToResponse(p))
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();
        return Ok(ToResponse(payment));
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (payment == null) return NotFound();
        return Ok(ToResponse(payment));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentRequest request)
    {
        var payment = new Payment
        {
            Id = 0,
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Completed" : request.Status,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, ToResponse(payment));
    }

    private static PaymentResponse ToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        CustomerId = payment.CustomerId,
        Amount = payment.Amount,
        Status = payment.Status,
        CreatedAt = payment.CreatedAt
    };
}
