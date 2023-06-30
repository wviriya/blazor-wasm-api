using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using TodoApi.Models;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<TodoContext>(options =>
{
    options.UseCosmos(
        builder.Configuration.GetConnectionString("CosmosEndpointUrl"),
        builder.Configuration.GetConnectionString("CosmosKey"),
        databaseName: builder.Configuration.GetConnectionString("CosmosDatabaseName"));
});
// Register RedisConnection as a singleton
builder.Services.AddSingleton(async sp => await RedisConnection.InitializeAsync(builder.Configuration.GetConnectionString("Redis")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "To do API",
        Version = "v1"
    });
});

var app = builder.Build();

app.UseSwagger();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseCors(policy => 
    policy.WithOrigins("*")
    .AllowAnyMethod()
    .WithHeaders(HeaderNames.ContentType));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
