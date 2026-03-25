namespace Ecommerce.Domain.Common;

/// <summary>Dòng đặt hàng: ProductId + số lượng (dùng cho repo, không phụ thuộc Application DTO).</summary>
public record OrderLineInput(int ProductId, int Quantity);
