using Microsoft.AspNetCore.Mvc;
using WC.DataAccess.Models;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;
using WC.Service.IService;

namespace WC.PublicAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static DataAccess.Models.User user = new();
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
        public async Task<bool> Register([FromBody] RegisterRequest registerRequest)
        {
            var user = await _wcService.GetUser(registerRequest.Username);

            if (user == null)
            {
                //TODO: Optimize, less db requests (..GetUser)
                var userInsert = await _wcService.UserInsert(registerRequest);
                var userFromDb = await _wcService.GetUser(registerRequest.Username);

                var userPlanRequest = new UserPlanRequest
                {
                    UserId = userFromDb.UserId,
                    Plan = registerRequest.Plan
                };

                var planInsert = await _wcService.UserPlanInsert(userPlanRequest);

                if (userInsert == true && planInsert == true)
                {
                    return true;
                }
            }
            return false;
        }

        [HttpPost("[action]")]
        public async Task<Login?> Login([FromBody] LoginRequest loginRequest)
        {
            var loginResponse = new Login();
            var user = await _wcService.GetUser(loginRequest.Username);

            if (user != null)
            {
                if (BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
                {
                    var creation = DateTime.Now;
                    var expiration = creation.AddDays(Int32.Parse(_expiration!));

                    string tokenValue = _wcService.GenerateToken(user, _secret!, expiration);

                    if (tokenValue != null)
                    {
                        var token = new TokenRequest
                        {
                            UserId = user.UserId,
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
