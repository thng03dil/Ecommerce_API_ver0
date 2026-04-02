using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class OrderController : BaseController
{
    private readonly IOrderService _orderService;
    private readonly IOrderPaymentService _orderPaymentService;

    public OrderController(IOrderService orderService, IOrderPaymentService orderPaymentService)
    {
        _orderService = orderService;
        _orderPaymentService = orderPaymentService;
    }

  [Authorize(Policy = "order.create")]
    [HttpPost]
    public async Task<IActionResult> Place([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var result = await _orderService.PlaceOrderAsync(CurrentUserId, dto, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = "order.read")]
    [HttpGet]
    public async Task<IActionResult> MyOrders(CancellationToken cancellationToken)
    {
        var result = await _orderService.ListForUserAsync(CurrentUserId, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = "order.read")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetByIdForUserAsync(CurrentUserId, id, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = "order.cancel")]
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var data = await _orderService.CancelPendingOrderAsync(CurrentUserId, id, cancellationToken);
        return OkResponse(data, "Order cancelled successfully.");
    }

    [Authorize(Policy = "order.checkout")]
    [HttpPost("{id:int}/checkout")]
    public async Task<IActionResult> CreateCheckout(int id, CancellationToken cancellationToken)
    {
        var result = await _orderPaymentService.CreateCheckoutSessionAsync(CurrentUserId, id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Yêu cầu hoàn hàng (chỉ đơn đã hoàn thành và đã thanh toán). Admin duyệt qua approve-return.
    /// </summary>
    [Authorize(Policy = "order.cancel")]
    [HttpPost("{id:int}/return-request")]
    public async Task<IActionResult> RequestReturn(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.RequestReturnAsync(CurrentUserId, id, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(ApiResponse<T> result)
    {
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new
            {
                success = false,
                message = result.Message,
                timestamp = DateTime.UtcNow
            });
        }

        return OkResponse(result.Data!, result.Message);
    }
}
