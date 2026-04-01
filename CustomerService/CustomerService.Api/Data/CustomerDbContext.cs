using Microsoft.EntityFrameworkCore;
using CustomerService.Api.Models;

namespace CustomerService.Api.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
}
