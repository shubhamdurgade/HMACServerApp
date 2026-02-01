using HMACServerApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddMemoryCache();
builder.Services.AddDbContext<HMACDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EFCoreDBConnection"));
});

builder.Services.AddScoped<ClientSecretService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// only run HMAC middleware for non-swagger requests
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase),
    appBuilder => appBuilder.UseMiddleware<HMACAuthenticationMiddleware>()
);

app.UseAuthorization();

app.MapControllers();

app.Run();
