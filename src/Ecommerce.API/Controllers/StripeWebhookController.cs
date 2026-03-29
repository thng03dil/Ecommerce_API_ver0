using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Ecommerce.API.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IOrderRepo _orderRepo;
    private readonly StripeSettings _stripe;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOrderRepo orderRepo,
        IOptions<StripeSettings> stripeOptions,
        ILogger<StripeWebhookController> logger)
    {
        _orderRepo = orderRepo;
        _stripe = stripeOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Stripe gửi raw JSON; chữ ký header Stripe-Signature bắt buộc. Pipeline phải bật buffering cho path này (xem Program.cs).
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_stripe.WebhookSecret))
        {
            _logger.LogWarning("Stripe webhook received but Stripe:WebhookSecret is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrEmpty(Request.Headers["Stripe-Signature"]))
            return BadRequest();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripe.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature or payload.");
            return BadRequest();
        }

        if (string.Equals(stripeEvent.Type, "checkout.session.completed", StringComparison.Ordinal))
        {
            if (stripeEvent.Data.Object is Session session)
            {
                if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                {
                    var ok = await _orderRepo.TryMarkPaidByStripeSessionAsync(
                        session.Id,
                        session.PaymentIntentId,
                        cancellationToken);

                    if (!ok)
                        _logger.LogWarning("Checkout session {SessionId} did not match a pending order.", session.Id);
                }
            }
        }

        return Ok();
    }
}
