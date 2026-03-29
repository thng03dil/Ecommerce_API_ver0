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

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var result = await _orderService.PlaceOrderAsync(CurrentUserId, dto, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders(CancellationToken cancellationToken)
    {
        var result = await _orderService.ListForUserAsync(CurrentUserId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetByIdForUserAsync(CurrentUserId, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/checkout")]
    public async Task<IActionResult> CreateCheckout(
        int id,
        [FromBody] CreateCheckoutSessionDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _orderPaymentService.CreateCheckoutSessionAsync(CurrentUserId, id, dto, cancellationToken);
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
