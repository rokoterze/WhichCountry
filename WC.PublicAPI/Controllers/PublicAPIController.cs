using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace WC.PublicAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class PublicAPIController : ControllerBase
    {
        private readonly WcApiClient _client;

        public PublicAPIController(WcApiClient client)
        {
            _client = client;
        }

        #region GeoLocation
        [HttpGet("[action]")]
        public async Task<GeoLocation?> CheckIpAddressGeoLocation([FromQuery] string ipAddress)
        {
            var result = await _client.IPAddressGeoLocationAsync(ipAddress);
            return result;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult?> GetCountryFlag([FromQuery] string countryCode)
        {
            var file = await _client.GetCountryFlagAsync(countryCode);

            if (file == null || file.Content == null)
            {
                return null;
            }

            var result = File(file.Content, file.MimeType, file.FileName);
            return result;
        }
        
        [HttpGet("[action]")]
        public async Task<FileResult?> GetCountryFlagBase64([FromQuery] string countryCode)
        {
            var result = await _client.GetCountryFlagAsync(countryCode);
            return result;
        }
        #endregion
    }
}
