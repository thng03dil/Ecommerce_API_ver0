using Ecommerce.API.Middleware;
using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
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
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

//log 
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Redis Configuration
var redisConn = builder.Configuration["Redis:ConnectionString"];
var instanceName = builder.Configuration["Redis:InstanceName"] ?? "Ecommerce:";

if (!string.IsNullOrEmpty(redisConn))
{
    // Register Multiplexer (Singleton) for advanced Redis ops inside RedisCacheService
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var configuration = ConfigurationOptions.Parse(redisConn, true);
        configuration.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(configuration);
    });

    // Register Distributed Cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = instanceName;
    });
}else
{
    // Fallback : use memory cache if redis is not configured
    builder.Services.AddDistributedMemoryCache();
    
    Log.Warning("Redis ConnectionString is missing. Using MemoryCache instead.");
}

builder.Services.AddSingleton<ICacheService, RedisCacheService>();

//Register Repository
builder.Services.AddScoped<ICategoryRepo, CategoryRepo>();
builder.Services.AddScoped<IProductRepo, ProductRepo>(); 
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IRefreshTokenRepo , RefreshTokenRepo>();
builder.Services.AddScoped<IRoleRepo, RoleRepo>();
builder.Services.AddScoped<IPermissionRepo, PermissionRepo>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Service
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.Configure<AuthSecuritySettings>(
    builder.Configuration.GetSection("AuthSecurity"));

var fingerprintSecret = builder.Configuration["AuthSecurity:FingerprintSecret"];
if (string.IsNullOrWhiteSpace(fingerprintSecret))
    throw new InvalidOperationException("AuthSecurity:FingerprintSecret is required. Set via User Secrets: dotnet user-secrets set \"AuthSecurity:FingerprintSecret\" \"your-secret-32-chars-min\" --project src/Ecommerce.API");

builder.Services.AddScoped<ISecurityFingerprintHelper, SecurityFingerprintHelper>();
builder.Services.AddScoped<ISessionValidationService, SessionValidationService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

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

//register JwtSettings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

//configure JWT in swagger and swagger global header.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce API", Version = "v1" });

    c.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];

        var order = controllerName switch
        {
            "Auth" => "1",
            "Category" => "2",
            "Product" => "3",
            "User" => "4",
            "Role" => "5",
            "Permission" => "6",
            _ => "99"
        };
        
        return new[] { $"{order}. {controllerName}" };
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });

    c.AddSecurityDefinition("DeviceId", new OpenApiSecurityScheme
        {
            Description = "Mandatory device identifier for auth. Example: X-Device-Id: swagger-device",
            Name = "X-Device-Id",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "DeviceId" }
            },
            Array.Empty<string>()
        }
    });
});

//configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    var secret = builder.Configuration["Jwt:Key"];

    if (string.IsNullOrWhiteSpace(secret))
    {
        throw new Exception("JWT Key is missing (env: Jwt__Key)");
    }

    if (secret.Length < 32)
    {
        throw new Exception("JWT Key must be at least 32 characters");
    }

    var key = Encoding.UTF8.GetBytes(secret);

    options.TokenValidationParameters =
    new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();

            logger.LogWarning(context.Exception, "JWT authentication failed");

            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();

            if (context.AuthenticateFailure is SecurityTokenExpiredException)
            
                throw new UnauthorizedException("Token has expired");
            

            if (context.AuthenticateFailure is SecurityTokenInvalidSignatureException)
            {
                throw new UnauthorizedException("Invalid token signature");
            }

            if (context.AuthenticateFailure is SecurityTokenException)
            {
                throw new UnauthorizedException("Invalid token");
            }

            throw new UnauthorizedException("Authentication failed : You are not authorized to access this resource.");
        },
        OnForbidden = async context =>
        {
            throw new ForbiddenException("Access denied: You do not have permission"); 
                
        }
    };
});

//register custom authorization handler and policy provider
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();


var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();


app.MapControllers();

// call seeder
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await DataSeeder.SeedAdminAsync(context);
}

app.Run();
