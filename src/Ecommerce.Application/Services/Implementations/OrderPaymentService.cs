using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Ecommerce.Application.Services.Implementations;

public class OrderPaymentService : IOrderPaymentService
{
    private readonly IOrderRepo _orderRepo;
    private readonly ICacheService _cacheService;
    private readonly StripeSettings _stripe;

    public OrderPaymentService(
        IOrderRepo orderRepo,
        ICacheService cacheService,
        IOptions<StripeSettings> stripeOptions)
    {
        _orderRepo = orderRepo;
        _cacheService = cacheService;
        _stripe = stripeOptions.Value;
    }

    public async Task<ApiResponse<CheckoutSessionResponseDto>> CreateCheckoutSessionAsync(
        int userId,
        int orderId,
        CreateCheckoutSessionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("User is not authenticated.", 401);

        if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Stripe is not configured.", 503);

        if (!dto.SuccessUrl.Contains("{CHECKOUT_SESSION_ID}", StringComparison.Ordinal))
        {
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse(
                "SuccessUrl must include the literal placeholder {CHECKOUT_SESSION_ID}.",
                400);
        }

        var order = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(orderId, userId, cancellationToken);
        if (order == null)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Order not found.", 404);

        if (order.Status != OrderStatus.Pending)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Order is not awaiting payment.", 400);

        if (order.PaymentExpiresAt.HasValue && order.PaymentExpiresAt.Value < DateTime.UtcNow)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Order payment window has expired.", 400);

        var currency = string.IsNullOrWhiteSpace(_stripe.DefaultCurrency)
            ? "usd"
            : _stripe.DefaultCurrency.Trim().ToLowerInvariant();

        var lineItems = order.OrderItems.Select(oi =>
        {
            var unitAmount = ToStripeUnitAmount(oi.PriceAtPurchase, currency);
            var name = oi.Product?.Name ?? $"Product {oi.ProductId}";
            return new SessionLineItemOptions
            {
                Quantity = oi.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = currency,
                    UnitAmountDecimal = unitAmount,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = name
                    }
                }
            };
        }).ToList();

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = dto.SuccessUrl,
            CancelUrl = dto.CancelUrl,
            ClientReferenceId = order.Id.ToString(),
            Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() },
            LineItems = lineItems
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(options, requestOptions: null, cancellationToken);

        var saved = await _orderRepo.TrySetStripeCheckoutSessionIdAsync(orderId, userId, session.Id, cancellationToken);
        if (!saved)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Could not attach checkout session to order.", 409);

        await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

        return ApiResponse<CheckoutSessionResponseDto>.SuccessResponse(
            new CheckoutSessionResponseDto { CheckoutUrl = session.Url, SessionId = session.Id });
    }

    private static decimal ToStripeUnitAmount(decimal price, string currency)
    {
        var zeroDecimal = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
        };

        if (zeroDecimal.Contains(currency))
            return decimal.Round(price, 0, MidpointRounding.AwayFromZero);

        return decimal.Round(price * 100m, 0, MidpointRounding.AwayFromZero);
    }
}
