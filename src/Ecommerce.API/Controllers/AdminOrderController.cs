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
    public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
    {
        var result = await _orderAdminService.GetAllOrdersAsync(pagination);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId:int}")]
    [Authorize(Policy = "order.manage.read")]
    public async Task<IActionResult> GetByUser(int userId, [FromQuery] PaginationDto pagination)
    {
        var result = await _orderAdminService.GetOrdersByUserAsync(userId, pagination);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "order.manage.read")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var result = await _orderAdminService.GetOrderDetailAsync(id);
        return ToActionResult(result);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Policy = "order.manage.update")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequestDto request)
    {
        var result = await _orderAdminService.UpdateStatusAsync(id, request);
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

        return OkResponse(result.Data, result.Message);
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
