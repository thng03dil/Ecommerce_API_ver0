using Ecommerce_API.DTOs.ProductDtos;
using FluentValidation;

namespace Ecommerce_API.Validators
{
    public class ProductCreateValidator : AbstractValidator<ProductCreateDto>
    {
        public ProductCreateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty();

            RuleFor(x => x.Price)
                .GreaterThan(0);

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.CategoryId)
                .GreaterThan(0);
        }
    }
}
