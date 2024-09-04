using AutoMapper;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
                cfg.AddProfile<AutoMapperConfig>();
            });

            _mapper = config.CreateMapper();
            _context = context;
            _httpClient = httpClient;
        }

        #region GeoLocation
        public async Task<bool> SaveGeoLocation(GeoLocationRequest request)
        {
            request.StartIpnumber = ConvertIpToNumber(request.StartIp!);
            request.EndIpnumber = ConvertIpToNumber(request.EndIp!);

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
                var geoLocation = _context.GeoLocations.Where(x => x.StartIpnumber <= numericIpAddress && x.EndIpnumber >= numericIpAddress).FirstOrDefault();
                
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
        public async Task<List<GeoLocationInfo>?> GetGeoLocations(string? countryCode)
        {
            List<GeoLocationInfo> geoLocations = [];
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
                        var mapped = _mapper.Map<GeoLocationInfo>(geoLocation);
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
                var request = await _context.CountryDetails.Where(x => x.CountryCode == countryCode).FirstOrDefaultAsync();

                if (request == null)
                {
                    return null;
                }
                else
                {
                    var response = _mapper.Map<CountryDetailsResponse>(request);
                    return response;
                }
            }
            catch
            {
                Log.Error($"Failed to get country from database:\nCountry code: {countryCode}");
                return null;
            }
        }
        public async Task<bool> SaveCountry(string? countryCode, string? provider)
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
        public async Task<bool> CountryExists(string? countryCode)
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
        public async Task<RestCountriesResponse?> GetCountryDetailsFromProvider(string? countryCode, string? provider)
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

        #region User and Token
        public string CreateToken(UserResponse user, string secret, DateTime expiration)
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
        public async Task<bool> SaveUser(UserRequest user)
        {
            try
            {
                var map = _mapper.Map<User>(user);
                map.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);

                await _context.AddAsync(map);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                Log.Error($"Failed to save user to database.");
                return false;
            }
        }
        public async Task<bool> SaveToken(TokenRequest token)
        {
            try
            {
                var map = _mapper.Map<Token>(token);

                await _context.AddAsync(map);
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
