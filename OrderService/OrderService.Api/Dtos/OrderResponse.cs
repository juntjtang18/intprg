namespace OrderService.Api.Dtos;

public class OrderResponse
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
