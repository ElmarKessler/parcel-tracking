using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ParcelTracking.Api.Data;
using ParcelTracking.Api.Endpoints;
using ParcelTracking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=parceltracking.db"));
builder.Services.AddScoped<ParcelEventService>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () => "Parcel Tracking API");
app.MapOrderEndpoints();
app.MapParcelEndpoints();
app.MapParcelEventEndpoints();
app.MapWarehouseEndpoints();

app.Run();
