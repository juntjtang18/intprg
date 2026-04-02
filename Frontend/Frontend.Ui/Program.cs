using Frontend.Ui.Components;
using Frontend.Ui.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"] ?? "http://apigateway:8080/";

builder.Services.AddHttpClient<GatewayApiClient>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
