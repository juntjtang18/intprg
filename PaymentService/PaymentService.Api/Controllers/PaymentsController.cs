using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
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
        return Ok(await _context.Payments.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Payment payment)
    {
        payment.Id = 0;
        payment.CreatedAt = payment.CreatedAt == default ? DateTime.UtcNow : payment.CreatedAt;
        if (string.IsNullOrWhiteSpace(payment.Status))
            payment.Status = "Completed";

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }
}
