namespace OrderService.Api.Models;

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public int CustomerId { get; set; }

    // ✅ add these
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}