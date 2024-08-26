using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace WC.Models.DTO
{
    public class RestCountriesResponse
    {
        [JsonPropertyName("cca2")]
        public string? CountryCode { get; set; }
        
        [JsonPropertyName("name")]
        public CountryName? Name { get; set; }
        
        [JsonPropertyName("region")]
        public string? CountryRegion { get; set; }
        
        [JsonPropertyName("subregion")]
        public string? CountrySubregion { get; set; }

        [JsonPropertyName("flags")]
        public Flag? Flags { get; set; }

        public string? CountryName => Name?.Official;
        public string? FlagUrl => Flags?.Png;
    }

    public class CountryName
    {
        [JsonPropertyName("official")]
        public string? Official { get; set; }
    }

    public class Flag
    {
        [JsonPropertyName("png")]
        public string? Png { get; set; }
    }
}
