using Ecommerce_API.DTOs.Common;
using FluentValidation;
namespace Ecommerce_API.Validators.CommonValidators
{
    public class PaginationValidator : AbstractValidator<PaginationDto>
    {
        public PaginationValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size must be greater than 0.")
                .LessThanOrEqualTo(100)
                .WithMessage("Page size cannot exceed 100 items per page.");
        }
    }
}
