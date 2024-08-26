namespace WC.Models.DTO
{
    public class GeoLocationResponse
    {
        public string? StartIp { get; set; }
        public string? EndIp { get; set; }
        public string? CountryCode { get; set; }
        public CountryDetailsResponse? CountryDetails { get; set; }
    }
}
