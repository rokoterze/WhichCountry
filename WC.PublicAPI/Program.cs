using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using WC.PublicAPI;

var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
builder.Services.Configure<WcApiSettings>(builder.Configuration.GetSection("WcApiSettings"));


// Register HttpClient
builder.Services.AddHttpClient<WcApiClient>((serviceProvider, client) =>
{
    //var settings = serviceProvider.GetRequiredService<IOptions<WcApiSettings>>().Value;
    client.BaseAddress = new Uri("https://localhost:7000/");
});

// Register WcApiClient
builder.Services.AddTransient<WcApiClient>((serviceProvider) =>
{
    var httpClient = serviceProvider.GetRequiredService<HttpClient>();

    var settings = serviceProvider.GetRequiredService<IOptions<WcApiSettings>>();


    return new WcApiClient(settings.Value.BaseURL,httpClient);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
