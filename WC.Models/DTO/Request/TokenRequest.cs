namespace WC.Models.DTO.Request
{
    public class TokenRequest
    {
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ValidUntil { get; set; }

        public string? TokenValue { get; set; }
    }
}
