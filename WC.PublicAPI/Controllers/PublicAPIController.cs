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
        public async Task<GeoLocationResponse?> CheckIpAddressGeoLocation([FromQuery] string ipAddress)
        {
            var result = await _client.IPAddressGeoLocationAsync(ipAddress);
            return result;
        }
        #endregion
    }
}
