namespace ApiGateway.Models;

public class OrderAggregateResponse
{
    public OrderDto? Order { get; set; }
    public CustomerDto? Customer { get; set; }
    public ProductDto? Product { get; set; }
    public PaymentDto? Payment { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
