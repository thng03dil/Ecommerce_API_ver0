using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers;

[Route("api/payment-callback")]
public class PaymentCallbackController : ControllerBase
{
    private readonly IOrderService _orderService;

    public PaymentCallbackController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("success")]
    [AllowAnonymous] 
    public async Task<IActionResult> Success([FromQuery] string session_id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(session_id))
            return BadRequest("Missing session_id");

        await Task.CompletedTask;

        return Ok(new
        {
            Status = "Payment Successful",
            Message = "The browser has successfully redirected back to the API.",
            StripeSessionId = session_id,
            Instruction = "Please check the database or call the Get Order API to verify if the status has been updated to 'Paid' (processed via Webhook)."
        });
    }

    [HttpGet("cancel")]
    [AllowAnonymous]
    public IActionResult Cancel()
    {
        return Ok(new
        {
            Status = "Payment Cancelled",
            Message = "The user has clicked back or cancelled the payment process."
        });
    }
}