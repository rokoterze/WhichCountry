using CsvHelper.Configuration.Attributes;

namespace WC.Models.DTO
{
    public class GeoLocationRequest
    {
        [Index(0)]
        public string? StartIp { get; set; }
        public int? StartIpnumber { get; set; }
        public string? EndIp { get; set; }
        public int? EndIpnumber { get; set; }
        public string? CountryCode { get; set; }
    }
}
