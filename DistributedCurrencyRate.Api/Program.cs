using DistributedCurrencyRate.Api;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddApplication()
    .AddInfrastructure()
    .AddRateLimiting();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseExceptionHandler(options => { });

app.MapApiEndpoints();

await app.RunAsync();