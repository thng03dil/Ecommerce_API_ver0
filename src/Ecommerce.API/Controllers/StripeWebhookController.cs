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

    /// Stripe gửi raw JSON; chữ ký header Stripe-Signature bắt buộc. Pipeline phải bật buffering cho path này (xem Program.cs).
    /// Subscribe: checkout.session.completed, payment_intent.payment_failed, checkout.session.async_payment_failed, checkout.session.expired.

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

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                if (stripeEvent.Data.Object is Session completedSession
                    && string.Equals(completedSession.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                {
                    var ok = await _orderRepo.TryMarkPaidByStripeSessionAsync(
                        completedSession.Id,
                        completedSession.PaymentIntentId,
                        cancellationToken);

                    if (!ok)
                        _logger.LogWarning("Checkout session {SessionId} did not match a pending order for mark paid.", completedSession.Id);
                }
                break;

            case "payment_intent.payment_failed":
                if (stripeEvent.Data.Object is PaymentIntent pi)
                {
                    var err = FormatStripePaymentError(pi);
                    if (TryGetOrderIdFromMetadata(pi.Metadata, out var orderId))
                    {
                        var ok = await _orderRepo.TryMarkPaymentFailedByOrderIdAsync(orderId, err, cancellationToken);
                        if (!ok)
                            _logger.LogWarning(
                                "payment_intent.payment_failed: order {OrderId} not updated (not pending or already final).",
                                orderId);
                    }
                    else
                        _logger.LogWarning(
                            "payment_intent.payment_failed: missing orderId in PaymentIntent metadata (PI {PaymentIntentId}).",
                            pi.Id);
                }
                break;

            case "checkout.session.async_payment_failed":
                if (stripeEvent.Data.Object is Session asyncFailedSession)
                {
                    var ok = await _orderRepo.TryMarkPaymentFailedByStripeSessionAsync(
                        asyncFailedSession.Id,
                        "Async payment failed",
                        cancellationToken);
                    if (!ok)
                        _logger.LogWarning(
                            "checkout.session.async_payment_failed: session {SessionId} did not update an order.",
                            asyncFailedSession.Id);
                }
                break;

            case "checkout.session.expired":
                if (stripeEvent.Data.Object is Session expiredSession)
                {
                    var ok = await _orderRepo.TryMarkPaymentFailedByStripeSessionAsync(
                        expiredSession.Id,
                        "Checkout session expired",
                        cancellationToken);
                    if (!ok)
                        _logger.LogWarning(
                            "checkout.session.expired: session {SessionId} did not update an order.",
                            expiredSession.Id);
                }
                break;
        }

        return Ok();
    }

    private static bool TryGetOrderIdFromMetadata(Dictionary<string, string>? metadata, out int orderId)
    {
        orderId = 0;
        if (metadata == null || !metadata.TryGetValue("orderId", out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;
        return int.TryParse(raw.Trim(), out orderId);
    }

    private static string? FormatStripePaymentError(PaymentIntent pi)
    {
        var msg = pi.LastPaymentError?.Message;
        var code = pi.LastPaymentError?.Code;
        if (string.IsNullOrEmpty(msg))
            return code;
        if (string.IsNullOrEmpty(code))
            return msg;
        return $"{code}: {msg}";
    }
}
