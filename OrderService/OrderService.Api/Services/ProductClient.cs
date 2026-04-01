using System.Net;
using System.Net.Http.Json;

namespace OrderService.Api.Services;

public class ProductClient : IProductClient
{
    private readonly HttpClient _httpClient;

    public ProductClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Keep this DTO local to avoid coupling OrderService to ProductService's domain model.
    private sealed class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public async Task<decimal?> GetProductPriceAsync(int productId)
    {
        // Simple retry to handle container startup timing + transient network hiccups.
        const int maxAttempts = 6;
        var delay = TimeSpan.FromMilliseconds(200);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync($"/api/products/{productId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"ProductService returned {(int)response.StatusCode} {response.ReasonPhrase}");

                var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
                return dto?.Price;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 2000));
            }
        }

        // If still failing, allow it to bubble so the API returns 503.
        using var finalResponse = await _httpClient.GetAsync($"/api/products/{productId}");
        if (finalResponse.StatusCode == HttpStatusCode.NotFound)
            return null;
        finalResponse.EnsureSuccessStatusCode();
        var finalDto = await finalResponse.Content.ReadFromJsonAsync<ProductDto>();
        return finalDto?.Price;
    }
}
