namespace WC.Models.DTO
{
    public class UserResponse
    {
        public int Id { get; set; }

        public string? Username { get; set; }

        public string? PasswordHash { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
