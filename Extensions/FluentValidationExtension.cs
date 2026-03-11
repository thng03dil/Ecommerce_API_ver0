using FluentValidation.Results;
using Ecommerce_API.Exceptions;

namespace Ecommerce_API.Extensions
{
    public static class FluentValidationExtensions
    {
        public static void ThrowIfInvalid(this ValidationResult result)
        {
            if (result.IsValid) return;

            var errors = result.Errors
                .Select(e => new ValidationError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                })
                .ToList();

            throw new ValidationException(errors);
        }
    }
}