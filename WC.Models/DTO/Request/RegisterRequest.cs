using System.Text.Json.Serialization;
using Enums = WC.Models.Enums;

namespace WC.Models.DTO.Request
{
    public class RegisterRequest
    {
        public required string Username { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public Enums.Plans Plan { get; set; }
        [JsonIgnore]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
