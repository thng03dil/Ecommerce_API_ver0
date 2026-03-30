namespace Ecommerce.Domain.Common.Settings;

public class StripeSettings
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string? PublishableKey { get; set; }

    /// <summary>ISO currency code for Checkout (e.g. usd, usd).</summary>
    public string DefaultCurrency { get; set; } = "usd";
}
