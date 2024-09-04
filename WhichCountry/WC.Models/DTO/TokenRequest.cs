namespace WC.Models.DTO
{
    public class TokenRequest
    {
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } 

        public DateTime ValidUntil { get; set; }

        public string? TokenValue { get; set; }
    }
}
