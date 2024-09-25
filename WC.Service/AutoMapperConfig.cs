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
            CreateMap<GeoLocationRequest, GeoLocation>();
            CreateMap<GeoLocationInfo, CsvUpload>().ReverseMap();

            CreateMap<GeoLocation, GeoLocationInfo>().ReverseMap();
            CreateMap<GeoLocation, GeoLocationResponse>().ReverseMap();

            CreateMap<CountryDetail, RestCountriesResponse>().ReverseMap();
            CreateMap<CountryDetail, CountryDetailsResponse>().ReverseMap();

            CreateMap<UserRequest, User>();
            CreateMap<User, UserResponse>();

            CreateMap<TokenRequest, Token>();
        }
    }
}
