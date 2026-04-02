using System.Net.Http.Json;
using System.Text.Json;
using Frontend.Ui.Models;

namespace Frontend.Ui.Services;

public class GatewayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public GatewayApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductDto>> GetProductsAsync()
        => await GetAsync<List<ProductDto>>("gateway/products") ?? new List<ProductDto>();

    public async Task CreateProductAsync(CreateProductDto request)
        => await PostAsync("gateway/products", request);

    public async Task<List<CustomerDto>> GetCustomersAsync()
        => await GetAsync<List<CustomerDto>>("gateway/customers") ?? new List<CustomerDto>();

    public async Task CreateCustomerAsync(CreateCustomerDto request)
        => await PostAsync("gateway/customers", request);

    public async Task<List<OrderAggregateDto>> GetOrdersAsync()
        => await GetAsync<List<OrderAggregateDto>>("aggregates/orders") ?? new List<OrderAggregateDto>();

    public async Task CreateOrderAsync(CreateOrderDto request)
        => await PostAsync("gateway/orders", request);

    public async Task<List<PaymentDto>> GetPaymentsAsync()
        => await GetAsync<List<PaymentDto>>("gateway/payments") ?? new List<PaymentDto>();

    public async Task CreatePaymentAsync(CreatePaymentDto request)
        => await PostAsync("gateway/payments", request);

    private async Task<T?> GetAsync<T>(string uri)
    {
        using var response = await _httpClient.GetAsync(uri);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task PostAsync<T>(string uri, T payload)
    {
        using var response = await _httpClient.PostAsJsonAsync(uri, payload);
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"Request failed with status code {(int)response.StatusCode}.";
        }

        throw new InvalidOperationException(message.Trim('"'));
    }
}
