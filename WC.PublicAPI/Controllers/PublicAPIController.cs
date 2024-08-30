using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;


namespace WC.PublicAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PublicAPIController : ControllerBase
    {
        private readonly WcApiClient _client;

        public PublicAPIController( WcApiClient client)
        {           
            _client = client;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<GeoLocationResponse?> CheckIpAddressGeoLocation(string ipAddress)
        {
            var result = await _client.IPAddressGeoLocationAsync(ipAddress);           
            return result;
        }
    }
}
