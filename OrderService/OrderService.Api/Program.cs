using OrderService.Api.Data;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Messaging;
using OrderService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite("Data Source=orders.db"));

builder.Services.AddHttpClient<ICustomerClient, CustomerClient>(client =>
{
    client.BaseAddress = new Uri("http://customerservice:8080/");
});

// Synchronous validation (OrderService -> ProductService)
builder.Services.AddHttpClient<IProductClient, ProductClient>(client =>
{
    client.BaseAddress = new Uri("http://productservice:8080/");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// RabbitMQ publisher (RabbitMQ.Client)
builder.Services.AddSingleton<OrderCreatedPublisher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
