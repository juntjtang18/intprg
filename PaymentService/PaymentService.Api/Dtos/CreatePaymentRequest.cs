namespace PaymentService.Api.Dtos;

public class CreatePaymentRequest
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Completed";
}
