using Microsoft.OpenApi.Models;

namespace Ecommerce.API.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                {
                    Title = "Ecommerce API", 
                    Version = "v1"
                });

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
                        "Order" => "7",
                        "StripeWebhook" => "8",
                        "AdminOrder" => "9",

                        _ => "99"
                    };

                    return new[] { $"{order}. {controllerName}" };
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Authorization: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                c.AddSecurityDefinition("DeviceId", new OpenApiSecurityScheme
                {
                    Description = "Mandatory device identifier for auth. \n Example: X-Device-Id: swagger-device",
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
            return services;
        }
        }
}
