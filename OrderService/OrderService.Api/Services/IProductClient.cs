namespace OrderService.Api.Services;

public interface IProductClient
{
    /// <summary>
    /// Returns the product price if the product exists; otherwise returns null.
    /// </summary>
    Task<decimal?> GetProductPriceAsync(int productId);
}
