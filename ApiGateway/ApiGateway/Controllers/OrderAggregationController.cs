using System.Text.Json;
using ApiGateway.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("aggregates/orders")]
public class OrderAggregationController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public OrderAggregationController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderAggregateResponse>>> GetAll(CancellationToken ct)
    {
        var orders = await GetOrdersAsync(ct);
        var results = new List<OrderAggregateResponse>();

        foreach (var order in orders)
        {
            results.Add(await BuildOrderAggregateAsync(order, ct));
        }

        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderAggregateResponse>> GetById(int id, CancellationToken ct)
    {
        var orders = await GetOrdersAsync(ct);
        var order = orders.FirstOrDefault(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(await BuildOrderAggregateAsync(order, ct));
    }

    private async Task<List<OrderSummaryDto>> GetOrdersAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("orders");
        using var response = await client.GetAsync("api/orders", ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<List<OrderSummaryDto>>(stream, JsonOptions, ct) ?? new List<OrderSummaryDto>();
    }

    private async Task<OrderAggregateResponse> BuildOrderAggregateAsync(OrderSummaryDto order, CancellationToken ct)
    {
        var customerTask = GetAsync<CustomerSummaryDto>("customers", $"api/customers/{order.CustomerId}", ct);
        var productTask = GetAsync<ProductSummaryDto>("products", $"api/products/{order.ProductId}", ct);
        var paymentTask = GetAsync<PaymentSummaryDto>("payments", $"api/payments/order/{order.Id}", ct);

        await Task.WhenAll(customerTask, productTask, paymentTask);

        return new OrderAggregateResponse
        {
            Order = order,
            Customer = customerTask.Result,
            Product = productTask.Result,
            Payment = paymentTask.Result
        };
    }

    private async Task<T?> GetAsync<T>(string clientName, string relativeUrl, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(clientName);
        using var response = await client.GetAsync(relativeUrl, ct);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }
}
