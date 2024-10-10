using Microsoft.AspNetCore.Http;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;

namespace WC.Service.IService
{
    public interface IWcService
    {
        #region GeoLocation
        Task<GeoLocation?> GetGeoLocation(int numericIpAddress);
        Task<bool> GeoLocationInsert(GeoLocationRequest request);
        Task<List<GeoLocationInfo>?> GetGeoLocations(string? countryCode);
        #endregion

        #region CountryDetails
        Task<CountryDetails?> GetCountry(string? countryCode);
        Task<bool> CountryInsert(string? countryCode, string? provider, string? imagePath, string? extension);
        Task<bool> DoesCountryExist(string? countryCode);
        Task<RestCountries?> GetCountryDetailsFromProvider(string? countryCode, string? provider);
        #endregion

        #region User and Token
        Task<bool> UserInsert(RegisterRequest user);
        Task<User> GetUser(string username);
        Task<bool> TokenInsert(TokenRequest token);
        #endregion

        #region Plans
        Task<bool> UserPlanInsert(UserPlanRequest userPlanRequest);
        Task<bool> UserPlanHistoryInsert()
        #endregion

        #region Helpers
        List<CsvUpload>? ConvertCSVToList(IFormFile request);
        int ConvertIpToNumber(string ipAddress);
        string GenerateToken(User user, string secret, DateTime expiration);
        Task<bool> DownloadImage(string url, string outputFolder, string outputFileName, string extension);
        #endregion
    }
}
