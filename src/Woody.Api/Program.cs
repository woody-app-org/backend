using Woody.Infrastructure.Persistence.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WoodyDbConfiguration.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();


if (!app.Environment.EnvironmentName.Equals("Prod", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();