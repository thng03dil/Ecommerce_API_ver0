using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using Stripe;

namespace Ecommerce.Application.Services.Implementations;

public class OrderPaymentService : IOrderPaymentService
{
    private const string StripeSuccessUrlKey = "Stripe:SuccessUrl";
    private const string StripeCancelUrlKey = "Stripe:CancelUrl";

    private readonly IOrderRepo _orderRepo;
    private readonly ICacheService _cacheService;
    private readonly StripeSettings _stripe;
    private readonly IConfiguration _configuration;

    public OrderPaymentService(
        IOrderRepo orderRepo,
        ICacheService cacheService,
        IOptions<StripeSettings> stripeOptions,
        IConfiguration configuration)
    {
        _orderRepo = orderRepo;
        _cacheService = cacheService;
        _stripe = stripeOptions.Value;
        _configuration = configuration;
    }

    public async Task<ApiResponse<CheckoutSessionResponseDto>> CreateCheckoutSessionAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("User is not authenticated.", 401);

        if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Stripe is not configured.", 503);

        var successUrl = (_configuration[StripeSuccessUrlKey] ?? string.Empty).Trim();
        var cancelUrl = (_configuration[StripeCancelUrlKey] ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
        {
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse(
                "Stripe SuccessUrl and CancelUrl must be configured (Stripe:SuccessUrl, Stripe:CancelUrl).",
                503);
        }

        if (!successUrl.Contains("{CHECKOUT_SESSION_ID}", StringComparison.Ordinal))
        {
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse(
                "Stripe:SuccessUrl must include the literal placeholder {CHECKOUT_SESSION_ID}.",
                503);
        }

        var order = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(orderId, userId, cancellationToken);
        if (order == null)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Order not found.", 404);

        if (order.Status != OrderStatus.Pending)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Order is not awaiting payment.", 400);

        if (order.PaymentStatus == PaymentStatus.Succeeded)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Order is already paid.", 400);

        var currency = string.IsNullOrWhiteSpace(_stripe.DefaultCurrency)
            ? "usd"
            : _stripe.DefaultCurrency.Trim().ToLowerInvariant();

        var lineItems = order.OrderItems.Select(oi =>
        {
            var unitAmount = ToStripeUnitAmountSmallest(oi.PriceAtPurchase, currency);
            var name = oi.Product?.Name ?? $"Product {oi.ProductId}";
            return new SessionLineItemOptions
            {
                Quantity = oi.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = currency,
                    UnitAmount = unitAmount,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = name
                    }
                }
            };
        }).ToList();

        var orderIdMeta = order.Id.ToString();
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = orderIdMeta,
            Metadata = new Dictionary<string, string> { ["orderId"] = orderIdMeta },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string> { ["orderId"] = orderIdMeta }
            },
            LineItems = lineItems
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(options, requestOptions: null, cancellationToken);

        var saved = await _orderRepo.TrySetStripeCheckoutSessionIdAsync(orderId, userId, session.Id, cancellationToken);
        if (!saved)
            return ApiResponse<CheckoutSessionResponseDto>.ErrorResponse("Could not attach checkout session to order.", 409);

        await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

        var reloaded = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(orderId, userId, cancellationToken);

        return ApiResponse<CheckoutSessionResponseDto>.SuccessResponse(
            new CheckoutSessionResponseDto
            {
                CheckoutUrl = session.Url ?? string.Empty,
                SessionId = session.Id,
                Order = reloaded != null ? OrderResponseMapper.ToDto(reloaded) : null
            });
    }
    public async Task<bool> RefundAsync(string paymentIntentId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(paymentIntentId)) return false;

        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
            };

            var service = new RefundService();
            var refund = await service.CreateAsync(options, cancellationToken: ct);

            // Trả về true nếu thành công hoặc đang chờ xử lý
            return refund.Status == "succeeded" || refund.Status == "pending";
        }
        catch (StripeException ex)
        {
            // Log lỗi: ví dụ tài khoản Stripe không đủ số dư để refund
            Console.WriteLine($"Stripe Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>Stripe smallest currency unit: USD = cents (price × 100); USD = whole units as <c>long</c>.</summary>
    private static long ToStripeUnitAmountSmallest(decimal price, string currency)
    {
        var c = currency.Trim().ToLowerInvariant();

        if (IsZeroDecimalCurrency(c))
        {
            return (long)decimal.Round(price, 0, MidpointRounding.AwayFromZero);
        }

        return (long)decimal.Round(price * 100m, 0, MidpointRounding.AwayFromZero);
    }

    private static bool IsZeroDecimalCurrency(string currency) =>
        ZeroDecimalCurrencies.Contains(currency);

    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vuv", "xaf", "xof", "xpf"
    };
}
