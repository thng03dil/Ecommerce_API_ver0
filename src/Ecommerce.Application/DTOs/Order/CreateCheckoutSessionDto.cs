using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs.OrderDtos;

public class CreateCheckoutSessionDto
{
    /// <summary>
    /// Must include the literal <c>{CHECKOUT_SESSION_ID}</c> where Stripe should substitute the session id (see Stripe Checkout docs).
    /// </summary>
    [Required]
    [Url]
    public string SuccessUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string CancelUrl { get; set; } = string.Empty;
}
