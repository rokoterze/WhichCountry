namespace WC.Models.DTO.Response
{
    public class Login
    {
        public string? Token { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
