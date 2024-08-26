using Microsoft.AspNetCore.Http;
using WC.Models.DTO;

namespace WC.Service.IService
{
    public interface IWcService
    {
        List<CsvUpload> ConvertCSVToList(IFormFile request);
        Task<bool> SaveGeoLocation(GeoLocationRequest request);
        Task<bool> SaveCountryDetails(string countryCode, string? provider);
    }
}
