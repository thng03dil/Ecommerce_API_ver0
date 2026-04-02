namespace Ecommerce.Domain.Interfaces;

public enum OrderReturnRequestFailure
{
    NotFound,
    NotEligible,
    AlreadyRequested
}

public sealed class OrderReturnRequestResult
{
    public bool Success { get; private init; }
    public OrderReturnRequestFailure? Failure { get; private init; }

    public static OrderReturnRequestResult Ok() =>
        new() { Success = true };

    public static OrderReturnRequestResult Fail(OrderReturnRequestFailure failure) =>
        new() { Success = false, Failure = failure };
}
