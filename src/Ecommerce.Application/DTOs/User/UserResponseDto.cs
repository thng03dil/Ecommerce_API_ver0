namespace Ecommerce.Application.DTOs.User
{
    public class UserResponseDto
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }
}
  