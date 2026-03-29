using Ecommerce.API;
using Ecommerce.API.BackgroundServices;
using Ecommerce.API.Extensions;
using Ecommerce.API.Middleware;
using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
using Stripe;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Data.Seed;
using Ecommerce.Infrastructure.RedisCaching;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.SecurityHelpers;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using System.Text.Json;

DotEnvBootstrap.LoadIfPresent();

var builder = WebApplication.CreateBuilder(args);

//log 
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// =========================   APPLICATION( services )   ===================================
// ===========   INFRASTRUCTURE ( database, redis configuration, repository )   ============
// =========================   AUTHENTICATION  ( JWT ) =====================================
// =========================   SWAGGER CONFIGURATION   =====================================
// =========================   CUSTOM AUTHORIZATION   =====================================

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddAuthenticationServices(builder.Configuration)
    .AddSwaggerDocumentation()
    .AddAuthorizationServices();



builder.Services.Configure<AuthSecuritySettings>(
builder.Configuration.GetSection("AuthSecurity"));

builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection(StripeSettings.SectionName));

builder.Services.AddHostedService<ExpiredPendingOrderCleanupService>();

var fingerprintSecret = builder.Configuration["AuthSecurity:FingerprintSecret"];
if (string.IsNullOrWhiteSpace(fingerprintSecret))
    throw new InvalidOperationException("AuthSecurity:FingerprintSecret is required. Set via User Secrets: dotnet user-secrets set \"AuthSecurity:FingerprintSecret\" \"your-secret-32-chars-min\" --project src/Ecommerce.API");


builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

//configure validation error response format
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value!.Errors.Count > 0)
            .Select(x => new
            {
                field = x.Key,
                messages = x.Value!.Errors.Select(e => e.ErrorMessage)
            });

        var response = new ErrorResponseDto
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Success = false,
            ErrorCode = "VALIDATION_ERROR",
            Message = "Validation failed",
            Path = context.HttpContext.Request.Path,
            TraceId = context.HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow,
            Errors = errors
        };

        return new BadRequestObjectResult(response);
    };
});


builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

var stripeSecretKey = app.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeSecretKey))
    StripeConfiguration.ApiKey = stripeSecretKey;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "[RequestId: {RequestId}] HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null)
            return LogEventLevel.Error;
        if (httpContext.Response.StatusCode > 499)
            return LogEventLevel.Error;
        if (httpContext.Response.StatusCode > 399)
            return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };
});

app.UseAuthentication();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();

app.UseWhen(
    ctx => string.Equals(ctx.Request.Path.Value, "/api/stripe/webhook", StringComparison.OrdinalIgnoreCase),
    branch => branch.Use(async (ctx, next) =>
    {
        ctx.Request.EnableBuffering();
        await next();
    }));

app.MapControllers();

// call seeder
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await DataSeeder.SeedAdminAsync(context);
}

app.Run();
