using Ecommerce_API.Data;
using Ecommerce_API.Services.Interfaces;
using Ecommerce_API.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using Ecommerce_API.Validators;
using Ecommerce_API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Service
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// register validation
builder.Services.AddFluentValidationAutoValidation();// auto check validation when api is called
builder.Services.AddValidatorsFromAssemblyContaining<CategoryCreateValidator>();// find class implement AbstractValidator

var app = builder.Build();


app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
