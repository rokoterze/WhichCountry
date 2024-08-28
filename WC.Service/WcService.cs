using AutoMapper;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;
using System.Text.Json;
using WC.DataAccess.Models;
using WC.Models.DTO;
using WC.Service.IService;

namespace WC.Service
{
    public class WcService : IWcService
    {
        private readonly WhichCountryContext _context;
        private readonly HttpClient _httpClient;
        private readonly IMapper _mapper;
        public WcService(WhichCountryContext context, HttpClient httpClient)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });

            _mapper = config.CreateMapper();

            _context = context;
            _httpClient = httpClient;
        }

        #region GeoLocation
        public async Task<bool> SaveGeoLocation(GeoLocationRequest request)
        {
            try
            {
                var map = _mapper.Map<GeoLocation>(request);

                await _context.AddAsync(map);
                await _context.SaveChangesAsync();
            }
            catch
            {
                Log.Error($"Failed to save geolocation:\n{request.CountryCode}, {request.StartIp}, {request.EndIp}");
                return false;
            }
            return true;
        }
        public GeoLocationResponse? GetGeoLocation(int numericIpAddress)
        {
            try
            {
                var geoLocation = _context.GeoLocations
               .AsEnumerable()
               .Where(x => ConvertIpToNumber(x.StartIp) <= numericIpAddress && ConvertIpToNumber(x.EndIp) >= numericIpAddress).FirstOrDefault();

                if (geoLocation == null)
                {
                    return null;
                }
                else
                {
                    var mapper = _mapper.Map<GeoLocationResponse>(geoLocation);
                    return mapper;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get IP address from database: {numericIpAddress}\n {ex.Message}");
                return null;
            }
        }
        public async Task<List<GeoLocationResponse>?> GetGeoLocations(string countryCode)
        {
            List<GeoLocationResponse> geoLocations = [];
            try
            {
                var result = await _context.GeoLocations.Where(x => x.CountryCode == countryCode).ToListAsync();

                if (result == null)
                {
                    return null;
                }
                else
                {
                    foreach (var geoLocation in result)
                    {
                        var mapped = _mapper.Map<GeoLocationResponse>(result);
                        geoLocations.Add(mapped);
                    }
                }
            }
            catch
            {
                Log.Error($"Failed to fetch data from database:\nCountry Code: {countryCode}");
                return null;
            }

            return geoLocations;
        }
        #endregion

        #region CountryDetails
        public async Task<CountryDetailsResponse?> GetCountry(string? countryCode)
        {
            try
            {
                var country = await _context.CountryDetails.Where(x => x.CountryCode == countryCode).FirstOrDefaultAsync();

                if (country == null)
                {
                    return null;
                }
                else
                {
                    var mapped = _mapper.Map<CountryDetailsResponse>(country);
                    return mapped;
                }
            }
            catch
            {
                Log.Error($"Failed to get country from database:\nCountry code: {countryCode}");
                return null;
            }
        }
        public async Task<bool> SaveCountry(string countryCode, string? provider)
        {
            var country = await CountryExists(countryCode);

            if (!country)
            {
                var result = await GetCountryDetailsFromProvider(countryCode, provider);

                if (result == null)
                {
                    Log.Information($"Failed to retrieve country details for {countryCode}.");
                    return false;
                }

                try
                {
                    var map = _mapper.Map<CountryDetail>(result);

                    await _context.AddAsync(map);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    Log.Error($"Failed to save country details.\nCountry code: {countryCode}.");
                    return false;
                }
            }

            return true;
        }
        public async Task<bool> CountryExists(string countryCode)
        {
            try
            {
                var country = await _context.CountryDetails.FirstOrDefaultAsync(x => x.CountryCode == countryCode);

                if (country == null)
                {
                    return false;
                }
            }
            catch
            {
                Log.Error($"Failed to retrieve response.\nCountry code: {countryCode}.");
                return false;
            }

            return true;
        }
        public async Task<RestCountriesResponse?> GetCountryDetailsFromProvider(string countryCode, string? provider)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(provider + countryCode);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var deserialized = JsonSerializer.Deserialize<List<RestCountriesResponse>>(responseBody);

                return deserialized?.FirstOrDefault();
            }
            catch
            {
                Log.Error($"Failed to retrieve country details from provider.\nCountry code: {countryCode}");
                return null;
            }
        }
        #endregion

        #region Helpers
        public List<CsvUpload>? ConvertCSVToList(IFormFile file)
        {
            var upload = new List<CsvUpload>();
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                upload = csv.GetRecords<CsvUpload>().ToList();
            }
            catch
            {
                return upload;
            }
            return upload;

        }
        public int ConvertIpToNumber(string ipAddress)
        {
            try
            {
                var ipParts = ipAddress.Split('.').Select(int.Parse).ToArray();

                int bitwise = (int)ipParts[0] << 24 | (int)ipParts[1] << 16 | (int)ipParts[2] << 8 | (int)ipParts[3];

                Log.Information($"{ipAddress} converted to integer: {bitwise}");
                return bitwise;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to convert IP address to integer:" + ex.ToString());
                return 0;
            }
        }
        #endregion
    }
}
