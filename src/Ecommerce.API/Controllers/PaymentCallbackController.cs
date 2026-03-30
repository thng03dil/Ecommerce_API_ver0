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
    [AllowAnonymous] // Cho phép Stripe redirect về mà không cần Token
    public async Task<IActionResult> Success([FromQuery] string session_id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(session_id))
            return BadRequest("Missing session_id");

        // Giao diện JSON trả về để bạn kiểm tra
        return Ok(new
        {
            Status = "Payment Successful",
            Message = "Trình duyệt đã quay về API thành công.",
            StripeSessionId = session_id,
            Instruction = "Bây giờ hãy kiểm tra Database hoặc gọi API Get Order để xem trạng thái đã thành 'Paid' chưa (nhờ Webhook xử lý)."
        });
    }

    [HttpGet("cancel")]
    [AllowAnonymous]
    public IActionResult Cancel()
    {
        return Ok(new
        {
            Status = "Payment Cancelled",
            Message = "Người dùng đã nhấn quay lại hoặc hủy thanh toán."
        });
    }
}