using AutoMapper;
using Azure.Core;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using WC.DataAccess.Models;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;
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
                cfg.AddProfile<AutoMapperConfig>();
            });

            _mapper = config.CreateMapper();
            _context = context;
            _httpClient = httpClient;
        }

        #region GeoLocation
        public async Task<bool> GeoLocationInsert(GeoLocationRequest request)
        {
            request.StartIpnumber = ConvertIpToNumber(request.StartIp!);
            request.EndIpnumber = ConvertIpToNumber(request.EndIp!);

            try
            {
                var geoLocation = _mapper.Map<GeoLocation>(request);

                await _context.AddAsync(geoLocation);
                await _context.SaveChangesAsync();
            }
            catch
            {
                Log.Error($"Failed to save geolocation: Country Code: {request.CountryCode}, Start IP: {request.StartIp}, End IP: {request.EndIp}");
                return false;
            }
            return true;
        }
        public async Task<GeoLocationResponse?> GetGeoLocation(int numericIpAddress)
        {
            try
            {
                var geoLocation = await _context.GeoLocations
                    .Where(x => x.StartIpnumber <= numericIpAddress && x.EndIpnumber >= numericIpAddress)
                    .FirstOrDefaultAsync();

                if (geoLocation == null)
                {
                    Log.Warning($"Geo Location with numeric IP address: {numericIpAddress} not found.");
                    return null;
                }
                else
                {
                    var response = _mapper.Map<GeoLocationResponse>(geoLocation);
                    return response;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get Geo Location with numeric IP address: {numericIpAddress} from database.\n {ex.Message}");
                return null;
            }
        }
        public async Task<List<GeoLocationInfo>?> GetGeoLocations(string? countryCode)
        {
            try
            {
                var geoLocations = await _context.GeoLocations
                    .Where(x => x.CountryCode == countryCode)
                    .ToListAsync();

                if (geoLocations == null)
                {
                    Log.Warning($"Geo Location with Country Code: {countryCode} not found.");
                    return null;
                }

                var response = _mapper.Map<List<GeoLocationInfo>>(geoLocations);
                return response;
            }
            catch
            {
                Log.Error($"Failed to fetch data from database:\nCountry Code: {countryCode}");
                return null;
            }
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
                    Log.Warning($"Country with Country Code: {countryCode} not found.");
                    return null;
                }
                else
                {
                    var response = _mapper.Map<CountryDetailsResponse>(country);
                    return response;
                }
            }
            catch
            {
                Log.Error($"Failed to get country from database. Country code: {countryCode}");
                return null;
            }
        }
        public async Task<bool> CountryInsert(string? countryCode, string? provider, string? imagePath, string? extension)
        {
            var countryExist = await DoesCountryExist(countryCode);

            if (!countryExist)
            {
                var countryFromProvider = await GetCountryDetailsFromProvider(countryCode, provider);

                if (countryFromProvider == null)
                {
                    Log.Information($"Failed to retrieve country details from provider for Country Code: {countryCode}.");
                    return false;
                }

                try
                {
                    countryFromProvider.FileName = $"{countryFromProvider.CountryCode}{extension}";

                    var countryDetail = _mapper.Map<CountryDetail>(countryFromProvider);

                    var image = await DownloadImage(countryDetail.FlagUrl, imagePath!, countryDetail.CountryCode, extension!);
                    if (!image)
                    {
                        Log.Warning($"Failed to download image from {countryDetail.FlagUrl} for Country Code: {countryDetail.CountryCode}");
                    }

                    await _context.AddAsync(countryDetail);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    Log.Error($"Failed to save country details for Country Code: {countryCode}.");
                    return false;
                }
            }

            return true;
        }
        public async Task<bool> DoesCountryExist(string? countryCode)
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
                Log.Error($"Failed to retrieve response for Country code: {countryCode}.");
                return false;
            }

            return true;
        }
        public async Task<RestCountriesResponse?> GetCountryDetailsFromProvider(string? countryCode, string? provider)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(provider + countryCode);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var deserialized = JsonSerializer.Deserialize<List<RestCountriesResponse>>(responseBody)!;

                return deserialized?.FirstOrDefault();
            }
            catch
            {
                Log.Error($"Failed to retrieve country details from provider for Country Code: {countryCode}");
                return null;
            }
        }
        #endregion

        #region User and Token
        public string GenerateToken(UserResponse user, string secret, DateTime expiration)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.Name, user.Username!)
            ];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiration,
                signingCredentials: cred
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
        public async Task<UserResponse> GetUser(string username)
        {
            try
            {
                var request = await _context.Users.Where(x => x.Username == username).FirstOrDefaultAsync();

                if (request == null)
                {
                    return null!;
                }
                else
                {
                    var response = _mapper.Map<UserResponse>(request);
                    return response;
                }
            }
            catch
            {
                Log.Error($"Failed to get user from database.");
                return null!;
            }
        }
        public async Task<bool> UserInsert(UserRequest request)
        {
            try
            {
                var user = _mapper.Map<User>(request);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                await _context.AddAsync(user);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                Log.Error($"Failed to save user to database.");
                return false;
            }
        }
        public async Task<bool> TokenInsert(TokenRequest request)
        {
            try
            {
                var token = _mapper.Map<Token>(request);

                await _context.AddAsync(token);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                Log.Error($"Failed to save token to database.");
                return false;
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
            catch(Exception ex)
            {
                Log.Error($"CSV file convert failed:" + ex.Message);
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

                return bitwise;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to convert IP address to integer:" + ex.ToString());
                return 0;
            }
        }
        public async Task<bool> DownloadImage(string url, string outputFolder, string outputFileName, string extension)
        {
            try
            {
                using Stream fileStream = await _httpClient.GetStreamAsync(url);
                Directory.CreateDirectory(outputFolder);
                string path = Path.Combine(outputFolder, $"{outputFileName}{extension}");

                using FileStream outputFileStream = new(path, FileMode.CreateNew);

                await fileStream.CopyToAsync(outputFileStream);
                return true;
            }
            catch (Exception ex) 
            {
                Log.Error($"Download Image operation failed: {ex.Message}");
                return false;
            }
        }
        #endregion


    }
}
