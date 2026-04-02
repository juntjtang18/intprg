namespace ApiGateway.Models;

public class OrderAggregateResponse
{
    public OrderSummaryDto? Order { get; set; }
    public CustomerSummaryDto? Customer { get; set; }
    public ProductSummaryDto? Product { get; set; }
    public PaymentSummaryDto? Payment { get; set; }
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CustomerSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class PaymentSummaryDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
