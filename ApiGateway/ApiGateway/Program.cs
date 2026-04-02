using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOcelot();

builder.Services.AddHttpClient("orders", client =>
{
    client.BaseAddress = new Uri("http://orderservice:8080/");
});

builder.Services.AddHttpClient("customers", client =>
{
    client.BaseAddress = new Uri("http://customerservice:8080/");
});

builder.Services.AddHttpClient("products", client =>
{
    client.BaseAddress = new Uri("http://productservice:8080/");
});

builder.Services.AddHttpClient("payments", client =>
{
    client.BaseAddress = new Uri("http://paymentservice:8080/");
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    message = "API Gateway is running.",
    routes = new[]
    {
        "/gateway/orders",
        "/gateway/products",
        "/gateway/customers",
        "/gateway/payments",
        "/aggregates/orders",
        "/aggregates/orders/{id}"
    }
}));

app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/aggregates"), branch =>
{
    branch.UseRouting();
    branch.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
});

await app.UseOcelot();

app.Run();
