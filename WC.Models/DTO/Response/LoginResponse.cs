namespace WC.Models.DTO.Response
{
    public class LoginResponse
    {
        public string? Token { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
