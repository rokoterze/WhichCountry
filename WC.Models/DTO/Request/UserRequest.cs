using System.Text.Json.Serialization;

namespace WC.Models.DTO.Request
{
    public class UserRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        [JsonIgnore]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
