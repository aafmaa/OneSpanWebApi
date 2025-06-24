using OneSpanWebApi.Data;
using OneSpanWebApi.Services;
using Serilog;
using OneSpanWebApi.Models;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using GemBox;
using GemBox.Document;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog configuration
Log.Logger = new LoggerConfiguration()
       .ReadFrom.Configuration(builder.Configuration)
       .CreateLogger();

// Add Serilog to the host
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddSingleton<DBConnectionFactory>();
builder.Services.AddSingleton<OneSpanService>();
builder.Services.AddSingleton<IasService>();

// Add the necessary using directive for the namespace containing DbConnectionFactory  

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure OneSpan options from appsettings.json
builder.Services.Configure<OneSpanConfig>(builder.Configuration.GetSection("OneSpan"));

// Register the OneSpanService with dependency injection
builder.Services.AddTransient<OneSpanService>();

// Configure IasClient options from appsettings.json
builder.Services.Configure<IasClientConfig>(builder.Configuration.GetSection("IasClient"));

// Register the IasService with dependency injection
builder.Services.AddTransient<IasService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));


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
