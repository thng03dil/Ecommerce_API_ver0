using Ecommerce_API.DTOs.CategoryDtos;
using Ecommerce_API.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_API.Validators
{
    public class CategoryCreateValidator : AbstractValidator<CategoryCreateDto>
    {
        private readonly AppDbContext _context;
        public CategoryCreateValidator(AppDbContext context)
        {
            _context = context;
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name cannot be empty")
                .MaximumLength(100).WithMessage("Category name must not exceed 300 characters  ");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug cannot be empty ")
                .MaximumLength(100).WithMessage("Slug must not exceed 100 characters  ")
                .Must((dto, slug) =>
                {
                    return !_context.Categories.Any(c => c.Slug == slug);
                })
                .WithMessage("Slug already existed ");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 300 characters  ") ;
        }
    }
}
