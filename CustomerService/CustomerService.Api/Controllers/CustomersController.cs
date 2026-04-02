using CustomerService.Api.Data;
using CustomerService.Api.Dtos;
using CustomerService.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _context;

    public CustomersController(CustomerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var customers = _context.Customers
            .Select(c => ToResponse(c))
            .ToList();

        return Ok(customers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        return Ok(ToResponse(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim()
        };

        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();

        var response = ToResponse(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, response);
    }

    private static CustomerResponse ToResponse(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email
    };
}
