using AutoMapper;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        public List<CsvUpload> ConvertCSVToList(IFormFile file)
        {
            var upload = new List<CsvUpload>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                upload = csv.GetRecords<CsvUpload>().ToList();
            }

            return upload;
        }
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
                return false;
            }
            return true;
        }
        public async Task<bool> SaveCountryDetails(string countryCode, string? provider)
        {
            var country = await CheckCountry(countryCode);

            if (!country)
            {
                var result = await GetCountryDetailsFromProvider(countryCode, provider);

                if (result == null)
                {
                    Console.WriteLine($"Failed to retrieve country details for {countryCode}.");
                    return false;
                }

                try
                {
                    var saveResult = await SaveCountryDetailsToDb(result);

                    if (!saveResult)
                    {
                        Console.WriteLine($"Failed to save country details for {countryCode}.");
                        return false;
                    }
                }
                catch
                {
                    Console.WriteLine("Saving country details to database failed!");
                    return false;
                }
            }

            return true;
        }

        private async Task<RestCountriesResponse?> GetCountryDetailsFromProvider(string countryCode, string? provider)
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
                return null;
            }
        }

        private async Task<bool> SaveCountryDetailsToDb(RestCountriesResponse response)
        {
            try
            {
                var map = _mapper.Map<CountryDetail>(response);

                await _context.AddAsync(map);
                await _context.SaveChangesAsync();
            }
            catch
            {
                return false;
            }
            return true;
        }

        private async Task<bool> CheckCountry(string countryCode)
        {
            var country = await _context.CountryDetails.FirstOrDefaultAsync(x => x.CountryCode == countryCode);

            if (country == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
