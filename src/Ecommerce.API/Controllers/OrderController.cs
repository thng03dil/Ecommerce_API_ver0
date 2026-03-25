//using Ecommerce.Application.DTOs.OrderDtos;
//using Ecommerce.Application.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace Ecommerce.API.Controllers;

//[Authorize]
//public class OrderController : BaseController
//{
//    private readonly IOrderService _orderService;

//    public OrderController(IOrderService orderService)
//    {
//        _orderService = orderService;
//    }

//   // [HttpPost]
//    public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
//    {
//        var result = await _orderService.PlaceOrderAsync(CurrentUserId, dto, cancellationToken);
//        return OkResponse(result);
//    }
//}
