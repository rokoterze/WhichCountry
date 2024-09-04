using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;
using System.Net;
using WC.DataAccess.Models;
using WC.Models.DTO;
using WC.Service.IService;

namespace WC.API.Controllers
{
    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public class GeoLocationController : ControllerBase
    {
        private readonly WhichCountryContext _context;
        private readonly IMapper _mapper;
        private readonly IWcService _wcService;
        private readonly ILogger<GeoLocationController> _logger;

        private readonly string? _uploadSize;
        private readonly string? _countryDetailsProvider;
        

        public GeoLocationController(WhichCountryContext context, IMapper mapper, IWcService wcService, ILogger<GeoLocationController> logger, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _wcService = wcService;
            _logger = logger;

            _countryDetailsProvider = configuration["WcConfiguration:countryDetailsProvider"];
            _uploadSize = configuration["WcConfiguration:uploadSize"];
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

            int uploadSize = _uploadSize != null ? Int32.Parse(_uploadSize) : 0 ; // -> Upload size from appsettings.json
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

                            var result = await _wcService.SaveGeoLocation(mappedGLR);

                            if (result)
                            {
                                counter.Inserted++;
                                saveGL = 1;
                            }
                            else 
                            {
                                counter.Failed++;
                            }

                            await _wcService.SaveCountry(b.CountryCode, _countryDetailsProvider);
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
            stopwatch.Stop();

            TimeSpan stopwatchElapsed = stopwatch.Elapsed;
            Log.Information($"Time elapsed: {stopwatchElapsed}");
            return counter;
        }

        [HttpGet("[action]")]
        [AllowAnonymous]
        public async Task<GeoLocationResponse?> IPAddressGeoLocation(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out _))
            {
                Log.Error($"IP address format is invalid: {ipAddress}");

                return null;
            }

            var numericIp = _wcService.ConvertIpToNumber(ipAddress);
            var geoLocation = _wcService.GetGeoLocation(numericIp);

            if (geoLocation == null)
            {
                Log.Information($"IP address not found: {ipAddress}");

                return null;
            }

            var countryDetails = await _wcService.GetCountry(geoLocation.CountryCode);

            if (countryDetails == null)
            {
                Log.Error($"Country details are not found: {geoLocation.CountryCode}");
            }

            var response = new GeoLocationResponse
            {
                CountryCode = geoLocation.CountryCode,
                CountryName = countryDetails?.CountryName,
                CountryRegion = countryDetails?.CountryRegion,
                CountrySubregion = countryDetails?.CountrySubregion,
                FlagUrl = countryDetails?.FlagUrl
            };

            Log.Information("GeoLocation: {@response}", response);

            return response;
        }
    }
}
