using WC.PublicAPI;
using WC.Service.IService;
using WC.Service;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WC.DataAccess.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// API URL:
var baseURL = configuration["WcApiSettings:BaseUrl"];

// Register HttpClient
builder.Services.AddHttpClient<WcApiClient>((client) =>
{
    client.BaseAddress = new Uri(baseURL!);
});

// Register WcApiClient
builder.Services.AddTransient<WcApiClient>((serviceProvider) =>
{
    var httpClient = serviceProvider.GetRequiredService<HttpClient>();
    return new WcApiClient(baseURL,httpClient);
});

//Dependency
builder.Services.AddScoped<IWcService, WcService>();
builder.Services.AddSqlServer<WhichCountryContext>(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("TokenSettings:Secret").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
