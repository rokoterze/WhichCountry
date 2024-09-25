using Serilog;
using WC.DataAccess.Models;
using WC.Service;
using WC.Service.IService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();
//DB
builder.Services.AddSqlServer<WhichCountryContext>(builder.Configuration.GetConnectionString("DefaultConnection"));

//AM
builder.Services.AddAutoMapper(typeof(Program).Assembly);

//DI
builder.Services.AddScoped<IWcService, WcService>();

//Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).CreateLogger();

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
