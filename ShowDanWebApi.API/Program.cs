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
builder.Services.AddHttpClient<ITranslationService, OnlineTranslationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrderPricingService, OrderPricingService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPublicOrderService, PublicOrderService>();
builder.Services.AddHttpClient<IAiAssistantService, AiAssistantService>();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
dataSourceBuilder.UseNetTopologySuite();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.UseNetTopologySuite();
    }));

var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ShowDan API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Введите: Bearer {ваш_токен}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    options.OperationFilter<AddRequiredHeaderParameter>();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Применение миграций к базе данных...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Миграции успешно применены.");
        logger.LogInformation("Запуск первоначального заполнения базы данных (Seeding)...");
        await DbInitializer.SeedCitiesAsync(context);
        await DbInitializer.SeedCatalogAsync(context);
        logger.LogInformation("Генерация фейковых исполнителей для тестирования платформы...");
        await DbInitializer.SeedFakePerformersAsync(context);
        logger.LogInformation("Генерация ленты новостей шоу-бизнеса и ивентов...");
        await DbInitializer.SeedFakeNewsAsync(context);
        logger.LogInformation("Базовые данные успешно проверены / заполнены.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Критическая ошибка во время миграции или сидинга базы данных!");
    }
}

CultureProvider.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>());
_ = app.Services.GetRequiredService<IMapService>();

app.UseSwagger();
app.UseSwaggerUI();

// ПРАВКА 2: Включаем раздачу статики (картинки, аватарки), если они лежат локально в wwwroot
app.UseStaticFiles();

// ПРАВКА 1: Подключаем наш глобальный ловушечник исключений ДО авторизации
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chat");

app.Run();

// === ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ===

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        int id = connection.User.GetUserId();
        return id > 0 ? id.ToString() : null;
    }
}

public class AddRequiredHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAttribute = context.MethodInfo.GetCustomAttributes(true).OfType<LocalizationRequiredAttribute>().Any() ||
                           (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<LocalizationRequiredAttribute>().Any() ?? false);

        if (!hasAttribute) return;

        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "a_lang",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string", Default = new OpenApiString("ru") },
            Description = "Язык контента (ru, en, uz)"
        });
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LocalizationRequiredAttribute : Attribute { }

public class JsonDateTimeUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime().ToUniversalTime();
    }
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}