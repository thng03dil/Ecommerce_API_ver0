using Ecommerce_API.Helpers;
using FluentValidation;
namespace Ecommerce_API.Validators
{
    public class PaginationValidator : AbstractValidator<Pagination>
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
