using Ecommerce_API.DTOs.ProductDtos;
using FluentValidation;

namespace Ecommerce_API.Validators
{
    public class ProductCreateValidator : AbstractValidator<ProductCreateDto>
    {
        public ProductCreateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name cannot be empty")
                .MaximumLength(150).WithMessage("Product name must not exceed 300 characters");

            RuleFor(x => x.Price)
                .NotEmpty().WithMessage("Price name cannot be empty")
                .GreaterThan(0).WithMessage("Price must be greater than 0.");

            RuleFor(x => x.Stock)
                .NotEmpty().WithMessage("Stock cannot be empty")
                .GreaterThanOrEqualTo(0).WithMessage("Stock must be greater than 0 or equal to 0.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("CategoryId cannot be empty")
                .GreaterThan(0);

            RuleFor(x => x.Description)
               .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters  ");
        }
    }
}
