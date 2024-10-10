namespace WC.Models.DTO.Response
{
    public class User
    {
        public int UserId { get; set; }

        public string? Username { get; set; }

        public string? PasswordHash { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
