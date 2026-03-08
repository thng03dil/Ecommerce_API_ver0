namespace Ecommerce_API.Models
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
