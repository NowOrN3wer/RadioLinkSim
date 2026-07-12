using FluentValidation;
using RadioLinkSim.Endpoints;
using RadioLinkSim.ErrorHandling;
using RadioLinkSim.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSingleton<GeodesyService>();
builder.Services.AddScoped<LinkProfileService>();

builder.Services.AddHttpClient<IElevationProvider, OpenElevationClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["OpenElevation:BaseAddress"]
        ?? "https://api.open-elevation.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:4294")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapLinkProfileEndpoints();

await app.RunAsync();
