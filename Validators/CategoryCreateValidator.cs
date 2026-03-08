using Ecommerce_API.DTOs.CategoryDtos;
using FluentValidation;

namespace Ecommerce_API.Validators
{
    public class CategoryCreateValidator : AbstractValidator<CategoryCreateDto>
    {
        public CategoryCreateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Slug)
                .NotEmpty();

            RuleFor(x => x.Description)
                .MaximumLength(300);
        }
    }
}
