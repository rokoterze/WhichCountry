using Microsoft.AspNetCore.Mvc;
using WC.DataAccess.Models;
using WC.Models.DTO;
using WC.Service.IService;

namespace WC.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static User user = new();
        private readonly IWcService _wcService;
        private readonly string? _secret;
        private readonly string? _expiration;

        public AuthController(IWcService wcService, IConfiguration configuration)
        {
            _wcService = wcService;
            _secret = configuration["AppSettings:Secret"];
            _expiration = configuration["AppSettings:TokenExpiration"];
        }

        [HttpPost("[action]")]
        public async Task<bool> Register([FromBody] UserRequest request)
        {
            var userFromDb = await _wcService.GetUser(request.Username);

            if (userFromDb == null)
            {
                var result = await _wcService.SaveUser(request);

                if (result == true)
                {
                    return true;
                }
            }
            return false;
        }

        [HttpPost("[action]")]
        public async Task<string> Login([FromBody] UserRequest userRequest)
        {
            var user = await _wcService.GetUser(userRequest.Username);

            if (user != null)
            {
                if (BCrypt.Net.BCrypt.Verify(userRequest.Password, user.PasswordHash))
                {
                    var creation = DateTime.Now;
                    var expiration = creation.AddMilliseconds(Int32.Parse(_expiration!));

                    string tokenValue = _wcService.CreateToken(user, _secret!, expiration);

                    if (tokenValue != null)
                    {
                        var token = new TokenRequest
                        {
                            UserId = user.Id,
                            CreatedAt = creation,
                            ValidUntil = expiration,
                            TokenValue = tokenValue
                        };

                        await _wcService.SaveToken(token);
                        return token.TokenValue;
                    }
                    else
                    {
                        return "Token not created!";
                    }
                }
                else
                {
                    return "Password does not match!";
                }
            }
            else
            {
                return "User does not exist!";
            }
        }
    }
}
