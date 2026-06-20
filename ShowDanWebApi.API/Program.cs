using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Npgsql;
using ShowDanWebApi.API.AppData;
using ShowDanWebApi.API.Controllers;
using ShowDanWebApi.API.Hubs;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.Language;
using ShowDanWebApi.Data;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonDateTimeUtcConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
builder.Services.AddSingleton<IMapService, MapService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
_ = app.Services.GetRequiredService<IMapService>();

app.UseSwagger();
app.UseSwaggerUI();
app.MapHub<ChatHub>("/chat");

app.Run();
