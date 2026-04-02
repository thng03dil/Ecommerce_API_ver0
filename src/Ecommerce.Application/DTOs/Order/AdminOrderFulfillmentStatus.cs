using System.Text.Json.Serialization;

namespace Ecommerce.Application.DTOs.Order;

/// <summary>
/// Trạng thái admin được phép gán qua PATCH status (khớp giá trị <see cref="Ecommerce.Domain.Enums.OrderStatus"/>).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdminOrderFulfillmentStatus
{
    Shipping = 2,
    Completed = 4
}
