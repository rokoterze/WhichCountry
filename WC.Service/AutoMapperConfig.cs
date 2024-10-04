using AutoMapper;
using WC.DataAccess.Models;
using WC.Models.DTO.Request;
using WC.Models.DTO.Response;

namespace WC.Service
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<GeoLocationRequest, GeoLocationInfo>().ReverseMap();
            CreateMap<GeoLocationRequest, DataAccess.Models.GeoLocation>();
            CreateMap<GeoLocationInfo, CsvUpload>().ReverseMap();

            CreateMap<DataAccess.Models.GeoLocation, GeoLocationInfo>().ReverseMap();
            CreateMap<DataAccess.Models.GeoLocation, Models.DTO.Response.GeoLocation>().ReverseMap();

            CreateMap<CountryDetail, RestCountries>().ReverseMap();
            CreateMap<CountryDetail, CountryDetails>().ReverseMap();

            CreateMap<UserRequest, DataAccess.Models.User>();
            CreateMap<DataAccess.Models.User, Models.DTO.Response.User>();

            CreateMap<TokenRequest, Token>();
        }
    }
}
