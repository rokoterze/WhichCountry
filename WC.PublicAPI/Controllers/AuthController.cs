using Microsoft.AspNetCore.Mvc;
using WC.DataAccess.Models;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;
using WC.Service.IService;

namespace WC.PublicAPI.Controllers
{
    [Route("api/[controller]")]
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
            _secret = configuration["TokenSettings:Secret"];
            _expiration = configuration["TokenSettings:TokenExpiration"];
        }

        [HttpPost("[action]")]
        public async Task<bool> Register([FromBody] UserRequest request)
        {
            var userFromDb = await _wcService.GetUser(request.Username);

            if (userFromDb == null)
            {
                var result = await _wcService.UserInsert(request);

                if (result == true)
                {
                    return true;
                }
            }
            return false;
        }

        [HttpPost("[action]")]
        public async Task<LoginResponse?> Login([FromBody] UserRequest userRequest)
        {
            var loginResponse = new LoginResponse();
            var user = await _wcService.GetUser(userRequest.Username);

            if (user != null)
            {
                if (BCrypt.Net.BCrypt.Verify(userRequest.Password, user.PasswordHash))
                {
                    var creation = DateTime.Now;
                    var expiration = creation.AddDays(Int32.Parse(_expiration!));

                    string tokenValue = _wcService.GenerateToken(user, _secret!, expiration);

                    if (tokenValue != null)
                    {
                        var token = new TokenRequest
                        {
                            UserId = user.Id,
                            CreatedAt = creation,
                            ValidUntil = expiration,
                            TokenValue = tokenValue
                        };

                        await _wcService.TokenInsert(token);

                        loginResponse.Token = tokenValue;

                        return loginResponse;
                    }
                    else
                    {
                        loginResponse.ErrorMessage = "Token not created!";
                        return loginResponse;
                    }
                }
                else
                {
                    loginResponse.ErrorMessage = "Password does not match!";
                    return loginResponse;
                }
            }
            else
            {
                loginResponse.ErrorMessage = "User does not exist!";
                return loginResponse;
            }
        }
    }
}
