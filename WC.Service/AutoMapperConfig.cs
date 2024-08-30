using AutoMapper;
using WC.DataAccess.Models;
using WC.Models.DTO;

namespace WC.Service
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<GeoLocationRequest, GeoLocationInfo>().ReverseMap();
            CreateMap<GeoLocationInfo, CsvUpload>().ReverseMap();

            CreateMap<GeoLocation, GeoLocationInfo>().ReverseMap();
            CreateMap<GeoLocation, GeoLocationResponse>().ReverseMap();

            CreateMap<CountryDetail, RestCountriesResponse>().ReverseMap();
            CreateMap<CountryDetail, CountryDetailsResponse>().ReverseMap();
        }
    }
}
