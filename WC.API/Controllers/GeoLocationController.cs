using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;
using System.Net;
using WC.DataAccess.Models;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;
using WC.Service.IService;
using static Azure.Core.HttpHeader;

namespace WC.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GeoLocationController : ControllerBase
    {
        private readonly WhichCountryContext _context;
        private readonly IMapper _mapper;
        private readonly IWcService _wcService;
        private readonly ILogger<GeoLocationController> _logger;

        private readonly string? _uploadSize;
        private readonly string? _countryDetailsProvider;
        private readonly string? _imagePath;
        private readonly string? _imageExtensionType;


        public GeoLocationController(WhichCountryContext context, IMapper mapper, IWcService wcService, ILogger<GeoLocationController> logger, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _wcService = wcService;
            _logger = logger;

            _countryDetailsProvider = configuration["CountryDetailsProvider:Url"];
            _uploadSize = configuration["Configuration:UploadSize"];
            _imagePath = configuration["Images:ImagePath"];
            _imageExtensionType = configuration["Images:ImageExtensionType"];
        }

        [HttpPost("[action]")]
        public async Task<Counter> UploadCSVFile(IFormFile file)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            Counter counter = new();

            var listA = new List<CsvUpload>(); // -> Contains chunk of objects from file. Chunks is defined by uploadSize
            var listB = new List<GeoLocationInfo>(); // -> Contains mapped objects from listA [CSVUpload -> GeoLocation] 
            var listC = new List<GeoLocationInfo>(); // -> Contains GeoLocations from db by common countryCode

            int uploadSize = _uploadSize != null ? Int32.Parse(_uploadSize) : 0; // -> Upload size from appsettings.json
            int fileSize; // -> Imported CSV file list size

            int counterIndex = uploadSize;
            int startIndex = 0;

            var fileList = _wcService.ConvertCSVToList(file);
            fileSize = fileList != null ? fileList.Count : 0;

            counter.FileSize = fileSize;
            counter.Duplicates = 0;
            counter.Inserted = 0;
            counter.Failed = 0;

            if (fileSize > uploadSize)
            {
                int endIndex = uploadSize;

                for (int i = startIndex; i < endIndex; i++)
                {
                    for (int j = startIndex; j < endIndex; j++)
                    {
                        listA.Add(fileList![j]);
                    }

                    foreach (var a in listA)
                    {
                        var mappedGL = _mapper.Map<GeoLocationInfo>(a);

                        var geoLocations = await _wcService.GetGeoLocations(mappedGL.CountryCode);
                        listC.AddRange(geoLocations!);

                        if (!listC.Any(x => x.CountryCode == mappedGL.CountryCode && x.StartIp == mappedGL.StartIp && x.EndIp == mappedGL.EndIp))
                        {
                            listB.Add(mappedGL);
                        }
                        else
                        {
                            counter.Duplicates++;
                        }
                    }

                    foreach (var b in listB)
                    {
                        int saveGL = 0;

                        if (!listC.Contains(b))
                        {
                            var mappedGLR = _mapper.Map<GeoLocationRequest>(b);

                            var result = await _wcService.GeoLocationInsert(mappedGLR);

                            if (result)
                            {
                                counter.Inserted++;
                                saveGL = 1;
                            }
                            else
                            {
                                counter.Failed++;
                            }

                            await _wcService.CountryInsert(b.CountryCode, _countryDetailsProvider, _imagePath, _imageExtensionType);
                        }
                        if (listC.Contains(b))
                        {
                            counter.Duplicates++;
                        }
                        if (saveGL == 1)
                        {
                            listC.Add(b);
                            saveGL = 0;
                        }
                    }

                    listA.Clear();
                    listB.Clear();
                    listC.Clear();

                    startIndex += uploadSize;
                    counterIndex += uploadSize;
                    endIndex = Math.Min(counterIndex, fileSize);
                }
            }
            else
            {
                //TODO: Implement if filesize is smaller than upload size.
                return null!;
            }
            stopwatch.Stop();

            TimeSpan stopwatchElapsed = stopwatch.Elapsed;
            Log.Information($"Time elapsed: {stopwatchElapsed}");
            return counter;
        }

        [HttpGet("[action]")]
        public async Task<Models.DTO.Response.GeoLocation?> IPAddressGeoLocation([FromQuery] string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out _))
            {
                Log.Error($"IP address format is invalid: {ipAddress}");

                return null;
            }

            var numericIp = _wcService.ConvertIpToNumber(ipAddress);
            var geoLocation = await _wcService.GetGeoLocation(numericIp);

            if (geoLocation == null)
            {
                Log.Warning($"IP address not found: {ipAddress}");

                return null;
            }

            var countryDetails = await _wcService.GetCountry(geoLocation.CountryCode);

            if (countryDetails == null)
            {
                Log.Error($"Country details are not found: {geoLocation.CountryCode}");
            }

            var response = new Models.DTO.Response.GeoLocation
            {
                CountryCode = geoLocation.CountryCode,
                CountryName = countryDetails?.CountryName,
                CountryRegion = countryDetails?.CountryRegion,
                CountrySubregion = countryDetails?.CountrySubregion,
            };

            Log.Information($"GeoLocation: {response}");

            return response;
        }

    
        [HttpGet("[action]")]
        public async Task<Models.DTO.Response.FileResult?> GetCountryFlag([FromQuery] string countryCode)
        {
            try
            {
                var image = await System.IO.File.ReadAllBytesAsync(_imagePath + "\\" + countryCode + _imageExtensionType);
                
                var file = new Models.DTO.Response.FileResult()
                {
                    FileName = $"{countryCode}{_imageExtensionType}",
                    Content = image,
                    MimeType = "image/png"
                };

                if (file == null || file.Content == null)
                {
                    return null;
                }

                return file;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error retrieving image for Country Code: {countryCode}: {ex.Message}";

                Log.Error(errorMessage);
                return null;
            }
        }

        [HttpPost("[action]")]
        public async Task<bool> UpdateUserPlan(UserPlanRequest userPlanRequest)
        {
            throw new NotImplementedException();
        }

    }
}
