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
        public async Task<string> Login([FromBody] UserRequest request)
        {
            var userFromDb = await _wcService.GetUser(request.Username);

            if (userFromDb != null)
            {
                if (BCrypt.Net.BCrypt.Verify(request.Password, userFromDb.PasswordHash))
                {
                    int expiration = Int32.Parse(_expiration!);

                    string token = _wcService.CreateToken(userFromDb, _secret!, expiration);
                    
                    //TODO: Save token to database (TokenRequest dto)
                    return token;
                }
                else 
                {
                    return null!;
                }
            }
            else 
            {
                return null!;
            }
        }
    }
}
