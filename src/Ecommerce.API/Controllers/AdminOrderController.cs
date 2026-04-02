using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers;

[Authorize]
[Route("api/admin/orders")]
[ApiController]
public class AdminOrderController : ControllerBase
{
    private readonly IOrderAdminService _orderAdminService;

    public AdminOrderController(IOrderAdminService orderAdminService)
    {
        _orderAdminService = orderAdminService;
    }

    [HttpGet]
    [Authorize(Policy = "order.manage.read")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination, CancellationToken cancellationToken)
    {
        var result = await _orderAdminService.GetAllOrdersAsync(pagination, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId:int}")]
    [Authorize(Policy = "order.manage.read")]
    public async Task<IActionResult> GetByUser(int userId, [FromQuery] PaginationDto pagination, CancellationToken cancellationToken)
    {
        var result = await _orderAdminService.GetOrdersByUserAsync(userId, pagination, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "order.manage.read")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
    {
        var result = await _orderAdminService.GetOrderDetailAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:int}/cancel")]
    [Authorize(Policy = "order.manage.update")]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken cancellationToken)
    {
        var data = await _orderAdminService.CancelOrderAsync(id, cancellationToken);
        return OkResponse(data, "Order cancelled successfully.");
    }

    [HttpPost("{id:int}/approve-return")]
    [Authorize(Policy = "order.manage.update")]
    public async Task<IActionResult> ApproveReturn(int id, CancellationToken cancellationToken)
    {
        var data = await _orderAdminService.ApproveReturnAsync(id, cancellationToken);
        return OkResponse(data, "Return approved; order cancelled and stock restored.");
    }

    /// <summary>Chuyển đơn đã thanh toán: Paid → Shipping → Completed (theo rule repo). Status qua query (Swagger: không cần body JSON).</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "order.manage.update")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] AdminOrderFulfillmentStatus newOrderStatus,
        CancellationToken cancellationToken)
    {
        var result = await _orderAdminService.UpdateOrderStatusAsync(id, newOrderStatus, cancellationToken);
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

    private IActionResult OkResponse<T>(T data, string message = "Success")
    {
        return Ok(new
        {
            success = true,
            message,
            data,
            timestamp = DateTime.UtcNow
        });
    }
}
