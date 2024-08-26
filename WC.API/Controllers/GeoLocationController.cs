using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using System.Data;
using System.Net;
using WC.DataAccess.Models;
using WC.Models.DTO;
using WC.Service;
using WC.Service.IService;

namespace WC.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GeoLocationController : ControllerBase
    {
        private readonly WhichCountryContext _context;
        private readonly IMapper _mapper;
        private readonly IWcService _wcService;
        private readonly int _uploadSize;
        private readonly string? _countryDetailsProvider;
        private readonly ILogger<GeoLocationController> _logger;

        public GeoLocationController(WhichCountryContext context, IMapper mapper, IWcService wcService, IOptions<WcConfiguration> wcConfig, ILogger<GeoLocationController> logger)
        {
            _context = context;
            _mapper = mapper;
            _wcService = wcService;
            _uploadSize = wcConfig.Value.UploadSize;
            _countryDetailsProvider = wcConfig.Value.CountryDetailsProvider;
            _logger = logger;
        }

        //Todo: Duplicates inserted, slow
        [HttpPost("UploadCSV")]
        public async Task<Counter> UploadCSV(IFormFile file)
        {
            {
                int endIndex;
                int counter = 0;

                int fileCount;
                int insertedTotal = 0;
                int duplicate = -1;

                int chunkSize = _uploadSize;

                var listFile = _wcService.ConvertCSVToList(file);
                var listToInsert = new List<CsvUpload>();

                fileCount = listFile.Count;

                if (chunkSize < listFile.Count)
                {
                    for (int i = 0; i < listFile.Count; i += chunkSize)
                    {
                        endIndex = Math.Min(i + chunkSize, listFile.Count);

                        for (int j = i; j < endIndex; j++)
                        {
                            listToInsert.Add(listFile[j]);
                            counter++;
                        }

                        var mapped = _mapper.Map<List<GeoLocation>>(listToInsert);

                        try
                        {
                            foreach (var geoLocation in mapped)
                            {
                                await _wcService.SaveCountryDetails(geoLocation.CountryCode, _countryDetailsProvider);

                                await _context.AddRangeAsync(geoLocation);
                                await _context.SaveChangesAsync();
                            }
                            insertedTotal += counter;
                            counter = 0;
                            duplicate = 0;
                        }

                        catch (DbUpdateException ex)
                        {
                            if (ex.InnerException is SqlException sqlException && sqlException.Number == 2627)
                            {
                                duplicate = (duplicate + chunkSize);
                                continue;
                            }
                        }
                    }
                }

                else
                {
                    var mapped = _mapper.Map<List<GeoLocation>>(listToInsert);

                    await _context.AddRangeAsync(mapped);
                    await _context.SaveChangesAsync();
                }

                var counterObj = new Counter
                {
                    FileCount = fileCount,
                    Duplicate = duplicate,
                    InsertedTotal = insertedTotal
                };

                return counterObj;
            }
        }

        [HttpPost("UploadCSV2")]
        public async Task<bool> UploadCSV2(IFormFile file)
        {
            //TODO: Return Counter object instead bool, continue inserting chunks

            var listA = new List<CsvUpload>(); // -> Contains chunk of objects from file. Chunks is defined by uploadSize
            var listB = new List<GeoLocation>(); // -> Contains mapped objects from listA [CSVUpload -> GeoLocation] 
            var listC = new List<GeoLocation>(); // -> Contains GeoLocations from db by common countryCode

            int uploadSize = _uploadSize; // -> Upload size from appsettings.json
            int fileSize; // -> Imported CSV file list size

            int indexStart = 1000;

            var fileList = _wcService.ConvertCSVToList(file);
            fileSize = fileList.Count;

            if (fileSize > uploadSize)
            {
                //Insert uploadSize number of items in new list (listA)
                for (int i = indexStart; i < indexStart + uploadSize; i++)
                {
                    listA.Add(fileList[i]);
                }

                indexStart = listA.Count;

                foreach (var a in listA)
                {
                    //Map every item from list to GeoLocation
                    var mappedGL = _mapper.Map<GeoLocation>(a);

                    //Add item obj to GeoLocation db
                    //1. Check does it exists already in db (fetch to listC every object by countryCode from listA)
                    listC.AddRange(await _context.GeoLocations.Where(x => x.CountryCode == mappedGL.CountryCode).ToListAsync());

                    if (!listC.Any(x => x.CountryCode == mappedGL.CountryCode && x.StartIp == mappedGL.StartIp && x.EndIp == mappedGL.EndIp))
                    {
                        listB.Add(mappedGL);
                    }
                }

                foreach (var b in listB)
                {
                    int saveGL = 0;

                    //1.1 If no -> Insert into GeoLocation
                    if (!listC.Contains(b))
                    {
                        var mappedGLR = _mapper.Map<GeoLocationRequest>(b);
                        if (await _wcService.SaveGeoLocation(mappedGLR))
                        {
                            saveGL = 1;
                        }

                        //Inside this function, CountryDetails check/fetch/save is done too.
                        await _wcService.SaveCountryDetails(b.CountryCode, _countryDetailsProvider);
                    }
                    if (saveGL == 1)
                    {
                        listC.Add(b);
                        saveGL = 0;
                    }
                }

                listB.Clear();
                listC.Clear();
            }
            return true;
        }

        [HttpGet("IPAddressGeoLocation")]
        public async Task<GeoLocationResponse> CheckIpAddress(string ipAddress)
        {
            //Check IP format
            if (!IPAddress.TryParse(ipAddress, out _))
            {
                Log.Error($"IP address format is invalid: {ipAddress}");

                return new GeoLocationResponse();
            }

            var numericIp = ConvertIpToNumber(ipAddress);

            var geoLocation = _context.GeoLocations
                .AsEnumerable()
                .Where(x => ConvertIpToNumber(x.StartIp) <= numericIp && ConvertIpToNumber(x.EndIp) >= numericIp).FirstOrDefault();

            if (geoLocation == null)
            {
                Log.Information($"IP address not found: {ipAddress}");

                return new GeoLocationResponse();
            }

            var countryDetails = await _context.CountryDetails
                .Where(x => x.CountryCode == geoLocation.CountryCode)
                .FirstOrDefaultAsync();

            if (countryDetails == null)
            {
                Log.Error($"Country details are not found: {geoLocation.CountryCode}");
            }

            var response = new GeoLocationResponse
            {
                StartIp = geoLocation.StartIp,
                EndIp = geoLocation.EndIp,
                CountryCode = geoLocation.CountryCode,
                CountryDetails = new CountryDetailsResponse
                {
                    CountryName = countryDetails?.CountryName,
                    CountryRegion = countryDetails?.CountryRegion,
                    CountrySubregion = countryDetails?.CountrySubregion,
                    FlagUrl = countryDetails?.FlagUrl
                }
            };

            Log.Information("GeoLocation: {@geoLocation}", geoLocation);


            return response;
        }

        private static int ConvertIpToNumber(string ipAddress)
        {
            try
            {
                var ipParts = ipAddress.Split('.').Select(int.Parse).ToArray();

                //bitwise shift, 
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

    }
}
