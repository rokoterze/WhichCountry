using Microsoft.AspNetCore.Http;
using WC.Models.DTO;

namespace WC.Service.IService
{
    public interface IWcService
    {
        #region GeoLocation
        GeoLocationResponse? GetGeoLocation(int numericIpAddress);
        Task<bool> SaveGeoLocation(GeoLocationRequest request);
        Task<List<GeoLocationInfo>?> GetGeoLocations(string? countryCode);
        #endregion

        #region CountryDetails
        Task<CountryDetailsResponse?> GetCountry(string? countryCode);
        Task<bool> SaveCountry(string? countryCode, string? provider);
        Task<bool> CountryExists(string? countryCode);
        Task<RestCountriesResponse?> GetCountryDetailsFromProvider(string? countryCode, string? provider);
        #endregion

        #region Helpers
        List<CsvUpload>? ConvertCSVToList(IFormFile request);
        int ConvertIpToNumber(string ipAddress);
        #endregion
    }
}
