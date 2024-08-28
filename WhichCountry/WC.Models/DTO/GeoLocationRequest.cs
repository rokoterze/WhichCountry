using CsvHelper.Configuration.Attributes;

namespace WC.Models.DTO
{
    public class GeoLocationRequest
    {
        [Index(0)]
        public string? StartIp { get; set; }
        [Index(1)]
        public string? EndIp { get; set; }
        [Index(2)]
        public string? CountryCode { get; set; }
    }
}
