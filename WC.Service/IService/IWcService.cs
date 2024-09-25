using Microsoft.AspNetCore.Http;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;

namespace WC.Service.IService
{
    public interface IWcService
    {
        #region GeoLocation
        Task<GeoLocationResponse?> GetGeoLocation(int numericIpAddress);
        Task<bool> GeoLocationInsert(GeoLocationRequest request);
        Task<List<GeoLocationInfo>?> GetGeoLocations(string? countryCode);
        #endregion

        #region CountryDetails
        Task<CountryDetailsResponse?> GetCountry(string? countryCode);
        Task<bool> CountryInsert(string? countryCode, string? provider, string? imagePath, string? extension);
        Task<bool> DoesCountryExist(string? countryCode);
        Task<RestCountriesResponse?> GetCountryDetailsFromProvider(string? countryCode, string? provider);
        #endregion

        #region User and Token
        Task<bool> UserInsert(UserRequest user);
        Task<UserResponse> GetUser(string username);
        Task<bool> TokenInsert(TokenRequest token);
        #endregion

        #region Helpers
        List<CsvUpload>? ConvertCSVToList(IFormFile request);
        int ConvertIpToNumber(string ipAddress);
        string GenerateToken(UserResponse user, string secret, DateTime expiration);
        Task<bool> DownloadImage(string url, string outputFolder, string outputFileName, string extension);
        #endregion
    }
}
