using Woody.Infrastructure.Persistence.Configuration;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WoodyDbConfiguration.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
    DbSeeder.Seed(dbContext);
}

if (!app.Environment.EnvironmentName.Equals("Prod", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();