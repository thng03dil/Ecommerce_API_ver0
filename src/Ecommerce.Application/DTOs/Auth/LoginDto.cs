using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Stable device identifier (UUID). Sent from body for easy Swagger/Postman testing.
        /// Falls back to X-Device-Id header when omitted.
        /// </summary>
        [MaxLength(256)]
        public string? DeviceId { get; set; }
    }
}
  